namespace RainMeadow
{
    public class JoinLobbyPacket : InformLobbyPacket
    {
        public JoinLobbyPacket() : base() { }
        public JoinLobbyPacket(int maxplayers, string name, bool passwordprotected, string mode, int currentplayercount) : base(maxplayers, name, passwordprotected, mode, currentplayercount) { }
        public override Type type => Type.JoinLobby;

        public override void Process()
        {
            RainMeadow.DebugMe();
            OnlineManager.currentlyJoiningLobby = MakeLobbyInfo();
            (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN] as LANMatchmakingManager).LobbyAcknoledgedUs(processingPlayer);
        }
    }
}