namespace RainMeadow
{
    public class NewEntityEvent : ResourceEvent
    {
        public OnlinePlayer owner;
        public int entityId;
        public bool isCreature;
        public string template = "";
        public WorldCoordinate pos;

        public NewEntityEvent() : base(null) // serialization friendly I guess
        {
        }

        public NewEntityEvent(RoomSession roomSession, RoomSession.OnlineEntity oe, WorldCoordinate pos) : base(roomSession)
        {
            owner = oe.owner;
            entityId = oe.id;
            isCreature = oe.entity is AbstractCreature;
            template = (oe.entity as AbstractCreature)?.creatureTemplate.type.ToString() ?? "";
            this.pos = pos;
        }

        public override EventTypeId eventType => EventTypeId.NewEntityEvent;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref owner);
            serializer.Serialize(ref entityId);
            serializer.Serialize(ref isCreature);
            if (isCreature)
            {
                serializer.Serialize(ref template);
            }
            serializer.SerializeNoStrings(ref pos);
        }

        internal override void Process()
        {
            (onlineResource as RoomSession).OnNewEntity(this);
        }
    }
}