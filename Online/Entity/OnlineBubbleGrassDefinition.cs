namespace RainMeadow
{
    public class OnlineBubbleGrassDefinition : OnlineConsumableDefinition
    {
        [OnlineField]
        public float originalOxygenLevel;
        public OnlineBubbleGrassDefinition() { }
        public OnlineBubbleGrassDefinition(OnlineConsumableDefinition ocd, BubbleGrass.AbstractBubbleGrass abg) : base(ocd)
        {
            this.originalOxygenLevel = abg.oxygenLeft;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlineBubbleGrass.FromDefinition(this, inResource);
        }
    }
}