using System.IO;
using Steamworks;

namespace RainMeadow
{
    public class RequestJoinPacket : Packet
    {
        public override Type type => Packet.Type.RequestJoin;

        public override void Process()
        {
            if(LobbyManager.lobby != null)
            {
                // Hello packet from joining peer
                (LobbyManager.instance as LocalLobbyManager).LocalPlayerJoined(processingPlayer);

                // Tell them they are in
                NetIO.SendP2P(processingPlayer, new JoinLobbyPacket(), NetIO.SendType.Reliable);
            }
        }
    }
}