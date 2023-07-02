using System.IO;
using Steamworks;

namespace RainMeadow {
	public class JoinLobbyPacket : Packet {
		public override Type type => Packet.Type.JoinLobby;
		
		OnlinePlayer lobbyOwner;
		int nextPlayerId;

		public JoinLobbyPacket() : base() { }
		public JoinLobbyPacket(OnlinePlayer lobbyOwner, int nextPlayerId) : base() {
			this.lobbyOwner = lobbyOwner;
			this.nextPlayerId = nextPlayerId;
		}

		public override void Serialize(BinaryWriter writer) {
			writer.Write(lobbyOwner);
			writer.Write(nextPlayerId);
		}

		public override void Deserialize(BinaryReader reader) {
			lobbyOwner = reader.ReadPlayer();
			nextPlayerId = reader.ReadInt32();
		}
		
		public override void Process() {
			PlayersManager.nextPlayerId = nextPlayerId;
			OnlineManager.lobby = new Lobby(lobbyOwner, SteamMatchmaking.GetLobbyData(LobbyManager.joiningLobbyId, OnlineManager.MODE_KEY));
			LobbyManager.GoToMenu();
		}
	}
}