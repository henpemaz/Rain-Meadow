namespace RainMeadow
{
    public class NewEntityEvent : ResourceEvent
    {
        public OnlinePlayer owner;
        public int entityId;
        public WorldCoordinate pos;
        public bool isCreature;
        public string template = "";

        public NewEntityEvent() : base(null) { } // serialization friendly I guess

        public NewEntityEvent(RoomSession roomSession, OnlineEntity oe) : base(roomSession)
        {
            owner = oe.owner;
            entityId = oe.id;
            pos = oe.pos;
            isCreature = oe.entity is AbstractCreature;
            template = (oe.entity as AbstractCreature)?.creatureTemplate.type.ToString() ?? "";
        }

        public override EventTypeId eventType => EventTypeId.NewEntityEvent;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref owner);
            serializer.Serialize(ref entityId);
            serializer.SerializeNoStrings(ref pos);
            serializer.Serialize(ref isCreature);
            if (isCreature)
            {
                serializer.Serialize(ref template);
            }
        }

        internal override void Process()
        {
            (onlineResource as RoomSession).OnNewEntity(this);
        }
    }
}