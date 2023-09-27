namespace RainMeadow
{
    public class MeadowGameMode : OnlineGameMode
    {
        public MeadowGameMode(Lobby lobby) : base(lobby)
        {
            lobby.OnLobbyAvailable += Lobby_OnLobbyAvailable;
        }

        private void Lobby_OnLobbyAvailable()
        {
            RainMeadow.Debug("Added persona settings!");
            personaSettings = new MeadowPersonaSettings(OnlineManager.mePlayer, new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, PersonaSettingsEntity.personaID));
            MeadowPersonaSettings.map.Add(OnlineManager.mePlayer, personaSettings as MeadowPersonaSettings);
            OnlineManager.recentEntities.Add(personaSettings.id, personaSettings);
            personaSettings.EnterResource(lobby);
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.MeadowMenu;
        }
    }
}
