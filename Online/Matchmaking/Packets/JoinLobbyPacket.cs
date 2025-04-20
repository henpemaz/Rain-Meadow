using System.IO;

namespace RainMeadow
{
    public class JoinLobbyPacket : InformLobbyPacket
    {
        public JoinLobbyPacket() : base() { }
        public JoinLobbyPacket(int maxplayers, string name, bool passwordprotected, string mode, int currentplayercount, string highImpactMods = "", string bannedMods = "", int joiningPlayerIndex = -1) : base(maxplayers, name, passwordprotected, mode, currentplayercount, highImpactMods, bannedMods)
        {
            this.joiningPlayerIndex = joiningPlayerIndex;
        }
        public override Type type => Type.JoinLobby;
        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(joiningPlayerIndex);
        }
        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            joiningPlayerIndex = reader.ReadInt32();
        }
        public override void Process()
        {
            if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN)
            {
                return;
            }
            var newLobbyInfo = MakeLobbyInfo();
            if (joiningPlayerIndex < -1)
            {
                RainMeadow.Error("THIS IS UNRELIABLE!");
            }
            else
            {
                (OnlineManager.mePlayer.id as LANMatchmakingManager.LANPlayerId)!.index = joiningPlayerIndex;
            }
            // If we don't have a lobby and we a currently joining a lobby
            if (OnlineManager.lobby is null && OnlineManager.currentlyJoiningLobby is not null) 
            {
                // If the lobby we want to join is a lan lobby
                if (OnlineManager.currentlyJoiningLobby is LANMatchmakingManager.LANLobbyInfo oldLobbyInfo) 
                {
                    // If the lobby we want to join is the lobby that allowed us to join.
                    if (UDPPeerManager.CompareIPEndpoints(oldLobbyInfo.endPoint, newLobbyInfo.endPoint)) 
                    {
                        OnlineManager.currentlyJoiningLobby = newLobbyInfo;
                        (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN] as LANMatchmakingManager).maxplayercount = newLobbyInfo.maxPlayerCount;
                        (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN] as LANMatchmakingManager).LobbyAcknoledgedUs(processingPlayer);
                    }
                }
            }
        }
        public int joiningPlayerIndex = -1; //person who recieved request from joining player and gets joinlobby packet, joining player index makes sure any changes called by the person recieving RequestJoinPacket will be synced
    }
}