using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using Steamworks;
using UnityEngine;

namespace RainMeadow
{
    public static class NetIO
    {
        public static void SendData(OnlinePlayer player, byte[] data, uint length, SendType sendType)
        {

        }

        public static void SendP2P(OnlinePlayer player, Packet packet, SendType sendType)
        {
            MemoryStream memory = new MemoryStream(128);
            BinaryWriter writer = new BinaryWriter(memory);

            Packet.Encode(packet, writer, player.id, player.endpoint);

            byte[] bytes = memory.GetBuffer();

            SendData(player, bytes, (uint)memory.Position, sendType);
        }

        public static void Update()
        {
            UdpPeer.Update();

            while (UdpPeer.IsPacketAvailable())
            {
                if (!UdpPeer.Read(out BinaryReader netReader, out IPEndPoint remoteEndpoint))
                    continue;

                Packet.Decode(netReader, fromIpEndpoint: remoteEndpoint);
            }
        }
    }

    public enum SendType : byte
    {
        Reliable,
        Unreliable,
    }

    public interface ISerializable
    {
        public void Serialize(BinaryWriter writer);
        public void Deserialize(BinaryReader reader);
    }
}
