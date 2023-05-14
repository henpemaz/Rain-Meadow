namespace RainMeadow
{
    public abstract class NewEntityEvent : EntityResourceEvent
    {
        public OnlinePlayer owner;
        public bool isTransferable;

        public NewEntityEvent() { }

        public NewEntityEvent(OnlineResource resource, OnlineEntity oe, PlayerTickReference tickReference) : base(resource, oe.id, tickReference)
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