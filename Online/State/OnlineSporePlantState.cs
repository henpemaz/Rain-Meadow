namespace RainMeadow
{
    public class OnlineSporePlantState : OnlineConsumableState
    {
        [OnlineField]
        bool pacified;
        [OnlineField]
        bool used;
        public OnlineSporePlantState() { }

        public OnlineSporePlantState(OnlineSporePlant onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            pacified = onlineEntity.AbstractSporePlant.pacified;
            used = onlineEntity.AbstractSporePlant.used;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var onlineSeedPod = onlineEntity as OnlineSporePlant;
            onlineSeedPod.AbstractSporePlant.pacified = pacified;
            onlineSeedPod.AbstractSporePlant.used = used;
        }
    }
}
