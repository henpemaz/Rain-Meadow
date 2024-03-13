namespace RainMeadow
{
    public class OnlinePebblesPearlDefinition : OnlineConsumableDefinition
    {
        [OnlineField]
        public sbyte originalColor;
        [OnlineField]
        public short originalNumber;
        public OnlinePebblesPearlDefinition() { }
        public OnlinePebblesPearlDefinition(OnlineConsumableDefinition ocmDef, PebblesPearl.AbstractPebblesPearl abstractPebblesPearl) : base(ocmDef)
        {
            //Color is only ever the numbers -4 to 4 based on DataPearl::ApplyPalette
            this.originalColor = (sbyte)abstractPebblesPearl.color;
            //This is probably safe. We might need to use an actual Int if number is actually big....
            this.originalNumber = (short)abstractPebblesPearl.number;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlinePebblesPearl.FromDefinition(this, inResource);
        }
    }
}
