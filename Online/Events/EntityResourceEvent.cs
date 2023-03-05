namespace RainMeadow
{
    public abstract class EntityResourceEvent : ResourceEvent
    {
        public OnlinePlayer owner;
        public int entityId;

        protected EntityResourceEvent() : base(null) { }

        protected EntityResourceEvent(OnlineResource resource, OnlinePlayer owner, int entityId) : base(resource)
        {
            this.owner = owner;
            this.entityId = entityId;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref owner);
            serializer.Serialize(ref entityId);
        }
    }
}