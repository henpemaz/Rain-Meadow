using System;
using System.IO;
using System.Net;
using Steamworks;

namespace RainMeadow {
	public abstract class Packet : ISerializable {
		public enum Type {
			None,
			RequestJoin,
			ModifyPlayer,
			ModifyPlayerList,
			JoinLobby,
			Session,
		}

		public abstract Type type { get; }
		public uint size = 0; // May not need this unless large data for some reason

		internal static CSteamID processingSteamID;
		internal static IPEndPoint processingIpEndpoint;

		public virtual void Serialize(BinaryWriter writer) { } // Write into bytes
		public virtual void Deserialize(BinaryReader reader) { } // Read from bytes
		public virtual void Process() { } // Do the payload

		public static void Encode(Packet packet, BinaryWriter writer, CSteamID toSteamID = default(CSteamID), IPEndPoint toIpEndpoint = null) {
			processingSteamID = toSteamID;
			processingIpEndpoint = toIpEndpoint;

			writer.EncodeVLQ((uint)packet.type); // 1 - 2 bytes
			var startingPos = writer.BaseStream.Position;
			var payloadPos = writer.BaseStream.Position += 2;

			writer.Write(packet);
			packet.size = (uint)(writer.BaseStream.Position - payloadPos);


			writer.BaseStream.Position = startingPos;
			writer.EncodeVLQ(packet.size); // 1 - 2 bytes
			writer.Write(((MemoryStream)writer.BaseStream).GetBuffer(), (int)payloadPos, (int)packet.size);
		}

		public static void Decode(BinaryReader reader, CSteamID fromSteamID = default(CSteamID), IPEndPoint fromIpEndpoint = null) {
			processingSteamID = fromSteamID;
			processingIpEndpoint = fromIpEndpoint;

			Type type = (Type)reader.DecodeVLQ();

			Packet? packet = type switch {
				Type.RequestJoin => new RequestJoinPacket(),
				Type.ModifyPlayer => new ModifyPlayerPacket(),
				Type.ModifyPlayerList => new ModifyPlayerListPacket(),
				Type.JoinLobby => new JoinLobbyPacket(),
				Type.Session => new SessionPacket(),
				_ => null
			};

			if (packet == null) throw new Exception($"Undetermined packet type ({(uint)type}) received");

			packet.size = reader.DecodeVLQ();

			var startingPos = reader.BaseStream.Position;
			try {
				reader.Read(packet);
				var readLength = reader.BaseStream.Position - startingPos;
				
				if (readLength != packet.size) throw new Exception($"Payload size mismatch, expected {packet.size} but read {readLength}");
			
				packet.Process();
			} catch (Exception e) {
				throw;
			} finally {
				// Move stream position to next part of packet
				reader.BaseStream.Position = startingPos + packet.size;
			}
		}
	}
}