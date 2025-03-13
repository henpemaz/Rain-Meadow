using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using UnityEngine;

namespace RainMeadow
{
//     public static class UdpPeer
//     {
//         public static float simulatedLoss = 0.05f;
//         public static float simulatedChainLoss = 0.4f;
//         public static float simulatedLatency = 80; //80   turns on delay;
//         public static float simulatedJitter = 100;
//         public static float simulatedJitterPower = 2;

//         public static System.Random random = new System.Random();

//         public static UdpClient udpClient;
//         public static int port;
//         public static IPEndPoint ownEndPoint;

//         public enum PacketType : byte
//         {
//             Reliable,
//             Unreliable,
//             UnreliableOrdered,
//             Acknowledge,
//             Termination,
//             Heartbeat,
//         }

//         public class SequencedPacket
//         {
//             public ulong index;
//             public byte[] packet; // Raw packet data
//             public Action OnAcknowledged;
//             public Action OnFailed;
//             public int attemptsLeft;

//             public SequencedPacket(ulong index, byte[] data, int length, int attempts = -1, bool termination = false)
//             {
//                 this.index = index;
//                 this.attemptsLeft = attempts;

//                 packet = new byte[9 + length];
//                 MemoryStream stream = new MemoryStream(packet);
//                 BinaryWriter writer = new BinaryWriter(stream);

//                 if (termination)
//                 {
//                     WriteTerminationHeader(writer, index);
//                 }
//                 else
//                 {
//                     WriteReliableHeader(writer, index);
//                 }
//                 writer.Write(data, 0, length);
//             }
//         }

//         private class RemotePeer
//         {
//             public byte[] machash = {};
//             public ulong packetIndex { get; set; } // Increments for each reliable packet sent
//             public ulong unreliablePacketIndex; // Increments for each unreliable ordered packet sent
//             public Queue<SequencedPacket> outgoingPackets = new Queue<SequencedPacket>(); // Keep track of packets we want to send while we wait for responses
//             public SequencedPacket latestOutgoingPacket { get; set; }
//             public int ticksSinceLastPacketReceived;
//             public int ticksSinceLastPacketSent;
//             public int ticksToResend = RESEND_TICKS;
//             public ulong lastAckedPacketIndex;
//             public ulong lastUnreliablePacketIndex;
//             internal bool loss;
//         }
//         public static bool waitingForTermination;
//         private static Dictionary<IPEndPoint, RemotePeer> peers;
//         private const int TIMEOUT_TICKS = 40 * 30; // about 30 seconds
//         private const int HEARTBEAT_TICKS = 40 * 5; // about 5 seconds
//         private const int RESEND_TICKS = 4; // about 0.1 seconds

//         //

//         public const int STARTING_PORT = 8720;

//         private class DelayedPacket
//         {
//             private DateTime timeToSend;
//             private IPEndPoint destination;
//             public byte[] packet;

//             public bool willSend => DateTime.Now > timeToSend;

//             public DelayedPacket(IPEndPoint destination, byte[] data, TimeSpan delay)
//             {
//                 this.destination = destination;
//                 this.packet = data;
//                 timeToSend = DateTime.Now + delay;
//             }

//             public void Send()
//             {
//                 udpClient.Send(packet, packet.Length, destination);
//                 //RainMeadow.Debug("Sent: " + packet.Length);
//             }
//         }

//         private static Queue<DelayedPacket> delayedPackets;

//         public static void Startup()
//         {
//             if (udpClient != null)
//                 return;

//             // Create udp client for local connection
//             udpClient = new UdpClient();

//             // With this set, it will be truely connectionless
//             udpClient.Client.IOControl(
//                 (IOControlCode)(-1744830452), // SIO_UDP_CONNRESET
//                 new byte[] { 0, 0, 0, 0 },
//                 null
//             );

//             port = STARTING_PORT;
//             for (int i = 0; i < 4; i++)
//             { // 4 tries
//                 bool alreadyinuse = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Any(p => p.Port == port);
//                 if (!alreadyinuse)
//                     break;

//                 RainMeadow.Debug($"Port {port} is already being used, incrementing...");

//                 port++;
//             }

//             ownEndPoint = new IPEndPoint(IPAddress.Any, port);
//             udpClient.Client.Bind(ownEndPoint);
//             peers = new Dictionary<IPEndPoint, RemotePeer>();
//             delayedPackets = new Queue<DelayedPacket>();
//         }

//         public static void Shutdown()
//         {
//             RainMeadow.DebugMe();
//             SendTermination();
//         }

//         private static void CleanUp()
//         {
//             if (!udpClient.Client.Connected || peers.Any(peer => peer.Value.outgoingPackets.Count > 0))
//                 return;

//             RainMeadow.DebugMe();
//             udpClient.Client.Shutdown(SocketShutdown.Both);
//             udpClient = null;
//             peers = null;
//             delayedPackets = null;
//             waitingForTermination = false;
//         }

//         public static void Update()
//         {
//             if (udpClient == null)
//                 return;

//             while (delayedPackets.Count > 0 && delayedPackets.Peek().willSend)
//             {
//                 delayedPackets.Dequeue().Send();
//             }

//             List<IPEndPoint> timedoutEndpoints = new List<IPEndPoint>();
//             foreach (var peer in peers)
//             {
//                 var peerIP = peer.Key;
//                 var peerData = peer.Value;

//                 peerData.ticksSinceLastPacketSent++;
//                 if (peerData.ticksSinceLastPacketSent > HEARTBEAT_TICKS)
//                 {
//                     // Send to heartbeat, do not need an acknowledge if remote is doing the same
//                     Send(peerIP, new byte[0], 0, PacketType.Unreliable);
//                 }

//                 peerData.ticksSinceLastPacketReceived++;
//                 if (peerData.ticksSinceLastPacketReceived > TIMEOUT_TICKS)
//                 {
//                     // Peer timed out and assume disconnected
//                     RainMeadow.Debug($"Peer {peerIP} timed out :c");
//                     timedoutEndpoints.Add(peerIP);
//                     continue;
//                 }

//                 // Try to resend packets that have not been acknowledge on the other end
//                 if (peerData.outgoingPackets.Count > 0)
//                 {
//                     peerData.ticksToResend--;
//                     if (peerData.ticksToResend <= 0)
//                     {
//                         SequencedPacket outgoingPacket = peerData.outgoingPackets.Peek();
//                         byte[] packetData = outgoingPacket.packet;

//                         RainMeadow.Debug($"Resending packet #{outgoingPacket.index}");
//                         Send(packetData, peerIP);

//                         outgoingPacket.attemptsLeft--;
//                         if (outgoingPacket.attemptsLeft == 0)
//                             peerData.outgoingPackets.Dequeue().OnFailed?.Invoke();

//                         peerData.ticksToResend = RESEND_TICKS;
//                     }
//                 }
//             }

//             foreach (IPEndPoint endPoint in timedoutEndpoints)
//             {
//                 peers.Remove(endPoint);
//             }
//         }

//         static byte[]? _machash = null;
//         public static byte[] getOurMacHash() {
//             if (_machash == null) {
//                 var processbuffer = BitConverter.GetBytes(IPAddress.NetworkToHostOrder(Process.GetCurrentProcess().Id));
//                 _machash = new MD5CryptoServiceProvider().ComputeHash(
//                             processbuffer.Concat(NetworkInterface.GetAllNetworkInterfaces().Where(
//                                 x=>x.OperationalStatus == OperationalStatus.Up).FirstOrDefault().GetPhysicalAddress().GetAddressBytes()).ToArray()
//                         );
//             }

//             return _machash;

//         }

//         public static bool Read(out BinaryReader netReader, out IPEndPoint remoteEndpoint)
//         {
//             remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
//             MemoryStream netStream = new MemoryStream(udpClient.Receive(ref remoteEndpoint));
//             netReader = new BinaryReader(netStream);

//             PacketType type = (PacketType)netReader.ReadByte();
//             //RainMeadow.Debug("Got packet meta-type: " + type);

//             if (!peers.TryGetValue(remoteEndpoint, out RemotePeer peerData))
//             {
//                 RainMeadow.Debug($"Communicating with new peer {remoteEndpoint}");
//                 peerData = new RemotePeer();
//                 peers[remoteEndpoint] = peerData;
//             }

//             if (simulatedLoss > 0)
//             {
//                 if (simulatedLoss > random.NextDouble() || (peerData.loss && simulatedChainLoss > random.NextDouble()))
//                 {
//                     // packet loss
//                     peerData.loss = true;
//                     return false;
//                 }
//                 peerData.loss = false;
//             }

//             peerData.ticksSinceLastPacketReceived = 0;

//             ulong receivedPacketIndex;
//             switch (type)
//             {
//                 case PacketType.Reliable:
//                     receivedPacketIndex = ReadReliableHeader(netReader);
//                     SendAcknowledge(remoteEndpoint, receivedPacketIndex); // Return Message

//                     // Process data if it is new
//                     if (receivedPacketIndex > peerData.lastAckedPacketIndex)
//                     {
//                         peerData.lastAckedPacketIndex = receivedPacketIndex;
//                         return true;
//                     }
//                     break;

//                 case PacketType.Acknowledge:
//                     if (peerData.outgoingPackets.Count == 0)
//                         // Nothing left to acknowledge
//                         return false;

//                     receivedPacketIndex = ReadAcknowledge(netReader);
//                     if (peerData.outgoingPackets.Peek().index == receivedPacketIndex)
//                     {
//                         SequencedPacket acknowledgedPacket = peerData.outgoingPackets.Dequeue();
//                         acknowledgedPacket.OnAcknowledged?.Invoke();

//                         if (acknowledgedPacket == peerData.latestOutgoingPacket)
//                         {
//                             peerData.latestOutgoingPacket = null;
//                         }

//                         // Attempt to send the next reliable one if any
//                         if (peerData.outgoingPackets.Count > 0)
//                         {
//                             byte[] packetData = peerData.outgoingPackets.Peek().packet;
//                             Send(packetData, remoteEndpoint);
//                             peerData.ticksToResend = RESEND_TICKS;
//                         }
//                     }
//                     break;

//                 case PacketType.Unreliable:
//                     return true;

//                 case PacketType.UnreliableOrdered:
//                     receivedPacketIndex = ReadUnreliableOrderedHeader(netReader);

//                     // Process data if it is latest
//                     if (receivedPacketIndex > peerData.lastUnreliablePacketIndex)
//                     {
//                         peerData.lastUnreliablePacketIndex = receivedPacketIndex;
//                         return true;
//                     }
//                     break;

//                 case PacketType.Termination:
//                     receivedPacketIndex = ReadTerminationHeader(netReader);
//                     SendAcknowledge(remoteEndpoint, receivedPacketIndex); // Return Message

//                     // Do not need to check for order if peer really wants to leave now
//                     peers.Remove(remoteEndpoint);
//                     RainMeadow.Debug($"Peer {remoteEndpoint} terminated connection");
//                     return false;

//                 case PacketType.Heartbeat:
//                     break; // Do nothing
//             }

//             return false;
//         }

//         public static bool IsPacketAvailable()
//         {
//             return udpClient != null && udpClient.Available > 0;
//         }

//         public static void Send(IPEndPoint remoteEndpoint, byte[] data, int length, PacketType packetType)
//         {
//             if (udpClient == null || waitingForTermination)
//                 return;

//             if (!peers.TryGetValue(remoteEndpoint, out RemotePeer peerData))
//             {
//                 RainMeadow.Debug($"Communicating with new peer {remoteEndpoint}");
//                 peerData = new RemotePeer();
//                 peers[remoteEndpoint] = peerData;
//             }

//             peerData.ticksSinceLastPacketSent = 0;

//             byte[] buffer = null;
//             byte[] packetData;
//             switch (packetType)
//             {
//                 case PacketType.Reliable:
//                     peerData.latestOutgoingPacket = new SequencedPacket(++peerData.packetIndex, data, length);
//                     peerData.outgoingPackets.Enqueue(peerData.latestOutgoingPacket);

//                     SequencedPacket outgoingPacket = peerData.outgoingPackets.Peek();
//                     buffer = outgoingPacket.packet;
//                     break;

//                 case PacketType.Unreliable:
//                     buffer = new byte[1 + length];
//                     MemoryStream stream = new MemoryStream(buffer);
//                     BinaryWriter writer = new BinaryWriter(stream);

//                     WriteUnreliableHeader(writer);
//                     writer.Write(data, 0, length);

//                     break;

//                 case PacketType.UnreliableOrdered:
//                     buffer = new byte[9 + length];
//                     stream = new MemoryStream(9 + length);
//                     writer = new BinaryWriter(stream);

//                     WriteUnreliableOrderedHeader(writer, ++peerData.unreliablePacketIndex);
//                     writer.Write(data, 0, length);
//                     break;
//                 default: throw new ArgumentException("UNHANDLED PACKETTYPE");
//             }

//             if (buffer == null)
//                 return;

//             Send(buffer, remoteEndpoint);
//         }

//         private static void Send(byte[] packet, IPEndPoint endPoint)
//         {
//             if (simulatedLatency > 0)
//             {
//                 delayedPackets.Enqueue(new DelayedPacket(endPoint, packet, TimeSpan.FromMilliseconds(simulatedLatency + simulatedJitter * Mathf.Pow((float)random.NextDouble(), simulatedJitterPower))));
//             }
//             else
//             {
//                 udpClient.Send(packet, packet.Length, endPoint);
//                 //RainMeadow.Debug("sent: " + packet.Length);
//             }
//         }

//         private static void SendAcknowledge(IPEndPoint remoteEndpoint, ulong index)
//         {
//             RainMeadow.Debug($"Sending acknowledge for packet #{index}");

//             byte[] buffer = new byte[9];
//             MemoryStream stream = new MemoryStream(buffer);
//             BinaryWriter writer = new BinaryWriter(stream);

//             WriteAcknowledge(writer, index);
//             Send(buffer, remoteEndpoint);
//         }

//         private static void SendTerminationHeader(IPEndPoint remoteEndpoint, ulong index)
//         {
//             RainMeadow.Debug($"Sending acknowledge for packet #{index}");

//             byte[] buffer = new byte[9];
//             MemoryStream stream = new MemoryStream(buffer);
//             BinaryWriter writer = new BinaryWriter(stream);

//             WriteTerminationHeader(writer, index);
//             Send(buffer, remoteEndpoint);
//         }

//         private static void SendTermination()
//         {
//             RainMeadow.Debug($"Sending all known peers a final message!");

//             foreach (var peer in peers)
//             {
//                 var peerIP = peer.Key;
//                 var peerData = peer.Value;

//                 peerData.outgoingPackets.Clear();

//                 peerData.latestOutgoingPacket = new SequencedPacket(++peerData.packetIndex, new byte[0], 0, 10, true);
//                 peerData.outgoingPackets.Enqueue(peerData.latestOutgoingPacket);

//                 peerData.latestOutgoingPacket.OnAcknowledged += CleanUp;
//                 peerData.latestOutgoingPacket.OnFailed += CleanUp;

//                 byte[] packetData = peerData.outgoingPackets.Peek().packet;
//                 Send(packetData, peerIP);
//             }

//             waitingForTermination = true;
//         }

//         //

//         /// <summary>Writes 9 bytes</summary>
//         private static void WriteReliableHeader(BinaryWriter writer, ulong index)
//         {
//             writer.Write((byte)PacketType.Reliable);
//             writer.Write(index);
//         }

//         private static ulong ReadReliableHeader(BinaryReader reader)
//         {
//             // Ignore type
//             ulong index = reader.ReadUInt64();
//             return index;
//         }

//         //

//         /// <summary>Writes 1 byte</summary>
//         private static void WriteUnreliableHeader(BinaryWriter writer)
//         {
//             writer.Write((byte)PacketType.Unreliable);
//         }

//         private static void ReadUnreliableHeader(BinaryReader reader)
//         {
//             // Ignore type
//         }

//         //

//         /// <summary>Writes 9 bytes</summary>
//         private static void WriteUnreliableOrderedHeader(BinaryWriter writer, ulong index)
//         {
//             writer.Write((byte)PacketType.UnreliableOrdered);
//             writer.Write(index);
//         }

//         private static ulong ReadUnreliableOrderedHeader(BinaryReader reader)
//         {
//             // Ignore type
//             ulong index = reader.ReadUInt64();
//             return index;
//         }

//         //

//         /// <summary>Writes 9 bytes</summary>
//         private static void WriteAcknowledge(BinaryWriter writer, ulong index)
//         {
//             writer.Write((byte)PacketType.Acknowledge);
//             writer.Write(index);
//         }

//         private static ulong ReadAcknowledge(BinaryReader reader)
//         {
//             // Ignore type
//             ulong index = reader.ReadUInt64();
//             return index;
//         }

//         //

//         /// <summary>Writes 9 bytes</summary>
//         private static void WriteTerminationHeader(BinaryWriter writer, ulong index)
//         {
//             writer.Write((byte)PacketType.Termination);
//             writer.Write(index);
//         }

//         private static ulong ReadTerminationHeader(BinaryReader reader)
//         {
//             // Ignore type
//             ulong index = reader.ReadUInt64();
//             return index;
//         }
//     }

    public class UDPPeerManager : IDisposable
    {
        public bool IsDisposed { get => _isDisposed; }
        private bool _isDisposed = false;
        public Socket socket;
        public int port;

        public enum PacketType : byte
        {
            Unreliable = 0,
            UnreliableBroadcast,
            Reliable,
            Acknowledgement,
            HeartBeat,
        }
        

        public class RemotePeer {
            public IPEndPoint PeerEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
            public ulong TicksSinceLastIncomingPacket = 0;
            public ulong OutgoingPacketAcummulator = 0;


            public Queue<byte[]> outgoingpacket = new Queue<byte[]>();
            public ulong wanted_acknowledgement = 0;
            public ulong remote_acknowledgement = 0;
            
        }
        public const int DEFAULT_PORT = 8720;
        public const int FIND_PORT_ATTEMPTS = 8; // 8 players somehow hosting from the same machine is ridiculous.
        public UDPPeerManager() {
            try {
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                this.socket.EnableBroadcast = true;

                port = DEFAULT_PORT;
                var activeUdpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
                bool alreadyinuse = false;
                for (int i = 0; i < FIND_PORT_ATTEMPTS; i++) { 
                    port = DEFAULT_PORT + i;
                    alreadyinuse = activeUdpListeners.Any(p => p.Port == port);
                    if (!alreadyinuse)
                        break;
                }

                if (alreadyinuse) {
                    throw new Exception("Failed to claim a socket port");
                }


                socket.Bind(new IPEndPoint(IPAddress.Any, port));
            } catch (SocketException except) {
                RainMeadow.Error(except.SocketErrorCode);
                throw except;
            }
            
        }

        List<RemotePeer> peers = new();
        public RemotePeer? GetRemotePeer(IPEndPoint endPoint, bool make_one = false) {
            RemotePeer? peer = peers.FirstOrDefault(x => CompareIPEndpoints(endPoint, x.PeerEndPoint));
            if (peer == null && make_one) {
                peer = new RemotePeer() {PeerEndPoint = endPoint};
                peers.Add(peer);
            }

            return peer;
        }


        static IPAddress[] interface_addresses = null;
        static public IPAddress[] getInterfaceAddresses() {
            if (interface_addresses == null) {
                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                var adapter_interface_addresses = adapters.Where(x =>
                    x.Supports(NetworkInterfaceComponent.IPv4) &&
                        (x.NetworkInterfaceType is NetworkInterfaceType.Ethernet ||
                         x.NetworkInterfaceType is NetworkInterfaceType.Wireless80211) &&
                        x.OperationalStatus == OperationalStatus.Up
                    )
                    .Select(x => x.GetIPProperties().UnicastAddresses)
                    .SelectMany(u => u)
                    .Select(u => u.Address)
                    .Where(u => u.AddressFamily == AddressFamily.InterNetwork && u != IPAddress.Loopback);
                if (!adapter_interface_addresses.Contains(IPAddress.Loopback))
                    adapter_interface_addresses = adapter_interface_addresses.Append(IPAddress.Loopback);
                interface_addresses = adapter_interface_addresses.ToArray();
                foreach(var addr in interface_addresses) {
                    RainMeadow.Debug(addr);
                }
            }

            return interface_addresses;
        }

        static public bool isLoopback(IPAddress address) {
            return getInterfaceAddresses().Contains(address);
        }
        public static bool CompareIPEndpoints(IPEndPoint a, IPEndPoint b) {
            if (!a.Port.Equals(b.Port)) {
                return false;
            }

            if (isLoopback(a.Address) && isLoopback(b.Address)) return true;

            return a.Address.MapToIPv4().Equals(b.Address.MapToIPv4());
        }
        public static bool isEndpointLocal(IPEndPoint endpoint) {
            var addressbytes = endpoint.Address.GetAddressBytes();
            if (endpoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                if (addressbytes[0] == 10) return true;

                if (addressbytes[0] == 172)
                if (addressbytes[1] >= 16 && addressbytes[1] <= 31) return true;


                if (addressbytes[0] == 192)
                if (addressbytes[1] == 168) return true;

                if (endpoint.Address.Equals(IPAddress.Loopback)) return true;
            }
            return false;
        }

        public delegate void OnPeerForgotten_t(IPEndPoint endPoint);
        public event OnPeerForgotten_t OnPeerForgotten = delegate { };
        
        
        public void ForgetPeer(RemotePeer peer) {
            OnPeerForgotten.Invoke(peer.PeerEndPoint);
            peers.Remove(peer);
        }
        public void ForgetPeer(IPEndPoint endPoint) {
            var remove_peers = peers.FindAll(x => CompareIPEndpoints(endPoint, x.PeerEndPoint));
            foreach (RemotePeer peer in remove_peers) {
                ForgetPeer(peer);
            }
        }

        public void ForgetAllPeers() {
            var remove_peers = peers.ToList();
            foreach (RemotePeer peer in remove_peers) {
                ForgetPeer(peer);
            }
        }
        public static IPEndPoint? GetEndPointByName(string name)
        {
            string[] parts = name.Split(':');
            if (parts.Length != 2) {
                RainMeadow.Debug("Invalid IP format without colon: " + name);
                return null;
            }


            IPAddress? address = null;
            try {
                address = IPAddress.Parse(parts[0]);
            } catch (FormatException) {
                try {
                    address = Dns.GetHostEntry(parts[0])
                        .AddressList
                        .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                        .FirstOrDefault();
                } catch (SocketException) {
                    if (address == null) return null;
                }
            }

            if (!short.TryParse(parts[1], out short port)) {
                RainMeadow.Debug("Invalid port format: " + parts[1]);
                return null;
            }
            
            return new IPEndPoint(address, port);
        }


        public void Send(byte[] packet, IPEndPoint endPoint, PacketType packet_type = PacketType.Reliable, bool begin_conversation = false) {
            //packet = Compression.CompressBytes(packet); //done in SendRaw instead
            if (GetRemotePeer(endPoint, true) is RemotePeer peer) {
                if (packet_type == PacketType.Reliable) {
                    peer.wanted_acknowledgement += 1;
                    peer.outgoingpacket.Enqueue(packet);
                    if (peer.outgoingpacket.Count > 0) {
                        SendRaw(peer.outgoingpacket.Peek(), peer, packet_type, begin_conversation);
                    }
                } else {
                    SendRaw(packet, peer, packet_type);
                }
            } else RainMeadow.Error("Failed to get remote peer");
        }

        public void SendRaw(byte[] packet, RemotePeer peer, PacketType packet_type, bool begin_conversation = false) {
            using (MemoryStream stream = new(packet.Length + 1)) 
            using (BinaryWriter writer = new(stream)) {
                writer.Write((byte)packet_type);
                if (packet_type == PacketType.Reliable)  {
                    writer.Write(begin_conversation);
                    writer.Write(peer.wanted_acknowledgement);
                }
                    

                if (packet_type == PacketType.Acknowledgement)
                    writer.Write(peer.remote_acknowledgement);
                writer.Write(packet);
                //compressed as late as possible
                socket.SendTo(Compression.CompressBytes(stream.GetBuffer()), peer.PeerEndPoint);
            }
        }

        const ulong PEER_TIMEOUT = 1000*3; 
        const ulong HEARTBEAT_TIME= 500; 
        

        Stopwatch stopWatch = new Stopwatch();
        public void Update() {
            long elapsedTime = stopWatch.ElapsedMilliseconds;
            stopWatch.Restart();

            List<RemotePeer> peersToRemove = new();
            for (int i = peers.Count - 1; i >= 0; i--) {
                RemotePeer peer = peers[i];
                peer.TicksSinceLastIncomingPacket += (ulong)elapsedTime;
                if (peer.TicksSinceLastIncomingPacket >= PEER_TIMEOUT) {
                    peersToRemove.Add(peer);
                    continue;
                }

                peer.OutgoingPacketAcummulator += (ulong)elapsedTime;
                ulong sendAmount; sendAmount = peer.OutgoingPacketAcummulator / HEARTBEAT_TIME;
                if (sendAmount > 1)  {
                    peer.OutgoingPacketAcummulator -= sendAmount*HEARTBEAT_TIME;
                    peer.OutgoingPacketAcummulator = Math.Max(peer.OutgoingPacketAcummulator, 0); // just to be sure

                    for (ulong j = 0; j < sendAmount; j++) {
                        if (peer.outgoingpacket.Count > 0) {
                            SendRaw(peer.outgoingpacket.Peek(), peer, PacketType.Reliable);
                        } else if (peer.TicksSinceLastIncomingPacket > HEARTBEAT_TIME) {
                            SendRaw(Array.Empty<byte>(), peer, PacketType.HeartBeat);
                        }
                    }


                }

            }

            foreach (var peer in peersToRemove) ForgetPeer(peer);
        }
        public bool IsPacketAvailable() { return socket.Available > 0; }
        public static void SerializeEndPoints(BinaryWriter writer, IPEndPoint[] endPoints, IPEndPoint addressedto, bool includeme = true) {
            writer.Write(includeme);
            writer.Write((int)endPoints.Length);
            foreach (IPEndPoint point in endPoints) {
                var sendpoint = point;
                if (CompareIPEndpoints(point, addressedto)) {
                    sendpoint = new IPEndPoint(IPAddress.Loopback, point.Port);
                }

                writer.Write(sendpoint.Address.MapToIPv4().GetAddressBytes()); // writes 4 bytes.
                writer.Write((int)sendpoint.Port);
            }
        }

        public static IPEndPoint[] DeserializeEndPoints(BinaryReader reader, IPEndPoint fromWho) {
            bool includesender = reader.ReadBoolean();
            IPEndPoint[] ret = new IPEndPoint[reader.ReadInt32() + (includesender? 1 : 0)];
            int i = 0;
            if (includesender) {
                ret[i] = fromWho;
                ++i;
            }

            for (; i != ret.Length; i++) {
                byte[] address_bytes = reader.ReadBytes(4);
                int port = reader.ReadInt32();
                ret[i] = new IPEndPoint(new IPAddress(address_bytes), port);
            }

            return ret.ToArray();
        }

        public byte[]? Recieve(out EndPoint? sender) {
            sender = null;

            if (socket.Available != 0) {
                sender = new IPEndPoint(IPAddress.Loopback, 8720);
                
                byte[] buffer = new byte[socket.Available];
                int len = 0;
                try {
                    len = socket.ReceiveFrom(buffer, socket.Available, SocketFlags.None, ref sender);
                } catch (SocketException except) {
                    RainMeadow.Error(except.SocketErrorCode);
                    return null;
                }

                //decompressed as early as possible
                buffer = Compression.DecompressBytes(buffer, len);
                len = buffer.Length;
                

                IPEndPoint? ipsender = sender as IPEndPoint;
                if (ipsender == null) return null; 

                RemotePeer? peer = GetRemotePeer(ipsender);
                

                


                

                using (MemoryStream stream = new(buffer, 0, len, false)) 
                using (BinaryReader reader = new(stream)) {
                    try {
                        PacketType type = (PacketType)reader.ReadByte();
                        
                        if (type == PacketType.Reliable) {
                            bool begin_conversation = reader.ReadBoolean();
                            if (begin_conversation && peer == null) {
                                peer = GetRemotePeer(ipsender, true);
                            }
                        }

                        if (type != PacketType.UnreliableBroadcast) // If it's a broadcast, we don't need to start a converstation.
                        if (peer == null) {
                            RainMeadow.Debug("Recieved packet from peer we haven't started a conversation with.");
                            RainMeadow.Debug(Enum.GetName(typeof(PacketType), type));
                            return null;
                        }

                        if (peer != null) peer.TicksSinceLastIncomingPacket = 0;



                        switch (type) {
                            case PacketType.UnreliableBroadcast:
                            case PacketType.Unreliable:
                                return reader.ReadBytes(len - 1);

                            case PacketType.Reliable:
                                if (peer == null) return null;

                                ulong wanted_ack = reader.ReadUInt64();
                                byte[]? new_data = null;
                                
                                if (EventMath.IsNewer(wanted_ack, peer.remote_acknowledgement)) {
                                    peer.remote_acknowledgement = wanted_ack;
                                    new_data = reader.ReadBytes(len - 1);
                                }
                                SendRaw(Array.Empty<byte>(), peer, PacketType.Acknowledgement);
                                return new_data;
                            case PacketType.Acknowledgement:
                                if (peer == null) return null;
                                ulong remote_ack = reader.ReadUInt64();
                                if (EventMath.IsNewerOrEqual(remote_ack, peer.wanted_acknowledgement)) {
                                    ++peer.wanted_acknowledgement;
                                    if (peer.outgoingpacket.Count > 0) {
                                        peer.outgoingpacket.Dequeue();
                                    }
                                }
                                return null;

                            case PacketType.HeartBeat:
                                if (peer == null) return null;
                                SendRaw(Array.Empty<byte>(), peer, PacketType.HeartBeat);
                                return null;

                            default:
                                return null; // Ignore it.
                        }
                    } catch (Exception except) {
                        RainMeadow.Debug(except);
                        RainMeadow.Debug($"Error: {except.Message}");
                        return null;
                    }
                }
            }
            return null;
        }



        

        void IDisposable.Dispose() {
            socket.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}