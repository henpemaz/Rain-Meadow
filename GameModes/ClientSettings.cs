using UnityEngine;

namespace RainMeadow
{
    public abstract class ClientSettings : OnlineEntity
    {
        public abstract class Definition : EntityDefinition
        {
            public Definition() : base() { }
            public Definition(ClientSettings clientSettings, OnlineResource inResource) : base(clientSettings, inResource) { }
        }

        /// <summary>
        /// the real avatar of the player
        /// </summary>
        public EntityId avatarId;
        public bool inGame;

        public ClientSettings(EntityDefinition entityDefinition, OnlineResource inResource, EntityState initialState) : base(entityDefinition, inResource, initialState)
        {
            avatarId = new EntityId(entityDefinition.owner, EntityId.IdType.none, 0);
        }

        protected ClientSettings(EntityId id, OnlinePlayer owner) : base(id, owner, false)
        {

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
            private EntityId avatarId;
            [OnlineField]
            public bool inGame;

            protected State() { }

            protected State(ClientSettings clientSettings, OnlineResource inResource, uint ts) : base(clientSettings, inResource, ts)
            {
                this.avatarId = clientSettings.avatarId;
                inGame = clientSettings.inGame;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var avatarSettings = (ClientSettings)onlineEntity;
                avatarSettings.avatarId = avatarId;
                avatarSettings.inGame = inGame;
            }
        }
    }
}