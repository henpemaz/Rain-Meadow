using System;

namespace RainMeadow
{
    public abstract class AvatarSettingsEntity : OnlineEntity
    {
        internal static int personaID = -100; // todo different types of entity id so we dont need this hack
        public EntityId target; // todo entity carries data so don't need to "target" another entity;

        public AvatarSettingsEntity(EntityDefinition entityDefinition) : base(entityDefinition) { }

        internal abstract void ApplyCustomizations(object subject, OnlineEntity oe);

        internal void BindEntity(OnlineEntity target)
        {
            this.target = target.id;
        }

        public abstract class AvatarSettingsState : EntityState
        {
            [OnlineField(nullable:true)]
            private EntityId target;

            protected AvatarSettingsState() { }

            protected AvatarSettingsState(OnlineEntity onlineEntity, uint ts) : base(onlineEntity, ts)
            {
                AvatarSettingsEntity personaSettings = (AvatarSettingsEntity)onlineEntity;
                target = personaSettings.target;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                AvatarSettingsEntity personaSettings = (AvatarSettingsEntity)onlineEntity;
                personaSettings.target = target;
            }
        }
    }
}