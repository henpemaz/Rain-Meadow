namespace RainMeadow
{
    public class LobbyLeaseChangeEvent : ResourceEvent
    {
        public OnlineResource.LobbyLeaseState leaseState;

        public LobbyLeaseChangeEvent() { }

        public LobbyLeaseChangeEvent(OnlineResource onlineResource, OnlineResource.LeaseState leaseState, TickReference dependsOnTick) : base(onlineResource)
        {
            this.leaseState = (OnlineResource.LobbyLeaseState)leaseState;
            this.dependsOnTick = dependsOnTick;
        }

        public override EventTypeId eventType => EventTypeId.LobbyLeaseChange;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref leaseState);
            serializer.SerializeNullable(ref dependsOnTick);
        }

        public override void Process()
        {
            this.onlineResource.OnLobbyLeaseChange(this);
        }
    }
}