using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace RainMeadow
{
    public class LANNetIO : NetIO {
        public LANNetIO() {
            UdpPeer.Startup();
        }


        public override void SendP2P(OnlinePlayer player, Packet packet, SendType sendType) {
            if (MatchmakingManager.currentMatchMaker != MatchmakingManager.MatchMaker.Local) {
                return;
            }

            MemoryStream memory = new MemoryStream(128);
            BinaryWriter writer = new BinaryWriter(memory);

            writer.Write((int)UdpPeer.getOurMacHash().Length);
            writer.Write((byte[])UdpPeer.getOurMacHash());
            Packet.Encode(packet, writer, player);
            UdpPeer.Send(((LANMatchmakingManager.LANPlayerId)player.id).endPoint, memory.GetBuffer(), (int)memory.Position,
                sendType switch
                {
                     NetIO.SendType.Reliable => UdpPeer.PacketType.Reliable,
                     NetIO.SendType.Unreliable => UdpPeer.PacketType.Unreliable,
                     _ => UdpPeer.PacketType.Unreliable,
                 });
        }

        public override void Update()
        {
            base.Update();
            UdpPeer.Update();
        }

        public override void RecieveData()
        {
            LANRecieveData();
        }

        public void LANRecieveData()
        {
            if (MatchmakingManager.currentMatchMaker != MatchmakingManager.MatchMaker.Local) {
                return;
            }

            
            while (UdpPeer.IsPacketAvailable())
            {
                try
                {
                    //RainMeadow.Debug("To read: " + UdpPeer.debugClient.Available);
                    if (!UdpPeer.Read(out BinaryReader netReader, out IPEndPoint remoteEndpoint))
                        continue;
                    if (netReader.BaseStream.Position == ((MemoryStream)netReader.BaseStream).Length) continue; // nothing to read somehow?
                    byte[] machash = netReader.ReadBytes(netReader.ReadInt32());
                    var player = (MatchmakingManager.instances[MatchmakingManager.MatchMaker.Local] as LANMatchmakingManager).GetPlayerLAN(machash);
                    if (player == null)
                    {
                        RainMeadow.Debug("Player not found! Instantiating new at: " + remoteEndpoint.Port);
                        var playerid = new LANMatchmakingManager.LANPlayerId(remoteEndpoint, false);
                        playerid.machash = machash;
                        player = new OnlinePlayer(playerid);
                    }

                    RainMeadow.Debug($"Recieved packet from: {remoteEndpoint.ToString()}");
                    
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