namespace RainMeadow
{
    public class OnlineSporePlant : OnlineConsumable
    {
        public class OnlineSporePlantDefinition : OnlineConsumableDefinition
        {
            public OnlineSporePlantDefinition() { }

            public OnlineSporePlantDefinition(OnlineSporePlant onlineSporePlant, OnlineResource inResource) : base(onlineSporePlant, inResource) { }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlineSporePlant(this, inResource, (OnlineSporePlantState)initialState);
            }
        }

        public OnlineSporePlant(OnlineSporePlantDefinition entityDefinition, OnlineResource inResource, OnlineSporePlantState initialState) : base(entityDefinition, inResource, initialState)
        {
            AbstractSporePlant.pacified = initialState.pacified;
            AbstractSporePlant.used = initialState.used;
        }

        public OnlineSporePlant(AbstractConsumable ac, EntityId id, OnlinePlayer owner, bool isTransferable) : base(ac, id, owner, isTransferable)
        {

        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new OnlineSporePlantDefinition(this, onlineResource);
        }

        public SporePlant.AbstractSporePlant AbstractSporePlant => apo as SporePlant.AbstractSporePlant;

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new OnlineSporePlantState(this, inResource, tick);
        }

        public class OnlineSporePlantState : OnlineConsumableState
        {
            [OnlineField]
            public bool pacified;
            [OnlineField]
            public bool used;
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
}
