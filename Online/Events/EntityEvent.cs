namespace RainMeadow
{
    public abstract class EntityEvent : ResourceEvent
    {
        public int entityId;
        public OnlinePlayer owner;

        protected EntityEvent() : base(null) { }

        protected EntityEvent(RoomSession roomSession, OnlineEntity oe) : base(roomSession)
        {
            this.entityId = oe.id;
            this.owner = oe.owner;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref owner);
            serializer.Serialize(ref entityId);
        }
    }
}