namespace RainMeadow
{
    public class OnlineBubbleGrassDefinition : OnlineConsumableDefinition
    {
        [OnlineField]
        public float originalOxygenLevel;

        public OnlineBubbleGrassDefinition() { }

        public OnlineBubbleGrassDefinition(OnlineConsumableDefinition opod, BubbleGrass.AbstractBubbleGrass abstractConsumable) : base(opod,abstractConsumable)
        {
            this.originalOxygenLevel = abstractConsumable.oxygenLeft;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlineBubbleGrass.FromDefinition(this, inResource);
        }
    }
}
