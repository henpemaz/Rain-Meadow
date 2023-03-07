namespace RainMeadow
{
    internal class EntityReleaseEvent : EntityEvent
    {
        public EntityReleaseEvent() { }

        public EntityReleaseEvent(OnlineEntity oe) : base(oe) { }

        public override EventTypeId eventType => EventTypeId.EntityReleaseEvent;

        internal override void Process()
        {
            oe.Released(this);
        }
    }
}