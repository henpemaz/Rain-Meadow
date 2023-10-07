namespace RainMeadow
{
    public abstract class NewPersonaSettingsEvent : NewEntityEvent
    {
        public NewPersonaSettingsEvent() :base() { }
        public NewPersonaSettingsEvent(PersonaSettingsEntity personaSettingsEntity) : base(OnlineManager.lobby, personaSettingsEntity, null)
        {

        }
    }

    public class NewMeadowPersonaSettingsEvent : NewPersonaSettingsEvent
    {
        public NewMeadowPersonaSettingsEvent() : base() { }

        public NewMeadowPersonaSettingsEvent(PersonaSettingsEntity personaSettingsEntity) : base(personaSettingsEntity) { }

        public override EventTypeId eventType => EventTypeId.NewMeadowPersonaSettingsEvent;
    }
}