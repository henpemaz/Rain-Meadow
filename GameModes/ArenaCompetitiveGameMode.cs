namespace RainMeadow
{
    public class ArenaCompetitiveGameMode : OnlineGameMode
    {
        public ArenaCompetitiveGameMode(Lobby lobby) : base(lobby)
        {
        }

        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.ArenaLobbyMenu;
        }
    }
}