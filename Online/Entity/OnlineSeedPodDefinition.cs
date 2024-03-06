namespace RainMeadow
{ 
    public class OnlineSeedPodDefinition : OnlineConsumableDefinition
    {
        [OnlineField]
        public bool originallyDead;
        [OnlineField]
        public string roomName;

        public OnlineSeedPodDefinition() { }

        public OnlineSeedPodDefinition(OnlineConsumableDefinition ocd, SeedCob.AbstractSeedCob abstractSeedCob) : base (ocd)
        {
            this.originallyDead = abstractSeedCob.dead;
            this.roomName = abstractSeedCob.Room.name;
        }

        public OnlineSeedPodDefinition(int seed, bool realized, string serializedObject, OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable, short originRoom, sbyte placedObjectIndex, bool originallyConsumed, string roomName, bool originallyDead) : base(seed, realized, serializedObject, entityId, owner, isTransferable, originRoom, placedObjectIndex, originallyConsumed)
        {
            this.originallyDead = originallyDead;
            this.roomName = roomName;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlineSeedPod.FromDefinition(this, inResource);
        }
    }
}
