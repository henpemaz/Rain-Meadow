using MoreSlugcats;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
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
                writer.Write(OnlineManager.mePlayer.tick);
            }
            if (isReading)
            {
                currPlayer.AckFromRemote(reader.ReadUInt64());
                // todo check for unordered packets, drop accordingly
                currPlayer.tick = reader.ReadUInt64();
            }
        }

        internal void BeginWrite(OnlinePlayer toPlayer)
        {
            currPlayer = toPlayer;
            if (isWriting || isReading) throw new InvalidOperationException("not done with previous operation");
            isWriting = true;
            stream.Seek(0, SeekOrigin.Begin);
        }

        internal bool CanFit(PlayerEvent playerEvent)
        {
            return playerEvent.EstimatedSize + margin < capacity;
        }

        internal bool CanFit(ResourceState resourceState)
        {
            return resourceState.EstimatedSize + margin < capacity;
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

        internal void WriteState(ResourceState resourceState)
        {
            stateCount++;
            writer.Write((byte)resourceState.stateType);
            resourceState.CustomSerialize(this);
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
            RainMeadow.Debug($"serializer wrote: {eventCount} events; {stateCount} states;");
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

        internal ResourceState ReadState()
        {
            ResourceState s = ResourceState.NewFromType((ResourceState.ResourceStateType)reader.ReadByte());
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
                writer.Write((ulong)player.id);
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
            throw new NotImplementedException();
        }

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
    }
}