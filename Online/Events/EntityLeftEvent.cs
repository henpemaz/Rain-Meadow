namespace RainMeadow
{
    public class EntityLeftEvent : EntityResourceEvent
    {
        public EntityLeftEvent() { }

        public EntityLeftEvent(OnlineResource resource, OnlineEntity oe) : base(resource, oe.id) { }

        public override EventTypeId eventType => EventTypeId.EntityLeftEvent;

        public override void Process()
        {
            onlineResource.OnEntityLeft(this);
        }
    }
}