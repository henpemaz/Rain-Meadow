namespace RainMeadow
{
    public class OnlineBubbleGrassState : OnlineConsumableState
    {
        [OnlineField]
        float oxygenLeft;
        public OnlineBubbleGrassState() { }

        public OnlineBubbleGrassState(OnlineBubbleGrass onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            oxygenLeft = onlineEntity.AbstractBubbleGrass.oxygenLeft;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var onlineBubbleGrass = onlineEntity as OnlineBubbleGrass;
            onlineBubbleGrass.AbstractBubbleGrass.oxygenLeft = oxygenLeft;
        }
    }
}