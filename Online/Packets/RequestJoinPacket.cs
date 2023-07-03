using System.IO;
using Steamworks;

namespace RainMeadow {
	public class RequestJoinPacket : Packet {
		public override Type type => Packet.Type.RequestJoin;

		public override void Process() {
			// Hello packet from joining peer
			OnlinePlayer joiningPlayer = processingSteamID.IsValid() ? 
				PlayersManager.SteamPlayerJoined(processingSteamID) :
				PlayersManager.IpPlayerJoined(processingIpEndpoint);

			// Tell them they are now ready by sending them the owner
			NetIO.SendP2P(joiningPlayer, new JoinLobbyPacket(OnlineManager.lobby.owner, PlayersManager.nextPlayerId), SendType.Reliable);
		}
	}
}