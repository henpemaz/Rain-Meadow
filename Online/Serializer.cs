using MoreSlugcats;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;

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
        private Dictionary<Type, PlayerEvent.EventTypeId> eventTypeIdsByType;
        private int eventCount;
        private long eventHeader;

        public Serializer(long bufferCapacity) 
        {
            this.capacity = bufferCapacity;
            margin = (long)(bufferCapacity * 0.25f);
            buffer = new byte[bufferCapacity];
            stream = new(buffer);
            writer = new(stream);
            reader = new(stream);
        }

        internal void BeginWrite()
        {
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
            playerEvent.CustomSerialize(this);
        }

        internal void EndWriteEvents()
        {
            var temp = stream.Position;
            stream.Position = eventHeader;
            writer.Write(eventCount);
            stream.Position = temp;
        }
       
        internal void WriteState(ResourceState resourceState)
        {
            resourceState.CustomSerialize(this);
        }

        internal void EndWrite()
        {
            if (!isWriting) throw new InvalidOperationException("not writing");
            isWriting = false;
            writer.Flush();
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
                player = OnlineManager.PlayerFromId(reader.ReadUInt64());
            }
        }

        internal void Serialize(ref OnlineResource onlineResource)
        {
            throw new NotImplementedException();
        }

        internal void BeginWriteStates()
        {
            throw new NotImplementedException();
        }

        internal void EndWriteStates()
        {
            throw new NotImplementedException();
        }

        internal void BeginRead()
        {
            throw new NotImplementedException();
        }

        internal void EndRead()
        {
            throw new NotImplementedException();
        }

        internal void WriteHeaders(OnlinePlayer toPlayer)
        {
            throw new NotImplementedException();
        }

        internal void ReadHeaders(CSteamID fromPlayer)
        {
            throw new NotImplementedException();
        }

        internal int BeginReadEvents()
        {
            throw new NotImplementedException();
        }

        internal PlayerEvent ReadEvent()
        {
            throw new NotImplementedException();
        }

        internal int BeginReadStates()
        {
            throw new NotImplementedException();
        }

        internal ResourceState ReadState()
        {
            throw new NotImplementedException();
        }

        internal void ReadHeaders(OnlinePlayer fromPlayer)
        {
            throw new NotImplementedException();
        }
    }
}