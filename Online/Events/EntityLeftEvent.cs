namespace RainMeadow
{
    internal class EntityLeftEvent : EntityEvent
    {
        public EntityLeftEvent() : base() { }

        public EntityLeftEvent(RoomSession roomSession, OnlineEntity oe) : base(roomSession, oe) { }

        public override EventTypeId eventType => EventTypeId.EntityLeftEvent;

        internal override void Process()
        {
            (onlineResource as RoomSession).OnEntityLeft(this);
        }

        public override string ToString()
        {
            return $"{base.ToString()}:{this.owner}:{entityId}";
        }
    }
}