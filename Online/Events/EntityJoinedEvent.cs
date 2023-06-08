namespace RainMeadow
{
    public class EntityJoinedEvent : EntityResourceEvent
    {
        public EntityState initialState;
        public EntityJoinedEvent() { }
        public EntityJoinedEvent(OnlineResource onlineResource, OnlineEntity oe, TickReference tickReference) : base(onlineResource, oe.id, tickReference)
        {
            initialState = oe.GetState(oe.owner.tick, onlineResource);
        }

        public override EventTypeId eventType => EventTypeId.EntityJoinedEvent;

        public override void Process()
        {
            onlineResource.OnEntityJoined(this);
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializePolyState(ref initialState);
        }
    }
}