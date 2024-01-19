namespace RainMeadow
{
    public class StoryPersonaSettingsDefintion : PersonaSettingsDefinition
    {
        public StoryPersonaSettingsDefintion() : base() { }
        public StoryPersonaSettingsDefintion(OnlineEntity.EntityId entityId, OnlinePlayer owner, bool isTransferable) : base(entityId, owner, isTransferable) { }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return StoryAvatarSettings.FromDefinition(this, inResource);
        }
    }
}