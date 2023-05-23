namespace RainMeadow
{
    internal class EntityJoinedEvent : EntityResourceEvent
    {
        public EntityJoinedEvent() { }
        public EntityJoinedEvent(OnlineResource onlineResource, OnlineEntity oe, TickReference tickReference) : base(onlineResource, oe.id, tickReference) { }

        public override EventTypeId eventType => EventTypeId.EntityJoinedEvent;

        public override void Process()
        {
            onlineResource.OnEntityJoined(this);
        }
    }
}