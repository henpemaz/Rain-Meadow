using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public sealed class ClientSettings : OnlineEntity
    {
        public sealed class Definition : EntityDefinition
        {
            public Definition() : base() { }
            public Definition(ClientSettings clientSettings, OnlineResource inResource) : base(clientSettings, inResource) { }

            public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
            {
                return new ClientSettings(this, inResource, initialState);
            }
        }

        public int nextID;
        public bool inGame;
        public bool isInteracting;
        public List<OnlineEntity.EntityId> avatars = new();

        public ClientSettings(EntityDefinition entityDefinition, OnlineResource inResource, EntityState initialState) : base(entityDefinition, inResource, initialState)
        {

        }

        public ClientSettings(EntityId id, OnlinePlayer owner) : base(id, owner, false)
        {

        }

        public override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new Definition(this, onlineResource);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new State(this, inResource, tick);
        }

        public class State : EntityState
        {
            [OnlineField]
            public int nextID;
            [OnlineField]
            public bool inGame;
            [OnlineField]
            public bool isInteracting;
            [OnlineField(nullable = true)]
            public Generics.DynamicOrderedEntityIDs avatars;

            public State() { }

            public State(ClientSettings clientSettings, OnlineResource inResource, uint ts) : base(clientSettings, inResource, ts)
            {
                nextID = clientSettings.nextID;
                inGame = clientSettings.inGame;
                isInteracting = clientSettings.isInteracting;
                avatars = new(clientSettings.avatars.ToList());
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var clientSettings = (ClientSettings)onlineEntity;
                clientSettings.nextID = nextID;
                clientSettings.inGame = inGame;
                clientSettings.isInteracting = isInteracting;
                clientSettings.avatars = avatars.list;
            }
        }
    }
}