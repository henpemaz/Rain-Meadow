using System.IO;

namespace RainMeadow
{
    public class CustomPacket : Packet
    {
        public string key = "";
        public int size;
        public byte[] data;
        public override Type type => Type.CustomPacket;

        public CustomPacket() { }
        public CustomPacket(string key, byte[] data)
        {
            this.key = key;
            this.size = data.Length;
            this.data = data;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(this.key);
            writer.Write(this.size);
            writer.Write(this.data);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            this.key = reader.ReadString();
            this.size = reader.ReadInt32();
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
    }
}
