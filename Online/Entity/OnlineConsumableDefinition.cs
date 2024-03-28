namespace RainMeadow
{
    public class OnlineConsumableDefinition : OnlinePhysicalObjectDefinition
    {
        [OnlineField]
        public short originRoom;
        [OnlineField]
        public sbyte placedObjectIndex;
        [OnlineField]
        public bool originallyConsumed;

        public OnlineConsumableDefinition() { }
        public OnlineConsumableDefinition(OnlinePhysicalObjectDefinition opod, AbstractConsumable abstractConsumable) : base(opod)
        {
            this.originRoom = (short)abstractConsumable.originRoom;
            this.placedObjectIndex = (sbyte)abstractConsumable.placedObjectIndex;
            this.originallyConsumed = abstractConsumable.isConsumed;
        }
        public OnlineConsumableDefinition(int seed, bool realized, string serializedObject, OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable, short originRoom, sbyte placedObjectIndex, bool originallyConsumed) : base(seed, realized, serializedObject, entityId, owner, isTransferable)
        {
            this.originRoom = originRoom;
            this.placedObjectIndex = placedObjectIndex;
            this.originallyConsumed = originallyConsumed;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlineConsumable.FromDefinition(this, inResource);
        }
    }
}