

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow
{
    public class CustomManager
    {
        
        private static Dictionary<string, IUseCustomPackets> subscribers = new();
        public static void HandlePacket(OnlinePlayer player, CustomPacket packet)
        {
            if (subscribers.TryGetValue(packet.key, out var s))
                s.ProcessPacket(player, packet);
        }
        public static void Subscribe(string k, IUseCustomPackets e) => subscribers.Add(k, e);
        public static void Unsubscribe(string k) => subscribers.Remove(k);

        public static void SendCustomData(OnlinePlayer toPlayer, string key, byte[] data, ushort size, NetIO.SendType sendType)
        {
            if (toPlayer.id is SteamMatchmakingManager.SteamPlayerId)
            {
                OnlineManager.SendCustomData(toPlayer, new CustomPacket(key, data, size), sendType);
            }
            else if (toPlayer.id is LANMatchmakingManager.LANPlayerId)
            {
                OnlineManager.netIO.SendP2P(toPlayer, new CustomPacket(key, data, size), sendType);
            }
        }

        public static void ReadCustom(OnlinePlayer fromPlayer, byte[] data)
        {
            using (MemoryStream  ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                string key = reader.ReadString();
                ushort size = reader.ReadUInt16();
                byte[] customData = reader.ReadBytes(size);
                HandlePacket(fromPlayer, new CustomPacket(key, customData, size));
            }
        }
    }

    public interface IUseCustomPackets
    {
        public bool Active { get; }
        public void ProcessPacket(OnlinePlayer fromPlayer, CustomPacket packet);
    }
}
