using Steamworks;
using System;

namespace RainMeadow
{
    public abstract class RequestEvent : PlayerEvent
    {
        public override long EstimatedSize => base.EstimatedSize + onlineResource.SizeOfIdentifier();
        public OnlineResource onlineResource;

        public RequestEvent(OnlineResource onlineResource)
        {
            this.onlineResource = onlineResource;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref onlineResource);
        }

        internal abstract void Resolve(ResultEvent resultEvent);
    }
}