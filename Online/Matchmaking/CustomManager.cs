

using System;
using System.Collections.Generic;
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
    }

    public interface IUseCustomPackets
    {
        public bool Active { get; }
        public void ProcessPacket(OnlinePlayer fromPlayer, CustomPacket packet);
    }
}
