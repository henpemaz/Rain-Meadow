using System;
using System.IO;

namespace RainMeadow
{
    public abstract class PlayerEvent
    {
        
        public ulong eventId;

        public virtual long EstimatedSize { get => sizeof(ulong); }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref eventId);
        }

        // enum and virtual property
        // or enum and type->enum dictionary?
        internal enum EventTypeId : byte
        {
            None
        }
    }
}