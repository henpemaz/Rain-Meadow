namespace RainMeadow
{
    public class NewEntityEvent : EntityResourceEvent
    {
        public WorldCoordinate initialPos;
        public bool isCreature;
        public string template = "";

        public NewEntityEvent() : base() { }

        public NewEntityEvent(OnlineResource resource, OnlineEntity oe) : base(resource, oe)
        {
            this.initialPos = oe.initialPos;
            isCreature = oe.entity is AbstractCreature;
            template = (oe.entity as AbstractCreature)?.creatureTemplate.type.ToString() ?? "";
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
        }

        internal override void Process()
        {
            this.onlineResource.OnNewEntity(this);
        }
    }
}