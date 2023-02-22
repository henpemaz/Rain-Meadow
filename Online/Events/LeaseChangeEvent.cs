namespace RainMeadow
{
    internal class LeaseChangeEvent : ResourceEvent
    {
        private OnlineResource.LeaseState leaseState;

        public LeaseChangeEvent(OnlineResource onlineResource, OnlineResource.LeaseState leaseState) : base(onlineResource)
        {
            this.leaseState = leaseState;
        }

        public override EventTypeId eventType => EventTypeId.LeaseChange;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref leaseState);
        }

        internal override void Process()
        {
            this.onlineResource.LeaseChange(leaseState);
        }
    }
}