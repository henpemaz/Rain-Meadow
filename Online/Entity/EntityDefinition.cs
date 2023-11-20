using RainMeadow.Generics;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public abstract class EntityDefinition : OnlineState, IIdentifiable<OnlineEntity.EntityId>
    {
        [OnlineField(always: true)]
        public OnlineEntity.EntityId entityId;
        [OnlineField]
        public OnlinePlayer owner;
        [OnlineField]
        public bool isTransferable;

        public EntityDefinition() : base() { }

        protected EntityDefinition(OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable)
        {
            this.entityId = entityId;
            this.owner = owner;
            this.isTransferable = isTransferable;
        }

        public abstract OnlineEntity MakeEntity(OnlineResource inResource);

        public OnlineEntity.EntityId ID => entityId;
    }
}