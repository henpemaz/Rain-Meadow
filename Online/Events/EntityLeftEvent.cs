namespace RainMeadow
{
    internal class EntityLeftEvent : ResourceEvent
    {
        public OnlinePlayer owner;
        public int entityId;

        public EntityLeftEvent() : base(null) { } // serialization friendly I guess

        public EntityLeftEvent(RoomSession roomSession, OnlineEntity oe) : base(roomSession)
        {
            owner = oe.owner;
            entityId = oe.id;
        }

        public override EventTypeId eventType => EventTypeId.EntityLeftEvent;

        internal override void Process()
        {
            (onlineResource as RoomSession).OnEntityLeft(this);
        }
    }
}