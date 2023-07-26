using System.IO;
using Steamworks;

namespace RainMeadow {
	public class JoinLobbyPacket : Packet {
		public override Type type => Packet.Type.JoinLobby;
		
		public override void Process() {
			(LobbyManager.instance as LocalLobbyManager).LobbyJoined();
        }
	}
}