namespace RainMeadow
{
    public class OnlineCreatureDefinition : OnlinePhysicalObjectDefinition
    {
        public OnlineCreatureDefinition() { }
        public OnlineCreatureDefinition(int seed, bool realized, string serializedObject, OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable) : base(seed, realized, serializedObject, entityId, owner, isTransferable) { }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlineCreature.FromDefinition(this, inResource);
        }
    }
}