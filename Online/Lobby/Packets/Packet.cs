using System;
using System.IO;

namespace RainMeadow
{
    public abstract class Packet
    {
        public enum Type : byte
        {
            None,
            RequestJoin,
            JoinLobby,
            ModifyPlayerList,
            Session,
        }

        public abstract Type type { get; }
        public ushort size = 0;

        public virtual void Serialize(BinaryWriter writer) { } // Write into bytes
        public virtual void Deserialize(BinaryReader reader) { } // Read from bytes
        public virtual void Process() { } // Do the payload

        public static OnlinePlayer processingPlayer;
        public static void Encode(Packet packet, BinaryWriter writer, OnlinePlayer toPlayer)
        {
            processingPlayer = toPlayer;

            writer.Write((byte)packet.type);
            long payloadPos = writer.Seek(2, SeekOrigin.Current);


            packet.Serialize(writer);
            packet.size = (ushort)(writer.BaseStream.Position - payloadPos);

            writer.Seek((int)payloadPos - 2, SeekOrigin.Begin);
            writer.Write(packet.size);
            writer.Seek(packet.size, SeekOrigin.Current);
        }

        public static void Decode(BinaryReader reader, OnlinePlayer fromPlayer)
        {
            processingPlayer = fromPlayer;

            Type type = (Type)reader.ReadByte();
            //RainMeadow.Debug("Got packet type: " + type);

            Packet? packet = type switch
            {
                Type.RequestJoin => new RequestJoinPacket(),
                Type.ModifyPlayerList => new ModifyPlayerListPacket(),
                Type.JoinLobby => new JoinLobbyPacket(),
                Type.Session => new SessionPacket(),
                _ => null
            };

            if (packet == null) throw new Exception($"Undetermined packet type ({type}) received");

            packet.size = reader.ReadUInt16();

            var startingPos = reader.BaseStream.Position;
            try
            {
                packet.Deserialize(reader);
                var readLength = reader.BaseStream.Position - startingPos;

                if (readLength != packet.size) throw new Exception($"Payload size mismatch, expected {packet.size} but read {readLength}");

                packet.Process();
            }
            finally
            {
                // Move stream position to next part of packet
                reader.BaseStream.Position = startingPos + packet.size;
            }
        }
    }
}