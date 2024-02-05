using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace RainMeadow
{
    public abstract class AvatarSettings : OnlineEntity
    {
        public abstract class Definition : EntityDefinition
        {
            public Definition() : base() { }
            public Definition(OnlineEntity.EntityId entityId, OnlinePlayer owner) : base(entityId, owner, false) { }
        }

        /// <summary>
        /// the real avatar of the player
        /// </summary>
        public OnlineEntity.EntityId target;

        public AvatarSettings(EntityDefinition entityDefinition) : base(entityDefinition)
        {
            target = new OnlineEntity.EntityId(entityDefinition.owner, OnlineEntity.EntityId.IdType.none, 0);
        }

        internal abstract AvatarCustomization MakeCustomization();

        public abstract class AvatarCustomization
        {
            internal abstract void ModifyBodyColor(ref Color bodyColor);

            internal abstract void ModifyEyeColor(ref Color eyeColor);
        }

        public abstract class State : EntityState
        {
            [OnlineField(nullable:true)]
            private EntityId target;

            protected State() { }

            protected State(AvatarSettings onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                this.target = onlineEntity.target;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                (onlineEntity as AvatarSettings).target = target;
            }
        }
    }
}