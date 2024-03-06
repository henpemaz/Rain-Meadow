using static RainMeadow.OnlineState;

namespace RainMeadow
{
    public class OnlineSeedCobState : PhysicalObjectEntityState
    {
        [OnlineField]
        bool isOpened;
        [OnlineField]
        bool isDead;

        public OnlineSeedCobState() { }

        public OnlineSeedCobState(OnlineSeedPod onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            isOpened = onlineEntity.AbstractSeedCob.opened;
            isDead = onlineEntity.AbstractSeedCob.dead;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var onlineSeedPod = onlineEntity as OnlineSeedPod;
            if (!onlineSeedPod.AbstractSeedCob.opened && isOpened)
            {
                (onlineSeedPod.AbstractSeedCob.realizedObject as SeedCob).Open();
            }
            onlineSeedPod.AbstractSeedCob.dead = isDead;
        }
    }
}
