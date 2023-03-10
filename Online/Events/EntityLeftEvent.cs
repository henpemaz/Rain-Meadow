namespace RainMeadow
{
    internal class EntityLeftEvent : EntityResourceEvent
    {
        public EntityLeftEvent() { }

        public EntityLeftEvent(OnlineResource resource, OnlineEntity oe) : base(resource, oe.id) { }

        public override EventTypeId eventType => EventTypeId.EntityLeftEvent;

        internal override void Process()
        {
            onlineResource.OnEntityLeft(this);
        }
    }
}