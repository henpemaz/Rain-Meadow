namespace RainMeadow
{
    public class OnlinePebblesPearl : OnlineConsumable
    {
        public class OnlinePebblesPearlDefinition : OnlineConsumableDefinition
        {
            [OnlineField]
            //Color is only ever the numbers -4 to 4 based on DataPearl::ApplyPalette
            public sbyte originalColor;
            [OnlineField]
            //This is probably safe. We might need to use an actual Int if number is actually big....
            public short originalNumber;
            public OnlinePebblesPearlDefinition() { }

            public OnlinePebblesPearlDefinition(OnlinePebblesPearl onlinePebblesPearl, OnlineResource inResource) : base(onlinePebblesPearl, inResource)
            {
                this.originalColor = (sbyte)onlinePebblesPearl.AbstractPebblesPearl.color;
                this.originalNumber = (short)onlinePebblesPearl.AbstractPebblesPearl.number;
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlinePebblesPearl(this, inResource, (OnlineConsumableState)initialState);
            }
        }

        public OnlinePebblesPearl(OnlinePebblesPearlDefinition entityDefinition, OnlineResource inResource, OnlineConsumableState initialState) : base(entityDefinition, inResource, initialState)
        {
            AbstractPebblesPearl.color = entityDefinition.originalColor;
            AbstractPebblesPearl.number = entityDefinition.originalNumber;
        }

        public OnlinePebblesPearl(AbstractConsumable ac, EntityId id, OnlinePlayer owner, bool isTransferable) : base(ac, id, owner, isTransferable)
        {

        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new OnlinePebblesPearlDefinition(this, onlineResource);
        }

        public PebblesPearl.AbstractPebblesPearl AbstractPebblesPearl => apo as PebblesPearl.AbstractPebblesPearl;
    }
}
