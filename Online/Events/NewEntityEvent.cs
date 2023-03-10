namespace RainMeadow
{
    public class NewEntityEvent : EntityResourceEvent
    {
        public OnlinePlayer owner;
        public bool isTransferable;
        public bool isCreature;
        public string template = "";
        public WorldCoordinate initialPos;
        public int seed;

        public NewEntityEvent() { }

        public NewEntityEvent(OnlineResource resource, OnlineEntity oe) : base(resource, oe.id)
        {
            owner = oe.owner;
            isTransferable = oe.isTransferable;
            isCreature = oe.entity is AbstractCreature;
            template = (oe.entity as AbstractCreature)?.creatureTemplate.type.ToString() ?? "";
            initialPos = oe.enterPos;
            seed = oe.seed;
        }

        public override EventTypeId eventType => EventTypeId.NewEntityEvent;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref owner);
            serializer.Serialize(ref isTransferable);
            serializer.Serialize(ref isCreature);
            if (isCreature)
            {
                serializer.Serialize(ref template);
            }
            serializer.SerializeNoStrings(ref initialPos);
            serializer.Serialize(ref seed);
        }

        internal override void Process()
        {
            this.onlineResource.OnNewEntity(this);
        }
    }
}