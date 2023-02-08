using Steamworks;

namespace RainMeadow
{
    public abstract class ResourceEvent : PlayerEvent
    {
        public override long EstimatedSize => base.EstimatedSize + 2*sizeof(ulong) + onlineResource.SizeOfIdentifier();
        public OnlinePlayer from;
        public OnlinePlayer to;
        public OnlineResource onlineResource;

        public ResourceEvent(OnlinePlayer from, OnlinePlayer to, OnlineResource onlineResource)
        {
            this.from = from;
            this.to = to;
            this.onlineResource = onlineResource;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref from);
            serializer.Serialize(ref to);
            serializer.Serialize(ref onlineResource);
        }
    }
}