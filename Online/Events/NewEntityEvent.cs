namespace RainMeadow
{
    public class NewEntityEvent : EntityResourceEvent
    {
        public WorldCoordinate initialPos;
        public bool isCreature;
        public string template = "";
        public int seed;

        public NewEntityEvent() { }

        public NewEntityEvent(OnlineResource resource, OnlineEntity oe) : base(resource, oe.owner, oe.id)
        {
            this.initialPos = oe.enterPos;
            isCreature = oe.entity is AbstractCreature;
            template = (oe.entity as AbstractCreature)?.creatureTemplate.type.ToString() ?? "";
            seed = oe.seed;
        }

        public override EventTypeId eventType => EventTypeId.NewEntityEvent;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeNoStrings(ref initialPos);
            serializer.Serialize(ref isCreature);
            if (isCreature)
            {
                serializer.Serialize(ref template);
            }
            serializer.Serialize(ref seed);
        }

        internal override void Process()
        {
            this.onlineResource.OnNewEntity(this);
        }
    }
}