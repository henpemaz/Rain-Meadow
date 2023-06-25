namespace RainMeadow
{
    public abstract class NewEntityEvent : EntityJoinedEvent
    {
        public OnlinePlayer owner;
        public bool isTransferable;

        public NewEntityEvent() { }

        public NewEntityEvent(OnlineResource resource, OnlineEntity oe, TickReference tickReference) : base(resource, oe, tickReference)
        {
            owner = oe.owner;
            isTransferable = oe.isTransferable;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref owner);
            serializer.Serialize(ref isTransferable);
        }

        public override void Process()
        {
            this.onlineResource.OnNewRemoteEntity(this);
        }
    }
}