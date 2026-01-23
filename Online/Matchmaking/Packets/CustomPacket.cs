using System.IO;

namespace RainMeadow
{
    public class CustomPacket : Packet
    {
        public string key = "";
        public byte[] data;
        public ushort dataSize;
        public override Type type => Type.CustomPacket;

        public CustomPacket() { }
        public CustomPacket(string key, byte[] data, ushort dataSize)
        {
            this.key = key;
            this.dataSize = dataSize;
            this.data = data;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(this.key);
            writer.Write(this.dataSize);
            writer.Write(this.data, 0, this.dataSize);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            this.key = reader.ReadString();
            this.dataSize = reader.ReadUInt16();
            this.data = reader.ReadBytes(this.dataSize);
        }

        public override void Process()
        {
            if (key == "" || data == null)
            {
                return;
            }
            if (key.Length > 16 || data.Length > 32768)
            {
                RainMeadow.Error($"Custom Packet was too large, the maximum size is 32768");
                return;
            }
            MatchmakingManager.currentInstance.RecieveCustomPacket(processingPlayer, this);
        }

        public void SteamEncode(MemoryStream ms, BinaryWriter writer)
        {
            writer.Write(this.key);
            writer.Write(this.dataSize);
            writer.Write(this.data, 0, this.dataSize);
        }
    }
}
