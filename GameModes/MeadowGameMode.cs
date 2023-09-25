namespace RainMeadow
{
    public class MeadowGameMode : OnlineGameMode
    {
        public MeadowPersonaSettings personaSettings;

        public MeadowGameMode(Lobby lobby) : base(lobby)
        {
            lobby.OnLobbyAvailable += Lobby_OnLobbyAvailable;
        }

        private void Lobby_OnLobbyAvailable()
        {
            RainMeadow.Debug("Added persona settings!");
            personaSettings = new MeadowPersonaSettings(OnlineManager.mePlayer, new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, 0));
            personaSettings.EnterResource(lobby);
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.MeadowMenu;
        }
    }
}
