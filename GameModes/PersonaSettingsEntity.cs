using System;

namespace RainMeadow
{
    public abstract class PersonaSettingsEntity : OnlineEntity
    {
        public PersonaSettingsEntity(OnlinePlayer owner, EntityId id) : base(owner, id, false)
        {

        }

        internal static OnlineEntity FromEvent(NewPersonaSettingsEvent newPersonaSettingsEvent, OnlineResource inResource)
        {
            if (newPersonaSettingsEvent is NewMeadowPersonaSettingsEvent meadowPersonaSettings) return MeadowPersonaSettings.FromEvent(meadowPersonaSettings, inResource);
            throw new InvalidOperationException("unknown entity event type");
        }
    }
}