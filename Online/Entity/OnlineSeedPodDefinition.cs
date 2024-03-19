namespace RainMeadow
{ 
    public class OnlineSeedCobDefinition : OnlineConsumableDefinition
    {
        [OnlineField]
        public bool originallyDead;
        [OnlineField]
        public string roomName;

        public OnlineSeedCobDefinition() { }

        public OnlineSeedCobDefinition(OnlineConsumableDefinition ocd, SeedCob.AbstractSeedCob abstractSeedCob) : base (ocd,abstractSeedCob)
        {
            this.originallyDead = abstractSeedCob.dead;
            this.roomName = abstractSeedCob.Room.name;
        }

        public OnlineSeedCobDefinition(int seed, bool realized, string serializedObject, OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable, short originRoom, sbyte placedObjectIndex, bool originallyConsumed, string roomName, bool originallyDead) : base(seed, realized, serializedObject, entityId, owner, isTransferable, originRoom, placedObjectIndex, originallyConsumed)
        {
            this.originallyDead = originallyDead;
            this.roomName = roomName;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlineSeedCob.FromDefinition(this, inResource);
        }
    }
}
