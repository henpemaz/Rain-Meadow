using System;

namespace RainMeadow
{
    public abstract class AvatarSettingsEntity : OnlineEntity
    {
        public AvatarSettingsEntity(EntityDefinition entityDefinition) : base(entityDefinition) { }

        public abstract class AvatarSettingsState : EntityState
        {
            protected AvatarSettingsState() { }

            protected AvatarSettingsState(OnlineEntity onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {

            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
            }
        }
    }
}