namespace RainMeadow
{
    public class LeaseChangeEvent : ResourceEvent
    {
        public OnlineResource.LeaseState leaseState;

        public LeaseChangeEvent() { }

        public LeaseChangeEvent(OnlineResource onlineResource, OnlineResource.LeaseState leaseState, TickReference dependsOnTick) : base(onlineResource)
        {
            this.leaseState = leaseState;
            this.dependsOnTick = dependsOnTick;
        }

        public override EventTypeId eventType => EventTypeId.LeaseChange;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref leaseState);
            serializer.SerializeNullable(ref dependsOnTick);
        }

        public override void Process()
        {
            this.onlineResource.OnLeaseChange(this);
        }
    }
}