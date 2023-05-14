namespace RainMeadow
{
    public abstract class old_NewEntityEvent : EntityResourceEvent
    {
        public OnlinePlayer owner;
        public bool realized;
        public bool isTransferable;
        public bool isCreature;
        public string template = "";
        public WorldCoordinate initialPos;
        public int seed;
        public string saveString;

        public old_NewEntityEvent() { }

        public old_NewEntityEvent(OnlineResource resource, OnlineEntity oe, PlayerTickReference tickReference) : base(resource, oe.id, tickReference)
        {
            owner = oe.owner;
            realized = oe.realized;
            isTransferable = oe.isTransferable;
            isCreature = oe.entity is AbstractCreature;
            initialPos = oe.enterPos;
            seed = oe.seed;

            if (isCreature)
            {
                var crit = (AbstractCreature)oe.entity;
                template = crit.creatureTemplate.type.ToString();
                saveString = crit.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.Slugcat ? SaveState.AbstractCreatureToStringStoryWorld(crit) : ""; //todo: fix loading and serializing players?
            }
            else
            {
                saveString = oe.entity.ToString();
            }

        }

        public override EventTypeId eventType => EventTypeId.old_NewEntityEvent;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref owner);
            serializer.Serialize(ref realized);
            serializer.Serialize(ref isTransferable);
            serializer.Serialize(ref isCreature);
            if (isCreature)
            {
                serializer.Serialize(ref template);
            }
            serializer.SerializeNoStrings(ref initialPos);
            serializer.Serialize(ref seed);
            serializer.Serialize(ref saveString);
        }

        public override void Process()
        {
            this.onlineResource.old_OnNewEntity(this);
        }
    }
}