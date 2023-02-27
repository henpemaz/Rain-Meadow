namespace RainMeadow
{
    internal class EntityLeftEvent : EntityEvent
    {
        public EntityLeftEvent() : base() { }

        public EntityLeftEvent(OnlineResource resource, OnlineEntity oe) : base(resource, oe) { }

        public override EventTypeId eventType => EventTypeId.EntityLeftEvent;

        internal override void Process()
        {
            onlineResource.OnEntityLeft(this);
        }

        public override string ToString()
        {
            return $"{base.ToString()}:{this.owner}:{entityId}";
        }
    }
}