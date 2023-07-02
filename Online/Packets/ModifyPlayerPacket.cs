using System;
using System.IO;
using Steamworks;

namespace RainMeadow {
	public class ModifyPlayerPacket : Packet {		
		public override Type type => Packet.Type.ModifyPlayer;

		int netId;
		CSteamID steamID;

		public ModifyPlayerPacket() : base() { }
		public ModifyPlayerPacket(OnlinePlayer newPlayerInfo) : base() {
			netId = newPlayerInfo.netId;
			steamID = newPlayerInfo.steamId;
		}

		public override void Serialize(BinaryWriter writer) {
			writer.Write(netId);
			writer.Write((ulong)steamID);
		}

		public override void Deserialize(BinaryReader reader) {
			netId = reader.ReadInt32();
			steamID = new CSteamID(reader.ReadUInt64());
		}
		
		public override void Process() {
			if (steamID.IsValid()) {
				if (steamID != PlayersManager.mePlayer.steamId) {
					throw new Exception($"Tried to update self with mismatching Steam ID");
				}
			} else {
				RainMeadow.Debug("Clearing steam id...");
				PlayersManager.mePlayer.steamId.Clear();
			}

			PlayersManager.mePlayer.netId = netId;
			RainMeadow.Debug($"Your new network id is {netId}");

			PlayersManager.nextPlayerId = netId + 1;
		}
	}
}