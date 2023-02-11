using System;
using System.IO;

namespace RainMeadow
{
    public abstract class PlayerEvent
    {
        public abstract EventTypeId eventType { get; }
        public ulong eventId;
        public OnlinePlayer from;// not serialized
        public OnlinePlayer to;// not serialized

        public virtual long EstimatedSize { get => sizeof(ulong); }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref eventId);
        }

        internal abstract void Process();

        // enum and virtual property
        // or enum and type->enum dictionary?
        public enum EventTypeId : byte
        {
            None,
            ResourceRequest,
            ReleaseRequest,
            TransferRequest,
            ReleaseResultReleased,
            ReleaseResultUnsubscribed,
            ReleaseResultError,
            RequestResultLeased,
            RequestResultSubscribed,
            RequestResultError,
            TransferResultError,
            TransferResultOk,
        }
    }
}