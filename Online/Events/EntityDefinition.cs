using RainMeadow.Generics;

namespace RainMeadow
{
    [DeltaSupport(level=StateHandler.DeltaSupport.FollowsContainer)]
    public abstract class EntityDefinition : OnlineState, IIdentifiable<OnlineEntity.EntityId>
    {
        public OnlineEntity.EntityId entityId;
        public OnlinePlayer owner;
        public bool isTransferable;

        public EntityDefinition() { }

        public EntityDefinition(OnlineEntity oe)
        {
            entityId = oe.id;
            owner = oe.owner;
            isTransferable = oe.isTransferable;
        }

        public OnlineEntity.EntityId ID => entityId;
    }
}