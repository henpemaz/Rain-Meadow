
namespace RainMeadow
{
    public class OnlineBubbleGrass : OnlineConsumable
    {
        public class OnlineBubbleGrassDefinition : OnlineConsumableDefinition
        {
            public OnlineBubbleGrassDefinition() { }

            public OnlineBubbleGrassDefinition(OnlineConsumable onlineConsumable, OnlineResource inResource) : base(onlineConsumable, inResource) { }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlineBubbleGrass(this, inResource, (OnlineBubbleGrassState)initialState);
            }
        }

        public OnlineBubbleGrass(OnlineConsumableDefinition entityDefinition, OnlineResource inResource, OnlineBubbleGrassState initialState) : base(entityDefinition, inResource, initialState)
        {
            AbstractBubbleGrass.oxygenLeft = initialState.oxygenLeft;
        }

        public OnlineBubbleGrass(AbstractConsumable ac, EntityId id, OnlinePlayer owner, bool isTransferable) : base(ac, id, owner, isTransferable)
        {

        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new OnlineBubbleGrassDefinition(this, onlineResource);
        }

        public BubbleGrass.AbstractBubbleGrass AbstractBubbleGrass => apo as BubbleGrass.AbstractBubbleGrass;

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new OnlineBubbleGrassState(this, inResource, tick);
        }

        public class OnlineBubbleGrassState : OnlineConsumableState
        {
            [OnlineField]
            public float oxygenLeft;
            public OnlineBubbleGrassState() { }

            public OnlineBubbleGrassState(OnlineBubbleGrass onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                oxygenLeft = onlineEntity.AbstractBubbleGrass.oxygenLeft;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                (onlineEntity as OnlineBubbleGrass).AbstractBubbleGrass.oxygenLeft = oxygenLeft;
            }
        }
    }
}
