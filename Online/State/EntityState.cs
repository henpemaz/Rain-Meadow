using RainMeadow.Generics;
using UnityEngine;

namespace RainMeadow
{
    public abstract class EntityState : RootDeltaState, IIdentifiable<OnlineEntity.EntityId>
    {
        // if sent "standalone" tracks Baseline
        // if sent inside another delta, doesn't
        [OnlineField(always:true)]
        public OnlineEntity.EntityId entityId;
        public OnlineEntity.EntityId ID => entityId;

        protected EntityState() : base() { }
        protected EntityState(OnlineEntity onlineEntity, uint ts) : base(ts)
        {
            this.entityId = onlineEntity.id;
        }

        public abstract void ReadTo(OnlineEntity onlineEntity);
    }
}
