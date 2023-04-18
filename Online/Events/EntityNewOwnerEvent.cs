namespace RainMeadow
{
    public class EntityNewOwnerEvent : EntityResourceEvent
    {
        public OnlinePlayer newOwner;

        public EntityNewOwnerEvent() { }

        public EntityNewOwnerEvent(OnlineResource onlineResource, OnlineEntity.EntityId entityId, OnlinePlayer newOwner) : base(onlineResource, entityId)
        {
            this.newOwner = newOwner;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref newOwner);
        }

        public override EventTypeId eventType => EventTypeId.EntityNewOwnerEvent;

        public override void Process()
        {
            onlineResource.OnEntityNewOwner(this);
        }
    }
}