namespace RainMeadow
{
    internal class EntityNewOwnerEvent : EntityResourceEvent
    {
        public OnlinePlayer newOwner;
        public int newId;

        public EntityNewOwnerEvent() : base() { }

        public EntityNewOwnerEvent(OnlineResource onlineResource, OnlinePlayer wasOwner, int wasId, OnlinePlayer newOwner, int newId) : base(onlineResource, wasOwner, wasId)
        {
            this.newOwner = newOwner;
            this.newId = newId;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref newOwner);
            serializer.Serialize(ref newId);
        }

        public override EventTypeId eventType => EventTypeId.EntityNewOwnerEvent;

        internal override void Process()
        {
            onlineResource.OnEntityNewOwner(this);
        }
    }
}