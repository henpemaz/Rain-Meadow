namespace RainMeadow
{
    public abstract class EntityResourceEvent : ResourceEvent
    {
        public int entityId;
        public OnlinePlayer owner;

        protected EntityResourceEvent() : base(null) { }

        protected EntityResourceEvent(OnlineResource resource, OnlineEntity oe) : base(resource)
        {
            this.entityId = oe.id;
            this.owner = oe.owner;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref owner);
            serializer.Serialize(ref entityId);
        }
    }
}