namespace RainMeadow
{
    public abstract class EntityResourceEvent : ResourceEvent
    {
        public OnlineEntity.EntityId entityId;
        protected EntityResourceEvent() { }

        protected EntityResourceEvent(OnlineResource resource, OnlineEntity.EntityId entityId, PlayerTickReference tickReference) : base(resource)
        {
            this.entityId = entityId;
            this.dependsOnTick = tickReference;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref entityId);
            serializer.SerializeNullable(ref dependsOnTick);
        }
    }
}