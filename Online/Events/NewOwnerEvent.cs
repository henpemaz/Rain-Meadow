namespace RainMeadow
{
    internal class NewOwnerEvent : ResourceEvent
    {
        public OnlinePlayer newOwner;

        public NewOwnerEvent(OnlineResource onlineResource, OnlinePlayer newOwner) : base(onlineResource)
        {
            this.newOwner = newOwner;
        }

        public override EventTypeId eventType => EventTypeId.NewOwnerEvent;

        internal override void Process()
        {
            onlineResource.NewOwner(this);
        }
    }
}