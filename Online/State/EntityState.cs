using RainMeadow.Generics;

namespace RainMeadow
{
    public abstract class EntityState : RootDeltaState, IIdentifiable<OnlineEntity.EntityId>
    {
        // if sent "standalone" tracks Baseline
        // if sent inside another delta, doesn't
        [OnlineField(always:true)]
        public OnlineEntity.EntityId entityId;
        [OnlineField(nullable:true, polymorphic:true)]
        public EntityDataState gamemodeDataState; // todo make non-null array
        public OnlineEntity.EntityId ID => entityId;

        protected EntityState() : base() { }
        protected EntityState(OnlineEntity onlineEntity, OnlineResource inResource, uint ts) : base(ts)
        {
            this.entityId = onlineEntity.id;
            this.gamemodeDataState = onlineEntity.gameModeData?.MakeState(inResource);
        }

        public virtual void ReadTo(OnlineEntity onlineEntity)
        {
            gamemodeDataState?.ReadTo(onlineEntity);
        }
    }
}
