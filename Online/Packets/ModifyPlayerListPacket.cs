using System.IO;
using System.Net;
using System.Linq;
using Steamworks;

namespace RainMeadow {
	public class ModifyPlayerListPacket : Packet {
		public override Type type => Packet.Type.ModifyPlayerList;

		public enum Operation : byte {
			Add,
			Remove,
		}
		Operation modifyOperation;
		OnlinePlayer[] players;

		public ModifyPlayerListPacket() : base() { }
		public ModifyPlayerListPacket(Operation modifyOperation, OnlinePlayer[] players) : base() {
			this.modifyOperation = modifyOperation;
			this.players = players;
		}

		public override void Serialize(BinaryWriter writer) {
			writer.Write((byte)modifyOperation);
			writer.EncodeVLQ((uint)players.Length);
			for (int i = 0; i < players.Length; i++) {
				var player = players[i];
				writer.Write(player.netId);

				if (modifyOperation != Operation.Add) continue;

				writer.Write((ulong)player.steamId);
				
				if (!processingSteamID.IsValid() || !player.isUsingSteam) {
					var addressBytes = player.endpoint.Address.GetAddressBytes();
					writer.Write((byte)addressBytes.Length);
					writer.Write(addressBytes);
					writer.Write((ushort)player.endpoint.Port);
				}
			}
		}

		public override void Deserialize(BinaryReader reader) {
			modifyOperation = (Operation)reader.ReadByte();
			players = new OnlinePlayer[reader.DecodeVLQ()];
			for (int i = 0; i < players.Length; i++) {
				int netId = reader.ReadInt32();

				switch (modifyOperation) {
					case Operation.Add:
						CSteamID steamId = new CSteamID(reader.ReadUInt64());

						IPEndPoint endPoint = null;
						if (!PlayersManager.mePlayer.isUsingSteam || !steamId.IsValid()) {
							endPoint = new IPEndPoint(new IPAddress(reader.ReadBytes(reader.ReadByte())), (int)reader.ReadUInt16());
							if (PlayersManager.players.Count == 1) {
								// The first thing received is the communicating peer, but they will send a loopback ip
								// so override it with where they are communicating from
								endPoint = processingIpEndpoint; 
							}
						}

						var player = PlayersManager.TryGetPlayer(netId);
						if (player != null) {
							RainMeadow.Error($"Player with ID {netId} '{player.name}' already exists!");
							continue;
						}
						
						players[i] = new OnlinePlayer(netId, steamId, endPoint);
						break;
					
					
					case Operation.Remove:
						players[i] = PlayersManager.TryGetPlayer(netId);
						break;
				}
			}
		}
		
		public override void Process() {
			switch (modifyOperation) {
				case Operation.Add:
					PlayersManager.players.AddRange(players);
					PlayersManager.nextPlayerId = players.Last().netId + 1;
					break;
				
				case Operation.Remove:
					PlayersManager.players.RemoveAll(player => players.Contains(player));
					break;
			}
			
		}
	}
}