using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.IO;

namespace RainMeadow
{
    public class Serializer
    {
        public readonly byte[] buffer;
        public readonly int capacity;
        private long margin;
        public long Position => stream.Position;

        public bool isWriting { get; private set; }
        public bool isReading { get; private set; }

        MemoryStream stream;
        BinaryWriter writer;
        BinaryReader reader;
        private Dictionary<Type, PlayerEvent.EventTypeId> eventTypeIdsByType;

        public Serializer(int bufferCapacity) 
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

        internal void WriteEvent(PlayerEvent e)
        {
            writer.Write(GetEventTypeId(e));
        }

        private byte GetEventTypeId(PlayerEvent e)
        {
            return eventTypeIdsByType[e.GetType()];
        }

        internal void Serialize(ref PlayerEvent e)
        {
            if (isWriting)
            {

            }
            if (isReading)
            {
                
            }
            e.CustomSerialize(this);
        }
    }
}