using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow
{
    public class CustomManager
    {
        public const int maxLength = 12;
        
        private static Dictionary<string, IUseCustomPackets> subscribers = new();
        public static void HandlePacket(OnlinePlayer player, CustomPacket packet)
        {
            if (subscribers.TryGetValue(packet.key, out var s))
                s.ProcessPacket(player, packet);
        }
        public static void Subscribe(string k, IUseCustomPackets e)
        { 
            subscribers.Add(k, e);
            RefreshSettings();
        }
        public static void Unsubscribe(string k)
        {
            subscribers.Remove(k);
            RefreshSettings();
        }

        public static void SendCustomData(OnlinePlayer toPlayer, string key, byte[] data, ushort size, NetIO.SendType sendType)
        {
            OnlineManager.SendCustomData(toPlayer, new CustomPacket(key, data, size), sendType);
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

        public static List<string> GetSubscribed()
        {
            return subscribers.Keys.ToList();
        }

        private static void RefreshSettings()
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].TryGetData<CustomClientSettings>(out var customSettings))
            {
                customSettings.Refresh();
            }
        }
    }

    public interface IUseCustomPackets
    {
        public bool Active { get; }
        public void ProcessPacket(OnlinePlayer fromPlayer, CustomPacket packet);
    }

    public class CustomClientSettings : OnlineEntity.EntityData
    {
        public List<string> keys = new();

        public CustomClientSettings()
        {
        }

        public void Refresh()
        {
            keys = CustomManager.GetSubscribed();
        }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new State(this);
        }

        public class State : EntityDataState
        {
            [OnlineField(group = "customClientData")]
            public List<string> keys;
            public State() { }
            public State(CustomClientSettings entity) : base()
            {
                keys = entity.keys;
            }

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var customClientSettings = (CustomClientSettings)data;
                if (keys.Count > 32) // Enforce a max list length of 32
                {
                    RainMeadow.Debug($"Tried syncing {keys.Count} custom packet keys. Capping at 32.");
                    keys.RemoveRange(32, keys.Count - 32);
                }
                customClientSettings.keys = keys.Select(s => s.Substring(0, Math.Min(CustomManager.maxLength, s.Length))).ToList(); // Enforce a max string length of 12
            }

            public override Type GetDataType() => typeof(CustomClientSettings);
        }
    }
}
