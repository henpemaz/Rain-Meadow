namespace RainMeadow
{
    public class EntityLeftEvent : EntityResourceEvent
    {
        public EntityLeftEvent() { }

        public EntityLeftEvent(OnlineResource resource, OnlineEntity oe, PlayerTickReference tickReference) : base(resource, oe.id, tickReference) { }

        public override EventTypeId eventType => EventTypeId.EntityLeftEvent;

        public override void Process()
        {
            onlineResource.OnEntityLeft(this);
        }
    }
}