using System;
using System.Collections.Generic;
using System.IO;

namespace RainMeadow
{
    public partial class Serializer
    {
        public readonly byte[] buffer;
        private readonly long capacity;
        private long margin;
        public long Position => stream.Position;

        public bool IsWriting { get; set; }
        public bool IsReading { get; set; }
        private bool Aborted { get; set; }

        public MemoryStream stream;
        public BinaryWriter writer;
        public BinaryReader reader;
        public OnlinePlayer currPlayer;
        private uint eventCount;
        private long eventHeader;
        private uint stateCount;
        private long stateHeader;
        public int zipTreshold = 4000;

        static Serializer scratchpad;
        public Serializer(long bufferCapacity, bool scratch = false)
        {
            this.capacity = bufferCapacity;
            margin = 16;
            buffer = new byte[bufferCapacity];
            stream = new(buffer);
            writer = new(stream);
            reader = new(stream);

            if (!scratch) scratchpad = new Serializer(this.capacity, true);
        }

        private void PlayerHeaders()
        {
            if (IsWriting)
            {
                DebugOverlay.playersWritten.addPlayer(currPlayer);
                writer.Write(OnlineManager.mePlayer.tick);
                writer.Write(currPlayer.lastEventFromRemote);
                writer.Write(currPlayer.tick); writer.Write(currPlayer.recentTicksToAckBitpack);
            }
            if (IsReading)
            {
                DebugOverlay.playersRead.addPlayer(currPlayer);
                var newTick = reader.ReadUInt32();
                if (!EventMath.IsNewer(newTick, currPlayer.tick))
                {
                    AbortRead();
                    return;
                }
                currPlayer.NewTick(newTick);
                currPlayer.EventAckFromRemote(reader.ReadUInt16());
                currPlayer.TickAckFromRemote(reader.ReadUInt32(), reader.ReadUInt16());
            }
        }

        private void AbortRead()
        {
            RainMeadow.Debug("aborted read");
            currPlayer = null;
            IsReading = false;
            IsDelta = false;
            Aborted = true;
            scratchpad.currPlayer = null;
            scratchpad.IsReading = false;
        }

        public void BeginWrite(OnlinePlayer toPlayer)
        {
            currPlayer = toPlayer;
            if (IsWriting || IsReading) throw new InvalidOperationException("not done with previous operation");
            IsWriting = true;
            IsDelta = false;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
            scratchpad.currPlayer = toPlayer;
            scratchpad.IsWriting = true;
        }

        private void BeginWriteEvents()
        {
            eventCount = 0;
            eventHeader = stream.Position;
            writer.Write(eventCount); // fake write, we'll overwrite this later
        }

        private bool WriteEvent(OnlineEvent playerEvent)
        {
            scratchpad.stream.Seek(0, SeekOrigin.Begin);
            playerEvent.CustomSerialize(scratchpad);
            if (scratchpad.Position < (capacity - Position - margin))
            {
                writer.Write((byte)playerEvent.eventType);
                writer.Write(scratchpad.buffer, 0, (int)scratchpad.Position);
                eventCount++;
                return true;
            }
            return false;
        }

        private void EndWriteEvents()
        {
            var temp = stream.Position;
            stream.Position = eventHeader;
            writer.Write(eventCount);
            stream.Position = temp;
        }

        private void BeginWriteStates()
        {
            stateCount = 0;
            stateHeader = stream.Position;
            writer.Write(stateCount);
        }

        private bool WriteZippedState(OnlineState state)
        {
            var pos = (int)scratchpad.Position;
            scratchpad.stream.Seek(0, SeekOrigin.Begin);
            var zippedState = new DeflateState(scratchpad.stream, pos);
            var zippedSize = zippedState.bytes.Length;
            RainMeadow.Debug($"zipping state {state}, was {pos} became ~{zippedSize}");
            if (zippedSize >= pos)
            {
                RainMeadow.Debug($"cancelling zipping of {state}");
                scratchpad.stream.Seek(pos, SeekOrigin.Begin); //restore position
                return false;
            }
            return WriteState(zippedState);
        }

        private bool WriteState(OnlineState state)
        {
            scratchpad.stream.Seek(0, SeekOrigin.Begin);
            state.WritePolymorph(scratchpad);
            scratchpad.WrappedSerialize(state);
            bool fits = scratchpad.Position < (capacity - Position - margin);
            if ((scratchpad.Position > zipTreshold || !fits) && state is not DeflateState)
            {
                return WriteZippedState(state);
            }
            else if (fits)
            {
                writer.Write(scratchpad.buffer, 0, (int)scratchpad.Position);
                stateCount++;
                return true;
            }
            return false;
        }

        private void EndWriteStates()
        {
            var temp = stream.Position;
            stream.Position = stateHeader;
            writer.Write(stateCount);
            stream.Position = temp;
        }

        public void EndWrite()
        {
            //RainMeadow.Debug($"serializer wrote: {eventCount} events; {stateCount} states; total {stream.Position} bytes");
            currPlayer = null;
            IsWriting = false;
            writer.Flush();
            scratchpad.currPlayer = null;
            scratchpad.IsWriting = false;
        }

        private void BeginRead(OnlinePlayer fromPlayer)
        {
            currPlayer = fromPlayer;
            if (IsWriting || IsReading) throw new InvalidOperationException("not done with previous operation");
            IsReading = true;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
            scratchpad.currPlayer = fromPlayer;
            scratchpad.IsReading = true;
        }

        private uint BeginReadEvents()
        {
            return reader.ReadUInt32();
        }

        private OnlineEvent ReadEvent()
        {
            OnlineEvent e = OnlineEvent.NewFromType((OnlineEvent.EventTypeId)reader.ReadByte());
            e.from = currPlayer;
            e.to = OnlineManager.mePlayer;
            e.CustomSerialize(this);
            return e;
        }

        private uint BeginReadStates()
        {
            return reader.ReadUInt32();
        }

        private OnlineState ReadState()
        {
            OnlineState s = OnlineState.ParsePolymorph(this);
            if (s is RootDeltaState ps)
            {
                ps.from = currPlayer;
                ps.tick = currPlayer.tick;
            }

            WrappedSerialize(s);

            if (s is DeflateState ds)
            {
                scratchpad.stream.Seek(0, SeekOrigin.Begin);
                ds.Decompress(scratchpad.stream);
                scratchpad.stream.Seek(0, SeekOrigin.Begin);
                s = scratchpad.ReadState();
            }

            return s;
        }

        public void EndRead()
        {
            currPlayer = null;
            IsReading = false;
            scratchpad.currPlayer = null;
            scratchpad.IsReading = false;
        }

        public void ReadData(OnlinePlayer fromPlayer, long size)
        {
            fromPlayer.bytesIn[fromPlayer.bytesSnapIndex] = (int)size;

            BeginRead(fromPlayer);

            PlayerHeaders();
            if (Aborted)
            {
                RainMeadow.Debug("skipped packet");
                return;
            }

            uint ne = BeginReadEvents();
            fromPlayer.eventsRead = ne > 0; // something is being read, record for debug
            RainMeadow.Trace($"Receiving {ne} events from player {fromPlayer}");
            for (uint ie = 0; ie < ne; ie++)
            {
                OnlineManager.ProcessIncomingEvent(ReadEvent());
            }

            uint ns = BeginReadStates();
            fromPlayer.statesRead = ns > 0; // something is being read, record for debug
            RainMeadow.Trace($"Receiving {ns} states");
            for (uint ist = 0; ist < ns; ist++)
            {
                OnlineManager.ProcessIncomingState(ReadState());
            }

            EndRead();
        }

        public long WriteData(OnlinePlayer toPlayer)
        {
            BeginWrite(toPlayer);

            PlayerHeaders();

            BeginWriteEvents();
            toPlayer.eventsWritten = toPlayer.OutgoingEvents.Count > 0; // something is being written, record for debug
            //RainMeadow.Debug($"Writing {toPlayer.OutgoingEvents.Count} events to player {toPlayer}");
            foreach (var e in toPlayer.OutgoingEvents)
            {
                if (WriteEvent(e))
                {
                    RainMeadow.Trace($"Wrote {e}");
                }
                else
                {
                    RainMeadow.Error($"WriteEvent failed for {e}");
                    RainMeadow.Error("no space for events");
                    break;
                }
            }
            EndWriteEvents();

            BeginWriteStates();
            toPlayer.statesWritten = toPlayer.OutgoingStates.Count > 0; // something is being written, record for debug

            RainMeadow.Trace($"Writing {toPlayer.OutgoingStates.Count} states");
            while (toPlayer.OutgoingStates.Count > 0)
            {
                var s = toPlayer.OutgoingStates.Dequeue();
                if (WriteState(s.state))
                {
                    RainMeadow.Trace($"{s.state} sent");
                    s.Sent();
                }
                else
                {
                    RainMeadow.Error($"State overflow writing to player {toPlayer}, {s.state} not sent");
                    s.Failed();
                }
            }

            EndWriteStates();

            EndWrite();

            toPlayer.bytesOut[toPlayer.bytesSnapIndex] = (int)Position;
            return Position;
        }

        // serializes resource.id and finds reference
        public void SerializeResourceByReference<T>(ref T onlineResource) where T : OnlineResource
        {
#if TRACING
            long wasPos = this.Position;
#endif
            if (IsWriting)
            {
                writer.Write(onlineResource.Id());
            }
            if (IsReading)
            {
                string r = reader.ReadString();
                onlineResource = (T)OnlineManager.ResourceFromIdentifier(r);
            }
#if TRACING
            if (IsWriting) RainMeadow.Trace(this.Position - wasPos);
#endif
        }

        // serializes entity.id and finds reference
        public void SerializeEntityById<T>(ref T onlineEntity) where T : OnlineEntity
        {
            if (IsWriting)
            {
                onlineEntity.id.CustomSerialize(this);
            }
            if (IsReading)
            {
                OnlineEntity.EntityId id = new();
                id.CustomSerialize(this);
                onlineEntity = (T)id.FindEntity();
            }
        }

        // serializes entity.id and finds reference
        public void SerializeNullableEntityById<T>(ref T onlineEntity) where T : OnlineEntity
        {
            if (IsWriting)
            {
                writer.Write(onlineEntity != null);
                if (onlineEntity != null)
                {
                    onlineEntity.id.CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    OnlineEntity.EntityId id = new();
                    id.CustomSerialize(this);
                    onlineEntity = (T)id.FindEntity();
                }
            }
        }

        public bool IsDelta; // OnlineState is allowed to change this (at the start of its serialize method)
        private void WrappedSerialize(OnlineState state)
        {
            bool wasDelta = IsDelta;
            state.CustomSerialize(this);
            IsDelta = wasDelta;
        }

        // Polymorphic - serializes its type and instantiates from it
        public void SerializePolyState<T>(ref T state) where T : OnlineState
        {
            if (IsWriting)
            {
                state.WritePolymorph(this);
                WrappedSerialize(state);
            }
            if (IsReading)
            {
                state = (T)OnlineState.ParsePolymorph(this);
                if (state is RootDeltaState ps)
                {
                    ps.from = currPlayer;
                    ps.tick = currPlayer.tick;
                }
                WrappedSerialize(state);
            }
        }

        public void SerializeNullablePolyState<T>(ref T nullableState) where T : OnlineState
        {
            if (IsWriting)
            {
                writer.Write(nullableState != null);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                if (nullableState != null)
                {
                    SerializePolyState(ref nullableState);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    SerializePolyState(ref nullableState);
                }
            }
        }

        public void SerializePolyStatesByte<T>(ref T[] states) where T : OnlineState
        {
            if (IsWriting)
            {
                if (states.Length > 255) throw new OverflowException("too many states");
                writer.Write((byte)states.Length);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                foreach (var state in states)
                {
                    state.WritePolymorph(this);
                    WrappedSerialize(state);
                }
            }
            if (IsReading)
            {
                byte count = reader.ReadByte();
                states = new T[count];
                for (int i = 0; i < count; i++)
                {
                    var s = (T)OnlineState.ParsePolymorph(this);
                    if (s is RootDeltaState ps)
                    {
                        ps.from = currPlayer;
                        ps.tick = currPlayer.tick;
                    }
                    WrappedSerialize(s);
                    states[i] = s;
                }
            }
        }

        public void SerializePolyStatesByte<T>(ref List<T> states) where T : OnlineState
        {
            if (IsWriting)
            {
                if (states.Count > 255) throw new OverflowException("too many states");
                writer.Write((byte)states.Count);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                foreach (var state in states)
                {
                    state.WritePolymorph(this);
                    WrappedSerialize(state);
                }
            }
            if (IsReading)
            {
                byte count = reader.ReadByte();
                states = new(count);
                for (int i = 0; i < count; i++)
                {
                    var s = (T)OnlineState.ParsePolymorph(this);
                    if (s is RootDeltaState ps)
                    {
                        ps.from = currPlayer;
                        ps.tick = currPlayer.tick;
                    }
                    WrappedSerialize(s);
                    states.Add(s);
                }
            }
        }


        public void SerializePolyStatesShort<T>(ref T[] states) where T : OnlineState
        {
            if (IsWriting)
            {
                if (states.Length > ushort.MaxValue) throw new OverflowException("too many states");
                writer.Write((ushort)states.Length);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                foreach (var state in states)
                {
                    state.WritePolymorph(this);
                    WrappedSerialize(state);
                }
            }
            if (IsReading)
            {
                ushort count = reader.ReadUInt16();
                states = new T[count];
                for (int i = 0; i < count; i++)
                {
                    var s = (T)OnlineState.ParsePolymorph(this);
                    if (s is RootDeltaState ps)
                    {
                        ps.from = currPlayer;
                        ps.tick = currPlayer.tick;
                    }
                    WrappedSerialize(s);
                    states[i] = s;
                }
            }
        }

        public void SerializePolyStatesShort<T>(ref List<T> states) where T : OnlineState
        {
            if (IsWriting)
            {
                if (states.Count > ushort.MaxValue) throw new OverflowException("too many states");
                writer.Write((ushort)states.Count);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                foreach (var state in states)
                {
                    state.WritePolymorph(this);
                    WrappedSerialize(state);
                }
            }
            if (IsReading)
            {
                ushort count = reader.ReadUInt16();
                states = new(count);
                for (int i = 0; i < count; i++)
                {
                    var s = (T)OnlineState.ParsePolymorph(this);
                    if (s is RootDeltaState ps)
                    {
                        ps.from = currPlayer;
                        ps.tick = currPlayer.tick;
                    }
                    WrappedSerialize(s);
                    states.Add(s);
                }
            }
        }

        // Static - fixed type
        public void SerializeStaticState<T>(ref T state) where T : OnlineState, new()
        {
            if (IsWriting)
            {
                WrappedSerialize(state);
            }
            if (IsReading)
            {
                state = new();
                if (state is RootDeltaState ps)
                {
                    ps.from = currPlayer;
                    ps.tick = currPlayer.tick;
                }
                WrappedSerialize(state);
            }
        }

        public void SerializeNullableStaticState<T>(ref T nullableState) where T : OnlineState, new()
        {
            if (IsWriting)
            {
                writer.Write(nullableState != null);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                if (nullableState != null)
                {
                    SerializeStaticState(ref nullableState);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    SerializeStaticState(ref nullableState);
                }
            }
        }

        public void SerializeStaticStatesByte<T>(ref T[] states) where T : OnlineState, new()
        {
            if (IsWriting)
            {
                if (states.Length > 255) throw new OverflowException("too many states");
                writer.Write((byte)states.Length);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                foreach (var state in states)
                {
                    WrappedSerialize(state);
                }
            }
            if (IsReading)
            {
                byte count = reader.ReadByte();
                states = new T[count];
                for (int i = 0; i < count; i++)
                {
                    T s = new();
                    if (s is RootDeltaState ps)
                    {
                        ps.from = currPlayer;
                        ps.tick = currPlayer.tick;
                    }
                    WrappedSerialize(s);
                    states[i] = s;
                }
            }
        }

        public void SerializeStaticStatesShort<T>(ref T[] states) where T : OnlineState, new()
        {
            if (IsWriting)
            {
                if (states.Length > ushort.MaxValue) throw new OverflowException("too many states");
                writer.Write((ushort)states.Length);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                foreach (var state in states)
                {
                    WrappedSerialize(state);
                }
            }
            if (IsReading)
            {
                ushort count = reader.ReadUInt16();
                states = new T[count];
                for (int i = 0; i < count; i++)
                {
                    T s = new();
                    if (s is RootDeltaState ps)
                    {
                        ps.from = currPlayer;
                        ps.tick = currPlayer.tick;
                    }
                    WrappedSerialize(s);
                    states[i] = s;
                }
            }
        }

        public void SerializePlayerIds(ref List<MeadowPlayerId> ids)
        {
            if (IsWriting)
            {
                writer.Write((byte)ids.Count);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                foreach (var id in ids)
                {
                    id.CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                byte count = reader.ReadByte();
                ids = new(count);
                for (int i = 0; i < count; i++)
                {
                    MeadowPlayerId s = MatchmakingManager.currentInstance.GetEmptyId();
                    s.CustomSerialize(this);
                    ids.Add(s);
                }
            }
        }

        public void SerializePlayerInLobby(ref OnlinePlayer player)
        {
            if (IsWriting)
            {
                writer.Write(player.inLobbyId);
#if TRACING
                if (IsWriting) RainMeadow.Trace(2);
#endif
            }
            if (IsReading)
            {
                var inLobbyId = reader.ReadUInt16();
                player = OnlineManager.lobby?.PlayerFromId(inLobbyId);
                if (player == null) RainMeadow.Error("Player not found! " + inLobbyId);
            }
        }

        // a referenced event is something that must have been ack'd that frame
        public void SerializeReferencedEvent(ref OnlineEvent referencedEvent)
        {
            if (IsWriting)
            {
                writer.Write(referencedEvent.eventId);
#if TRACING
                if (IsWriting) RainMeadow.Trace(2);
#endif
            }
            if (IsReading)
            {
                referencedEvent = currPlayer.GetRecentEvent(reader.ReadUInt16());
            }
        }

        public void SerializeEvent<T>(ref T playerEvent) where T : OnlineEvent
        {
            if (IsWriting)
            {
                writer.Write((byte)playerEvent.eventType);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                playerEvent.CustomSerialize(this);
            }
            if (IsReading)
            {
                playerEvent = (T)OnlineEvent.NewFromType((OnlineEvent.EventTypeId)reader.ReadByte());
                playerEvent.from = currPlayer;
                playerEvent.to = OnlineManager.mePlayer;
                playerEvent.CustomSerialize(this);
            }
        }

        public void SerializeEvents<T>(ref List<T> events) where T : OnlineEvent
        {
            if (IsWriting)
            {
                // TODO dynamic length
                if (events.Count > 255) throw new OverflowException("too many events");
                writer.Write((byte)events.Count);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                foreach (var playerEvent in events)
                {
                    writer.Write((byte)playerEvent.eventType);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                    playerEvent.CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                byte count = reader.ReadByte();
                events = new(count);
                for (int i = 0; i < count; i++)
                {
                    T playerEvent = (T)OnlineEvent.NewFromType((OnlineEvent.EventTypeId)reader.ReadByte());
                    playerEvent.from = currPlayer;
                    playerEvent.to = OnlineManager.mePlayer;
                    playerEvent.CustomSerialize(this);
                    events.Add(playerEvent);
                }
            }
        }
    }
}