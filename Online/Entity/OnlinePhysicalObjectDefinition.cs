namespace RainMeadow
{
    public class OnlinePhysicalObjectDefinition : EntityDefinition
    {
        [OnlineField]
        public int seed;
        [OnlineField]
        public bool realized;
        [OnlineField]
        public string serializedObject;

        public OnlinePhysicalObjectDefinition() { }
        public OnlinePhysicalObjectDefinition(int seed, bool realized, string serializedObject, OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable) : base(entityId, owner, isTransferable)
        {
            this.seed = seed;
            this.realized = realized;
            this.serializedObject = serializedObject;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlinePhysicalObject.FromDefinition(this, inResource);
        }
    }
}