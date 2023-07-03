using System;
using System.IO;

namespace RainMeadow {
	public class SessionPacket : Packet {
		public override Type type => Packet.Type.Session;
		
		byte[] data;

		public SessionPacket() : base() { }
		public SessionPacket(byte[] data) : base() {
			this.data = data;
		}

		public override void Serialize(BinaryWriter writer) {
			writer.Write(data);
		}

		public override void Deserialize(BinaryReader reader) {
			data = reader.ReadBytes((int)size);
		}
		
		public override void Process() {
			if (processingSteamID.IsValid()) {
				Buffer.BlockCopy(data, 0, OnlineManager.serializer.buffer, 0, (int)size);
				OnlineManager.serializer.ReceiveDataSteam();
			} else {
				OnlineManager.serializer.ReceiveDataDebug(processingIpEndpoint, data);
			}
		}
	}
}