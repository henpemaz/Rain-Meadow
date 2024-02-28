using System.IO;

namespace RainMeadow
{
    public class RequestJoinPacket : Packet
    {
        public override Type type => Type.RequestJoin;
        private string password;

        public RequestJoinPacket() : base() { }
        public RequestJoinPacket(string password) : base()
        {
            this.password = password;
        }
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(password);
        }
        public override void Deserialize(BinaryReader reader)
        {
            password = reader.ReadString();
        }
        public override void Process()
        {
            if (OnlineManager.lobby != null)
            {
                if ((MatchmakingManager.instance as LocalMatchmakingManager).lobbyPassword.Length > 0) {
                    if ((MatchmakingManager.instance as LocalMatchmakingManager).lobbyPassword != password) {
                        NetIO.SendP2P(processingPlayer, new FailedJoinLobbyPacket(), NetIO.SendType.Reliable);
                        return;
                    }
                }
                // Hello packet from joining peer
                (MatchmakingManager.instance as LocalMatchmakingManager).LocalPlayerJoined(processingPlayer);

                // Tell them they are in
                NetIO.SendP2P(processingPlayer, new JoinLobbyPacket(), NetIO.SendType.Reliable);
            }
        }
    }
}