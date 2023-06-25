namespace RainMeadow
{
    public class EntityTransferedEvent : EntityResourceEvent
    {
        public OnlinePlayer newOwner;

        public EntityTransferedEvent() { }

        public EntityTransferedEvent(OnlineResource onlineResource, OnlineEntity.EntityId entityId, OnlinePlayer newOwner, TickReference tickReference) : base(onlineResource, entityId, tickReference)
        {
            this.newOwner = newOwner;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref newOwner);
        }

        public override EventTypeId eventType => EventTypeId.EntityTransferedEvent;

        public override void Process()
        {
            onlineResource.OnEntityTransfered(this);
        }
    }
}