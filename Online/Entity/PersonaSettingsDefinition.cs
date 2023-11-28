namespace RainMeadow
{
    public abstract class PersonaSettingsDefinition : EntityDefinition
    {
        public PersonaSettingsDefinition() : base() { }
        public PersonaSettingsDefinition(OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable) : base(entityId, owner, isTransferable) { }
    }

    public class MeadowPersonaSettingsDefinition : PersonaSettingsDefinition
    {
        public MeadowPersonaSettingsDefinition() : base() { }
        public MeadowPersonaSettingsDefinition(OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable) : base(entityId, owner, isTransferable) { }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return MeadowAvatarSettings.FromDefinition(this, inResource);
        }
    }
}