namespace RainMeadow
{
    public abstract class EntityResourceEvent : ResourceEvent
    {
        public OnlineEntity.EntityId entityId;

        protected EntityResourceEvent() { }

        protected EntityResourceEvent(OnlineResource resource, OnlineEntity.EntityId entityId) : base(resource)
        {
            this.entityId = entityId;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref entityId);
        }
    }
}