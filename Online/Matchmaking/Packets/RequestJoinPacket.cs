namespace RainMeadow
{
    public class RequestJoinPacket : Packet
    {
        public override Type type => Type.RequestJoin;

        public override void Process()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby != null && OnlineManager.lobby.isOwner && MatchmakingManager.currentMatchMaker == MatchmakingManager.MatchMaker.Local)
            {
                var matchmaker = (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager);
                // Hello packet from joining peer
                matchmaker.AcknoledgeLANPlayer(processingPlayer);

                // Tell them they are in
                OnlineManager.netIO.SendP2P(processingPlayer, new JoinLobbyPacket(
                    matchmaker.maxplayercount,
                    "LAN Lobby",
                    OnlineManager.lobby.hasPassword,
                    OnlineManager.lobby.gameModeType.value,
                    OnlineManager.players.Count
                ), NetIO.SendType.Reliable);
            }
        }
    }
}