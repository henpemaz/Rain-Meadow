namespace RainMeadow
{
    public abstract class PersonaSettingsDefinition : EntityDefinition
    {
        public PersonaSettingsDefinition() :base() { }
        public PersonaSettingsDefinition(PersonaSettingsEntity personaSettingsEntity) : base(personaSettingsEntity) { }
    }

    public class MeadowPersonaSettingsDefinition : PersonaSettingsDefinition
    {
        public MeadowPersonaSettingsDefinition() : base() { }

        public MeadowPersonaSettingsDefinition(PersonaSettingsEntity personaSettingsEntity) : base(personaSettingsEntity) { }
    }
}