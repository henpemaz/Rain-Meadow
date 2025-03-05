using System.Collections.Generic;

namespace RainMeadow
{
    partial class NetIOPlatform {
        static partial void PlatformSteamAvailable(ref bool val);
        static partial void PlatformLanAvailable(ref bool val);
        static partial void PlatformRouterAvailable(ref bool val);


        public static bool isSteamAvailable { get { bool val = false; PlatformSteamAvailable(ref val); return val; } }
        public static bool isLANAvailable { get { bool val = false; PlatformLanAvailable(ref val); return val; } }
        public static bool isRouterAvailable { get { bool val = false; PlatformRouterAvailable(ref val); return val; } }


        public static UDPPeerManager? PlatformUDPManager { get; } = new();
    }
    public abstract class NetIO
    {
        public static NetIO? currentInstance { get => instances[MatchmakingManager.currentDomain]; }
        public static Dictionary<MatchmakingManager.MatchMakingDomain, NetIO> instances = new();

        public enum SendType : byte
        {
            Reliable,
            Unreliable,
        }

        public static void InitializesNetIO() {
            if (NetIOPlatform.isLANAvailable) instances.Add(MatchmakingManager.MatchMakingDomain.LAN, new LANNetIO());
            // if (NetIOPlatform.isRouterAvailable) instances.Add(MatchmakingManager.MatchMakingDomain.Router, new RouterNetIO());
            if (NetIOPlatform.isSteamAvailable) instances.Add(MatchmakingManager.MatchMakingDomain.Steam, new SteamNetIO())    
        }

        public virtual void SendSessionData(OnlinePlayer toPlayer) {}
        public virtual void ForgetPlayer(OnlinePlayer player) {}
        public virtual void ForgetEverything() {}


        // public void SendP2P(OnlinePlayer player, Packet packet, SendType sendType)
        // {
        //     var localPlayerId = player.id as LocalMatchmakingManager.LocalPlayerId;
        //     MemoryStream memory = new MemoryStream(128);
        //     BinaryWriter writer = new BinaryWriter(memory);

        //     Packet.Encode(packet, writer, player);

        //     byte[] bytes = memory.GetBuffer();

        //     UdpPeer.Send(localPlayerId.endPoint, bytes, (int)memory.Position,
        //         sendType switch
        //         {
        //             SendType.Reliable => UdpPeer.PacketType.Reliable,
        //             SendType.Unreliable => UdpPeer.PacketType.Unreliable,
        //             _ => UdpPeer.PacketType.Unreliable,
        //         });
        // }

        public virtual void Update()
        {
               RecieveData();
        }

        public abstract void RecieveData();

        // public void ReceiveDataLocal()
        // {
        //     UdpPeer.Update();

        //     while (UdpPeer.IsPacketAvailable())
        //     {
        //         try
        //         {
        //             //RainMeadow.Debug("To read: " + UdpPeer.debugClient.Available);
        //             if (!UdpPeer.Read(out BinaryReader netReader, out IPEndPoint remoteEndpoint))
        //                 continue;
        //             if (netReader.BaseStream.Position == ((MemoryStream)netReader.BaseStream).Length) continue; // nothing to read somehow?
        //             var player = (MatchmakingManager.instance as LocalMatchmakingManager).GetPlayerLocal(remoteEndpoint.Port);
        //             if (player == null)
        //             {
        //                 RainMeadow.Debug("Player not found! Instantiating new at: " + remoteEndpoint.Port);
        //                 player = new OnlinePlayer(new LocalMatchmakingManager.LocalPlayerId(remoteEndpoint.Port, remoteEndpoint, remoteEndpoint.Port == UdpPeer.STARTING_PORT));
        //             }

        //             Packet.Decode(netReader, player);
        //         }
        //         catch (Exception e)
        //         {
        //             RainMeadow.Error(e);
        //             OnlineManager.serializer.EndRead();
        //         }
        //     }
        // }

        // public void ReceiveDataSteam()
        // {
        //     lock (OnlineManager.serializer)
        //     {
        //         int n;
        //         IntPtr[] messages = new IntPtr[32];
        //         do // process in batches
        //         {
        //             n = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messages, messages.Length);
        //             for (int i = 0; i < n; i++)
        //             {
        //                 var message = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
        //                 try
        //                 {
        //                     if (OnlineManager.lobby != null)
        //                     {

        //                         var fromPlayer = (MatchmakingManager.instance as SteamMatchmakingManager).GetPlayerSteam(message.m_identityPeer.GetSteamID().m_SteamID);
        //                         if (fromPlayer == null)
        //                         {
        //                             RainMeadow.Error("player not found: " + message.m_identityPeer + " " + message.m_identityPeer.GetSteamID());
        //                             continue;
        //                         }
        //                         //RainMeadow.Debug($"Receiving message from {fromPlayer}");
        //                         Marshal.Copy(message.m_pData, OnlineManager.serializer.buffer, 0, message.m_cbSize);
        //                         OnlineManager.serializer.ReadData(fromPlayer, message.m_cbSize);
        //                     }
        //                 }
        //                 catch (Exception e)
        //                 {
        //                     RainMeadow.Error("Error reading packet from player : " + message.m_identityPeer.GetSteamID());
        //                     RainMeadow.Error(e);
        //                     OnlineManager.serializer.EndRead();
        //                     //throw;
        //                 }
        //                 finally
        //                 {
        //                     SteamNetworkingMessage_t.Release(messages[i]);
        //                 }
        //             }
        //         }
        //         while (n > 0);
        //     }
        // }

 
    }
}
