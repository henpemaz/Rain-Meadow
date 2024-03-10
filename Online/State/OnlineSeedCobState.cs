namespace RainMeadow
{
    public class OnlineSeedCobState : OnlineConsumableState
    {
        [OnlineField]
        bool isOpened;

        public OnlineSeedCobState() { }

        public OnlineSeedCobState(OnlineSeedCob onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            isOpened = onlineEntity.AbstractSeedCob.opened;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var onlineSeedPod = onlineEntity as OnlineSeedCob;
            if (!onlineSeedPod.AbstractSeedCob.opened && isOpened)
            {
                (onlineSeedPod.AbstractSeedCob.realizedObject as SeedCob).Open();
            }
        }
    }
}
