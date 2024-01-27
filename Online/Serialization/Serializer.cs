using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        private StringBuilder eventLog;
        private long eventHeader;
        private uint stateCount;
        private long stateHeader;
        private bool warnOnSizeMissmatch = false; // to the brave soul that will fix/implement the estimates for obj sizes, good luck

        public Serializer(long bufferCapacity)
        {
            this.capacity = bufferCapacity;
            margin = (long)(bufferCapacity * 0.25f);
            buffer = new byte[bufferCapacity];
            stream = new(buffer);
            writer = new(stream);
            reader = new(stream);
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
                if (!NetIO.IsNewer(newTick, currPlayer.tick))
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
        }

        public void BeginWrite(OnlinePlayer toPlayer)
        {
            currPlayer = toPlayer;
            if (IsWriting || IsReading) throw new InvalidOperationException("not done with previous operation");
            IsWriting = true;
            IsDelta = false;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
        }

        private bool CanFit(OnlineEvent playerEvent)
        {
            return Position + playerEvent.EstimatedSize + margin < capacity;
        }

        private bool CanFit(OnlineState state)
        {
            return Position + state.EstimatedSize(this) + margin < capacity;
        }

        private void BeginWriteEvents()
        {
            eventCount = 0;
            eventLog = new(64);
            eventHeader = stream.Position;
            writer.Write(eventCount); // fake write, we'll overwrite this later
        }

        private void WriteEvent(OnlineEvent playerEvent)
        {
            eventCount++;
            long prevPos = (int)Position;
            writer.Write((byte)playerEvent.eventType);
            playerEvent.CustomSerialize(this);
            eventLog.AppendLine(playerEvent.ToString());
            long effectiveSize = Position - prevPos;
            string msg = $"size:{effectiveSize} est:{playerEvent.EstimatedSize}";
            if (warnOnSizeMissmatch && (effectiveSize != playerEvent.EstimatedSize)) RainMeadow.Error(msg);
            eventLog.AppendLine(msg);
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

        private void WriteState(OnlineState state)
        {
            stateCount++;
            state.WritePolymorph(this);
            WrappedSerialize(state);
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
            if (!IsWriting) throw new InvalidOperationException("not writing");
            IsWriting = false;
            writer.Flush();
        }

        private void BeginRead(OnlinePlayer fromPlayer)
        {
            currPlayer = fromPlayer;
            if (IsWriting || IsReading) throw new InvalidOperationException("not done with previous operation");
            IsReading = true;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
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
            if(s is RootDeltaState ps)
            {
                ps.from = currPlayer;
                ps.tick = currPlayer.tick;
            }
            
            WrappedSerialize(s);
            return s;
        }

        public void EndRead()
        {
            currPlayer = null;
            IsReading = false;
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
            //RainMeadow.Debug($"Receiving {ne} events from player {fromPlayer}");
            for (uint ie = 0; ie < ne; ie++)
            {
                OnlineManager.ProcessIncomingEvent(ReadEvent());
            }

            uint ns = BeginReadStates();
            fromPlayer.statesRead = ns > 0; // something is being read, record for debug
            //RainMeadow.Debug($"Receiving {ns} states");
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
                if (CanFit(e))
                {
                    WriteEvent(e);
                    //RainMeadow.Debug($"Wrote {e}");
                }
                else
                {
                    RainMeadow.Error("no buffer space for events");
                    RainMeadow.Error(eventLog.ToString());
                    break;
                }
            }
            EndWriteEvents();

            BeginWriteStates();
            toPlayer.statesWritten = toPlayer.OutgoingStates.Count > 0; // something is being written, record for debug
            //RainMeadow.Debug($"Writing {toPlayer.OutgoingStates.Count} states");
            while (toPlayer.OutgoingStates.Count > 0 && CanFit(toPlayer.OutgoingStates.Peek()))
            {
                var s = toPlayer.OutgoingStates.Dequeue();
                WriteState(s);
            }
            // todo handle states overflow, planing a packet for maximum size and least stale states
            EndWriteStates();

            EndWrite();

            toPlayer.bytesOut[toPlayer.bytesSnapIndex] = (int)Position;
            return Position;
        }

        // serializes resource.id and finds reference
        public void SerializeResourceByReference<T>(ref T onlineResource) where T : OnlineResource
        {
#if TRACING
            #if TRACING
            long wasPos = this.Position;
#endif
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

        // serializes resource.id and finds reference
        public void SerializEntityById(ref OnlineEntity onlineEntity)
        {
            if (IsWriting)
            {
                onlineEntity.id.CustomSerialize(this);
            }
            if (IsReading)
            {
                OnlineEntity.EntityId id = new();
                id.CustomSerialize(this);
                onlineEntity = id.FindEntity();
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
                if(state is RootDeltaState ps)
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

        public void SerializePolyStates<T>(ref T[] states) where T : OnlineState
        {
            if (IsWriting)
            {
                // TODO dynamic length
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

        public void SerializePolyStates<T>(ref List<T> states) where T : OnlineState
        {
            if (IsWriting)
            {
                // TODO dynamic length
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

        public void SerializeStaticStates<T>(ref T[] states) where T : OnlineState, new()
        {
            if (IsWriting)
            {
                // TODO dynamic length
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
                    MeadowPlayerId s = MatchmakingManager.instance.GetEmptyId();
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