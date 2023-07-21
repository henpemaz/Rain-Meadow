using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace RainMeadow
{
    public partial class Serializer
    {
        public readonly byte[] buffer;
        private readonly long capacity;
        private long margin;
        public long Position => stream.Position;

        private bool isWriting { get; set; }
        private bool isReading { get; set; }
        private bool Aborted { get; set; }

        private MemoryStream stream;
        private BinaryWriter writer;
        private BinaryReader reader;
        private OnlinePlayer currPlayer;
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
            if (isWriting)
            {
                writer.Write(currPlayer.lastEventFromRemote);
                writer.Write(currPlayer.tick);
                writer.Write(LobbyManager.mePlayer.tick);
            }
            if (isReading)
            {
                currPlayer.EventAckFromRemote(reader.ReadUInt64());
                currPlayer.TickAckFromRemote(reader.ReadUInt64());
                var newTick = reader.ReadUInt64();
                if (!OnlineManager.IsNewer(newTick, currPlayer.tick))
                {
                    // abort reading
                    AbortRead();
                }
                else
                {
                    currPlayer.tick = newTick;
                }
            }
        }

        private void AbortRead()
        {
            RainMeadow.Debug("aborted read");
            currPlayer = null;
            isReading = false;
            Aborted = true;
        }

        private void BeginWrite(OnlinePlayer toPlayer)
        {
            currPlayer = toPlayer;
            if (isWriting || isReading) throw new InvalidOperationException("not done with previous operation");
            isWriting = true;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
        }

        private bool CanFit(OnlineEvent playerEvent)
        {
            return Position + playerEvent.EstimatedSize + margin < capacity;
        }

        private bool CanFit(OnlineState state)
        {
            return Position + state.EstimatedSize + margin < capacity;
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
            writer.Write((byte)state.stateType);
            state.CustomSerialize(this);
        }

        private void EndWriteStates()
        {
            var temp = stream.Position;
            stream.Position = stateHeader;
            writer.Write(stateCount);
            stream.Position = temp;
        }

        private void EndWrite()
        {
            //RainMeadow.Debug($"serializer wrote: {eventCount} events; {stateCount} states; total {stream.Position} bytes");
            currPlayer = null;
            if (!isWriting) throw new InvalidOperationException("not writing");
            isWriting = false;
            writer.Flush();
        }

        private void BeginRead(OnlinePlayer fromPlayer)
        {
            currPlayer = fromPlayer;
            if (isWriting || isReading) throw new InvalidOperationException("not done with previous operation");
            isReading = true;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
        }

        private int BeginReadEvents()
        {
            return reader.ReadInt32();
        }

        private OnlineEvent ReadEvent()
        {
            OnlineEvent e = OnlineEvent.NewFromType((OnlineEvent.EventTypeId)reader.ReadByte());
            e.from = currPlayer;
            e.to = LobbyManager.mePlayer;
            e.CustomSerialize(this);
            return e;
        }

        private int BeginReadStates()
        {
            return reader.ReadInt32();
        }

        private OnlineState ReadState()
        {
            OnlineState s = OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
            s.from = currPlayer;
            s.tick = currPlayer.tick;
            s.CustomSerialize(this);
            return s;
        }

        public void EndRead()
        {
            currPlayer = null;
            isReading = false;
        }

        public void ReadData(OnlinePlayer fromPlayer, long size)
        {
            BeginRead(fromPlayer);

            PlayerHeaders();
            if (Aborted)
            {
                RainMeadow.Debug("skipped packet");
                return;
            }

            int ne = BeginReadEvents();
            //RainMeadow.Debug($"Receiving {ne} events");
            for (int ie = 0; ie < ne; ie++)
            {
                OnlineManager.ProcessIncomingEvent(ReadEvent());
            }

            int ns = BeginReadStates();
            //RainMeadow.Debug($"Receiving {ns} states");
            for (int ist = 0; ist < ns; ist++)
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
            //RainMeadow.Debug($"Writing {toPlayer.OutgoingEvents.Count} events");
            foreach (var e in toPlayer.OutgoingEvents)
            {
                if (CanFit(e))
                {
                    WriteEvent(e);
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
            //RainMeadow.Debug($"Writing {toPlayer.OutgoingStates.Count} states");
            while (toPlayer.OutgoingStates.Count > 0 && CanFit(toPlayer.OutgoingStates.Peek()))
            {
                var s = toPlayer.OutgoingStates.Dequeue();
                WriteState(s);
            }
            // todo handle states overflow, planing a packet for maximum size and least stale states
            EndWriteStates();

            EndWrite();

            return Position;
        }

        // serializes resource.id and finds reference
        public void Serialize(ref OnlineResource onlineResource)
        {
            if (isWriting)
            {
                // todo switch to bytes?
                writer.Write(onlineResource.Id());
            }
            if (isReading)
            {
                string r = reader.ReadString();
                onlineResource = OnlineManager.ResourceFromIdentifier(r);
            }
        }

        // Polymorphic - serializes its type and instantiates from it
        public void SerializePolyState<T>(ref T state) where T : OnlineState
        {
            if (isWriting)
            {
                writer.Write((byte)state.stateType);
                state.CustomSerialize(this);
            }
            if (isReading)
            {
                state = (T)OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
                state.from = currPlayer;
                state.tick = currPlayer.tick;
                state.CustomSerialize(this);
            }
        }

        public void SerializeNullablePolyState<T>(ref T nullableState) where T : OnlineState
        {
            if (isWriting)
            {
                writer.Write(nullableState != null);
                if (nullableState != null)
                {
                    SerializePolyState(ref nullableState);
                }
            }
            if (isReading)
            {
                if (reader.ReadBoolean())
                {
                    SerializePolyState(ref nullableState);
                }
            }
        }

        public void SerializePolyStates<T>(ref T[] states) where T : OnlineState
        {
            if (isWriting)
            {
                // TODO dynamic length
                if (states.Length > 255) throw new OverflowException("too many states");
                writer.Write((byte)states.Length);
                foreach (var state in states)
                {
                    writer.Write((byte)state.stateType);
                    state.CustomSerialize(this);
                }
            }
            if (isReading)
            {
                byte count = reader.ReadByte();
                states = new T[count];
                for (int i = 0; i < count; i++)
                {
                    var s = OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
                    s.from = currPlayer;
                    s.tick = currPlayer.tick;
                    s.CustomSerialize(this);
                    states[i] = s as T; // can throw an invalid cast? or will it just be null?
                }
            }
        }

        // Static - fixed type
        public void SerializeStaticState<T>(ref T state) where T : OnlineState, new()
        {
            if (isWriting)
            {
                state.CustomSerialize(this);
            }
            if (isReading)
            {
                state = new();
                state.from = currPlayer;
                state.tick = currPlayer.tick;
                state.CustomSerialize(this);
            }
        }

        public void SerializeNullableStaticState<T>(ref T nullableState) where T : OnlineState, new()
        {
            if (isWriting)
            {
                writer.Write(nullableState != null);
                if (nullableState != null)
                {
                    SerializeStaticState(ref nullableState);
                }
            }
            if (isReading)
            {
                if (reader.ReadBoolean())
                {
                    SerializeStaticState(ref nullableState);
                }
            }
        }

        public void SerializeStaticStates<T>(ref T[] states) where T : OnlineState, new()
        {
            if (isWriting)
            {
                // TODO dynamic length
                if (states.Length > 255) throw new OverflowException("too many states");
                writer.Write((byte)states.Length);
                foreach (var state in states)
                {
                    state.CustomSerialize(this);
                }
            }
            if (isReading)
            {
                byte count = reader.ReadByte();
                states = new T[count];
                for (int i = 0; i < count; i++)
                {
                    T s = new();
                    s.from = currPlayer;
                    s.tick = currPlayer.tick;
                    s.CustomSerialize(this);
                    states[i] = s;
                }
            }
        }

        // a referenced event is something that must have been ack'd that frame
        public void SerializeReferencedEvent(ref OnlineEvent referencedEvent)
        {
            if (isWriting)
            {
                writer.Write(referencedEvent.eventId);
            }
            if (isReading)
            {
                referencedEvent = currPlayer.GetRecentEvent(reader.ReadUInt64());
            }
        }

        internal void SerializeEvent<T>(ref T playerEvent) where T : OnlineEvent
        {
            if (isWriting)
            {
                writer.Write((byte)playerEvent.eventType);
                playerEvent.CustomSerialize(this);
            }
            if (isReading)
            {
                playerEvent = (T)OnlineEvent.NewFromType((OnlineEvent.EventTypeId)reader.ReadByte());
                playerEvent.from = currPlayer;
                playerEvent.to = LobbyManager.mePlayer;
                playerEvent.CustomSerialize(this);
            }
        }
    }
}