using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RainMeadow
{
    
    public class LANNetIO : NetIO {
        public readonly UDPPeerManager manager;
        public LANNetIO() {
            manager = new();
        }


        public override void SendP2P(OnlinePlayer player, Packet packet, SendType sendType) {
            if (MatchmakingManager.currentMatchMaker != MatchmakingManager.MatchMaker.Local) {
                return;
            }

            MemoryStream memory = new MemoryStream(128);
            BinaryWriter writer = new BinaryWriter(memory);

            Packet.Encode(packet, writer, player);
            manager.Send(memory.GetBuffer(), ((LANMatchmakingManager.LANPlayerId)player.id).endPoint, sendType switch
                {
                     NetIO.SendType.Reliable => UDPPeerManager.PacketType.Reliable,
                     NetIO.SendType.Unreliable => UDPPeerManager.PacketType.Unreliable,
                     _ => UDPPeerManager.PacketType.Unreliable,
                 }, true);
        }

        public override void Update()
        {
            base.Update();
            manager.Update();
        }

        public override void RecieveData()
        {
            if (MatchmakingManager.currentMatchMaker != MatchmakingManager.MatchMaker.Local) {
                return;
            }

            
            while (manager.IsPacketAvailable())
            {
                try
                {
                    //RainMeadow.Debug("To read: " + UdpPeer.debugClient.Available);
                    byte[]? data = manager.Recieve(out EndPoint remoteEndpoint);
                    if (data == null) continue;
                    IPEndPoint? iPEndPoint = remoteEndpoint as IPEndPoint;
                    if (iPEndPoint is null) continue;
                    
                    MemoryStream netStream = new MemoryStream(data);
                    BinaryReader netReader = new BinaryReader(netStream);
                    
                    if (netReader.BaseStream.Position == ((MemoryStream)netReader.BaseStream).Length) continue; // nothing to read somehow?
                    var player = (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).GetPlayerLAN(iPEndPoint);
                    if (player == null)
                    {
                        RainMeadow.Debug("Player not found! Instantiating new at: " + iPEndPoint.Port);
                        var playerid = new LANMatchmakingManager.LANPlayerId(iPEndPoint);
                        player = new OnlinePlayer(playerid);
                    }
                    
                    Packet.Decode(netReader, player);
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                    OnlineManager.serializer.EndRead();
                }
            }
        }

    }
}