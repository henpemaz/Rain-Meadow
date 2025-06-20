using System.IO;

namespace RainMeadow
{
    public class CustomPacket : Packet
    {
        public string key = "";
        public byte[] data;
        public override Type type => Type.CustomPacket;

        public CustomPacket() { }
        public CustomPacket(string key, byte[] data, ushort size)
        {
            this.key = key;
            this.data = data;
            this.size = size;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(this.key);
            writer.Write(this.data, 0, this.size);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            this.key = reader.ReadString();
            this.data = reader.ReadBytes(this.size);
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
            writer.Write(this.size);
            writer.Write(this.data, 0, this.size);
        }
    }
}
