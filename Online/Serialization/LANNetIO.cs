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
            manager.OnPeerForgotten += (peer) => {
                if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN) {
                    return;
                }
                List<OnlinePlayer> playerstoRemove = new();
                foreach (OnlinePlayer player in OnlineManager.players) {
                    if (player.id is LANMatchmakingManager.LANPlayerId lanid) {
                        if (lanid.endPoint is null) continue;
                        if (UDPPeerManager.CompareIPEndpoints(lanid.endPoint, peer)) {
                            if ((OnlineManager.lobby?.owner is OnlinePlayer owner && owner == player) ||
                                (OnlineManager.lobby?.isOwner ?? true)
                            ) {
                                playerstoRemove.Add(player);
                                
                            }
                        }
                    }
                }

                foreach (var player in playerstoRemove) ((LANMatchmakingManager)MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN]).RemoveLANPlayer(player);
            };
        }

        public void SendBroadcast(Packet packet) {
            RainMeadow.DebugMe();
            for (int broadcast_port = UDPPeerManager.DEFAULT_PORT; 
                broadcast_port < (UDPPeerManager.FIND_PORT_ATTEMPTS + UDPPeerManager.DEFAULT_PORT); 
                broadcast_port++) {
                IPEndPoint point = new(IPAddress.Broadcast, broadcast_port);

                var player = (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN] as LANMatchmakingManager).GetPlayerLAN(point);
                if (player == null)
                {
                    RainMeadow.Debug("Player not found! Instantiating new at: " + point);
                    var playerid = new LANMatchmakingManager.LANPlayerId(point);
                    player = new OnlinePlayer(playerid);
                }

                MemoryStream memory = new MemoryStream(128);
                BinaryWriter writer = new BinaryWriter(memory);

                Packet.Encode(packet, writer, player);

                for (int i = 0; i < 4; i++)
                manager.Send(memory.GetBuffer(), ((LANMatchmakingManager.LANPlayerId)player.id).endPoint, 
                    UDPPeerManager.PacketType.UnreliableBroadcast, true);
                }
        }
        public override void SendP2P(OnlinePlayer player, Packet packet, SendType sendType, bool start_conversation = false) {
            if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN) {
                return;
            }

            MemoryStream memory = new MemoryStream(128);
            BinaryWriter writer = new BinaryWriter(memory);

            Packet.Encode(packet, writer, player);
            manager.Send(memory.GetBuffer(), ((LANMatchmakingManager.LANPlayerId)player.id).endPoint, sendType switch
                {
                     NetIO.SendType.Reliable => UDPPeerManager.PacketType.Reliable,
                     NetIO.SendType.Unreliable => start_conversation? UDPPeerManager.PacketType.UnreliableBroadcast : UDPPeerManager.PacketType.Unreliable,
                     _ => UDPPeerManager.PacketType.Unreliable,
                }, start_conversation);
        }


        public void SendAcknoledgement(OnlinePlayer player) {
            if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN) {
                return;
            }

            manager.Send(Array.Empty<byte>(), ((LANMatchmakingManager.LANPlayerId)player.id).endPoint, 
                UDPPeerManager.PacketType.Reliable, true);
        }

        public override void ForgetPlayer(OnlinePlayer player) {
            manager.ForgetPeer(((LANMatchmakingManager.LANPlayerId)player.id).endPoint);
        }

        public override void ForgetEverything() {
            manager.ForgetAllPeers();
        }

        public override void Update()
        {
            base.Update();
            manager.Update();
        }

        public override void RecieveData()
        {
            if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN) {
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
                    var player = (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.LAN] as LANMatchmakingManager).GetPlayerLAN(iPEndPoint);
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