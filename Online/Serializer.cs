using MoreSlugcats;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class Serializer
    {
        public readonly byte[] buffer;
        public readonly long capacity;
        private long margin;
        public long Position => stream.Position;

        public bool isWriting { get; private set; }
        public bool isReading { get; private set; }
        public bool Aborted { get; private set; }

        MemoryStream stream;
        BinaryWriter writer;
        BinaryReader reader;
        private OnlinePlayer currPlayer;
        private int eventCount;
        private long eventHeader;
        private int stateCount;
        private long stateHeader;

        public Serializer(long bufferCapacity) 
        {
            this.capacity = bufferCapacity;
            margin = (long)(bufferCapacity * 0.25f);
            buffer = new byte[bufferCapacity];
            stream = new(buffer);
            writer = new(stream);
            reader = new(stream);
        }

        internal void PlayerHeaders()
        {
            if (isWriting)
            {
                writer.Write(currPlayer.lastEventFromRemote);
                writer.Write(currPlayer.tick);
                writer.Write(OnlineManager.mePlayer.tick);
            }
            if (isReading)
            {
                currPlayer.AckFromRemote(reader.ReadUInt64());
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

        internal void BeginWrite(OnlinePlayer toPlayer)
        {
            currPlayer = toPlayer;
            if (isWriting || isReading) throw new InvalidOperationException("not done with previous operation");
            isWriting = true;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
        }

        internal bool CanFit(PlayerEvent playerEvent)
        {
            return Position + playerEvent.EstimatedSize + margin < capacity;
        }

        internal bool CanFit(OnlineState state)
        {
            return Position + state.EstimatedSize + margin < capacity;
        }

        internal void BeginWriteEvents()
        {
            eventCount = 0;
            eventHeader = stream.Position;
            writer.Write(eventCount);
        }

        internal void WriteEvent(PlayerEvent playerEvent)
        {
            eventCount++;
            writer.Write((byte)playerEvent.eventType);
            playerEvent.CustomSerialize(this);
        }

        internal void EndWriteEvents()
        {
            var temp = stream.Position;
            stream.Position = eventHeader;
            writer.Write(eventCount);
            stream.Position = temp;
        }

        internal void BeginWriteStates()
        {
            stateCount = 0;
            stateHeader = stream.Position;
            writer.Write(stateCount);
        }

        internal void WriteState(OnlineState state)
        {
            stateCount++;
            writer.Write((byte)state.stateType);
            state.CustomSerialize(this);
        }

        internal void EndWriteStates()
        {
            var temp = stream.Position;
            stream.Position = stateHeader;
            writer.Write(stateCount);
            stream.Position = temp;
        }

        internal void EndWrite()
        {
            //RainMeadow.Debug($"serializer wrote: {eventCount} events; {stateCount} states; total {stream.Position} bytes");
            currPlayer = null;
            if (!isWriting) throw new InvalidOperationException("not writing");
            isWriting = false;
            writer.Flush();
        }

        internal void BeginRead(OnlinePlayer fromPlayer)
        {
            currPlayer = fromPlayer;
            if (isWriting || isReading) throw new InvalidOperationException("not done with previous operation");
            isReading = true;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
        }

        internal int BeginReadEvents()
        {
            return reader.ReadInt32();
        }

        internal PlayerEvent ReadEvent()
        {
            PlayerEvent e = PlayerEvent.NewFromType((PlayerEvent.EventTypeId)reader.ReadByte());
            e.from = currPlayer;
            e.to = OnlineManager.mePlayer;
            e.CustomSerialize(this);
            return e;
        }

        internal int BeginReadStates()
        {
            return reader.ReadInt32();
        }

        internal OnlineState ReadState()
        {
            OnlineState s = OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
            s.fromPlayer = currPlayer;
            s.ts = currPlayer.tick;
            s.CustomSerialize(this);
            return s;
        }

        internal void EndRead()
        {
            currPlayer = null;
            isReading = false;
        }

        internal void Free()
        {
            // unused
        }

        internal void Serialize(ref ulong uLong)
        {
            if (isWriting) writer.Write(uLong);
            if (isReading) uLong = reader.ReadUInt64();
        }

        internal void Serialize(ref OnlinePlayer player)
        {
            if (isWriting)
            {
                writer.Write(player is { } ? (ulong)player.id : 0ul);
            }
            if (isReading)
            {
                player = OnlineManager.PlayerFromId(new CSteamID(reader.ReadUInt64()));
            }
        }

        internal void Serialize(ref OnlineResource onlineResource)
        {
            if (isWriting)
            {
                // todo switch to bytes?
                writer.Write(onlineResource.Identifier());
            }
            if (isReading)
            {
                string r = reader.ReadString();
                onlineResource = OnlineManager.ResourceFromIdentifier(r);
            }
        }

        internal void Serialize(ref List<OnlinePlayer> players)
        {
            if (isWriting)
            {
                writer.Write((byte)players.Count);
                foreach (var player in players)
                {
                    writer.Write(player is { } ? (ulong)player.id : 0ul);
                }
            }
            if (isReading)
            {
                byte count = reader.ReadByte();
                players = new List<OnlinePlayer>(count);
                for (int i = 0; i < count; i++)
                {
                    players.Add(OnlineManager.PlayerFromId(new CSteamID(reader.ReadUInt64())));
                }
            }
        }

        // a referenced event is something that must have been ack'd that frame
        internal void SerializeReferencedEvent(ref ResourceEvent referencedEvent)
        {
            if(isWriting)
            {
                writer.Write(referencedEvent.eventId);
            }
            if (isReading)
            {
                referencedEvent = (ResourceEvent)currPlayer.GetRecentEvent(reader.ReadUInt64());
            }
        }

        internal void Serialize(ref OnlineResource.LeaseState leaseState)
        {
            if (isWriting)
            {
                writer.Write((byte)leaseState.ownership.Count);
                foreach (var item in leaseState.ownership)
                {
                    writer.Write(item.Key); writer.Write(item.Value);
                }
            }
            if (isReading)
            {
                leaseState = new();
                var count = reader.ReadByte();
                leaseState.ownership = new(count);
                for (int i = 0; i < count; i++)
                {
                    leaseState.ownership[reader.ReadString()] = reader.ReadUInt64();
                }
            }
        }

        internal void Serialize(ref int data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadInt32();
        }

        internal void Serialize(ref bool data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadBoolean();
        }

        internal void Serialize(ref string data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadString();
        }

        internal void SerializeNoStrings(ref WorldCoordinate pos)
        {
            if (isWriting)
            {
                writer.Write((short)pos.room);
                writer.Write((short)pos.x);
                writer.Write((short)pos.y);
                writer.Write((short)pos.abstractNode);
            }
            if(isReading)
            {
                pos = new WorldCoordinate()
                {
                    room = reader.ReadInt16(),
                    x = reader.ReadInt16(),
                    y = reader.ReadInt16(),
                    abstractNode = reader.ReadInt16(),
                };
            }
        }

        internal void Serialize(ref OnlineState[] states)
        {
            if (isWriting)
            {
                // TODO dynamic length
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
                states = new OnlineState[count];
                for (int i = 0; i < count; i++)
                {
                    var s = OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
                    s.fromPlayer = currPlayer;
                    s.ts = currPlayer.tick;
                    s.CustomSerialize(this);
                    states[i] = s;
                }
            }
        }

        // watch this throw lots!
        // ideally newentities are delivered through events, so they should already exist when states come
        internal void Serialize(ref OnlineEntity onlineEntity)
        {
            if (isWriting)
            {
                writer.Write((ulong)onlineEntity.owner.id);
                writer.Write(onlineEntity.id);
            }
            if (isReading)
            {
                var player = OnlineManager.PlayerFromId(new CSteamID(reader.ReadUInt64()));
                var id = reader.ReadInt32();
                onlineEntity = player.recentEntities[id];
            }
        }

        internal void SerializeNullable(ref OnlineState nullableState)
        {
            if (isWriting)
            {
                writer.Write(nullableState != null);
                if(nullableState != null)
                {
                    Serialize(ref nullableState);
                }
            }
            if (isReading)
            {
                if (reader.ReadBoolean())
                {
                    Serialize(ref nullableState);
                }
            }
        }

        private void Serialize(ref OnlineState state)
        {
            if (isWriting)
            {
                writer.Write((byte)state.stateType);
                state.CustomSerialize(this);
            }
            if (isReading)
            {
                state = OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
                state.fromPlayer = currPlayer;
                state.ts = currPlayer.tick;
                state.CustomSerialize(this);
            }
        }

        internal void Serialize(ref Vector2 data)
        {
            if (isWriting)
            {
                writer.Write(data.x);
                writer.Write(data.y);
            }
            if (isReading)
            {
                data.x = reader.ReadSingle();
                data.y = reader.ReadSingle();
            }
        }

        // todo generics for crap like this
        internal void Serialize(ref OnlineEntity.ChunkState[] chunkStates)
        {
            if (isWriting)
            {
                // TODO dynamic length
                writer.Write((byte)chunkStates.Length);
                foreach (var state in chunkStates)
                {
                    state.CustomSerialize(this);
                }
            }
            if (isReading)
            {
                byte count = reader.ReadByte();
                chunkStates = new OnlineEntity.ChunkState[count];
                for (int i = 0; i < count; i++)
                {
                    var s = new OnlineEntity.ChunkState(null);
                    s.CustomSerialize(this);
                    chunkStates[i] = s;
                }
            }
        }
    }
}