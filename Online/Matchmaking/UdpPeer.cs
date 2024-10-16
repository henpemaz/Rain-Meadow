using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

namespace RainMeadow
{
    public static class UdpPeer
    {
        public static float simulatedLoss = 0.05f;
        public static float simulatedChainLoss = 0.4f;
        public static float simulatedLatency = 80; //80   turns on delay;
        public static float simulatedJitter = 100;
        public static float simulatedJitterPower = 2;

        public static System.Random random = new System.Random();

        public static UdpClient udpClient;
        public static int port;
        public static IPEndPoint ownEndPoint;

        public enum PacketType : byte
        {
            Reliable,
            Unreliable,
            UnreliableOrdered,
            Acknowledge,
            Termination,
            Heartbeat,
        }

        public class SequencedPacket
        {
            public ulong index;
            public byte[] packet; // Raw packet data
            public Action OnAcknowledged;
            public Action OnFailed;
            public int attemptsLeft;

            public SequencedPacket(ulong index, byte[] data, int length, int attempts = -1, bool termination = false)
            {
                this.index = index;
                this.attemptsLeft = attempts;

                packet = new byte[9 + length];
                MemoryStream stream = new MemoryStream(packet);
                BinaryWriter writer = new BinaryWriter(stream);

                if (termination)
                {
                    WriteTerminationHeader(writer, index);
                }
                else
                {
                    WriteReliableHeader(writer, index);
                }
                writer.Write(data, 0, length);
            }
        }

        private class RemotePeer
        {
            public ulong packetIndex { get; set; } // Increments for each reliable packet sent
            public ulong unreliablePacketIndex; // Increments for each unreliable ordered packet sent
            public Queue<SequencedPacket> outgoingPackets = new Queue<SequencedPacket>(); // Keep track of packets we want to send while we wait for responses
            public SequencedPacket latestOutgoingPacket { get; set; }
            public int ticksSinceLastPacketReceived;
            public int ticksSinceLastPacketSent;
            public int ticksToResend = RESEND_TICKS;
            public ulong lastAckedPacketIndex;
            public ulong lastUnreliablePacketIndex;
            internal bool loss;
        }
        public static bool waitingForTermination;
        private static Dictionary<IPEndPoint, RemotePeer> peers;
        private const int TIMEOUT_TICKS = 40 * 30; // about 30 seconds
        private const int HEARTBEAT_TICKS = 40 * 5; // about 5 seconds
        private const int RESEND_TICKS = 4; // about 0.1 seconds

        //

        public const int STARTING_PORT = 8720;

        private class DelayedPacket
        {
            private DateTime timeToSend;
            private IPEndPoint destination;
            public byte[] packet;

            public bool willSend => DateTime.Now > timeToSend;

            public DelayedPacket(IPEndPoint destination, byte[] data, TimeSpan delay)
            {
                this.destination = destination;
                this.packet = data;
                timeToSend = DateTime.Now + delay;
            }

            public void Send()
            {
                udpClient.Send(packet, packet.Length, destination);
                //RainMeadow.Debug("Sent: " + packet.Length);
            }
        }

        private static Queue<DelayedPacket> delayedPackets;

        public static void Startup()
        {
            if (udpClient != null)
                return;

            // Create udp client for local connection
            udpClient = new UdpClient();

            // With this set, it will be truely connectionless
            udpClient.Client.IOControl(
                (IOControlCode)(-1744830452), // SIO_UDP_CONNRESET
                new byte[] { 0, 0, 0, 0 },
                null
            );

            port = STARTING_PORT;
            for (int i = 0; i < 4; i++)
            { // 4 tries
                bool alreadyinuse = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Any(p => p.Port == port);
                if (!alreadyinuse)
                    break;

                RainMeadow.Debug($"Port {port} is already being used, incrementing...");

                port++;
            }

            ownEndPoint = new IPEndPoint(IPAddress.Any, port);
            udpClient.Client.Bind(ownEndPoint);
            peers = new Dictionary<IPEndPoint, RemotePeer>();
            delayedPackets = new Queue<DelayedPacket>();
        }

        public static void Shutdown()
        {
            RainMeadow.DebugMe();
            SendTermination();
        }

        private static void CleanUp()
        {
            if (!udpClient.Client.Connected || peers.Any(peer => peer.Value.outgoingPackets.Count > 0))
                return;

            RainMeadow.DebugMe();
            udpClient.Client.Shutdown(SocketShutdown.Both);
            udpClient = null;
            peers = null;
            delayedPackets = null;
            waitingForTermination = false;
        }

        public static void Update()
        {
            if (udpClient == null)
                return;

            while (delayedPackets.Count > 0 && delayedPackets.Peek().willSend)
            {
                delayedPackets.Dequeue().Send();
            }

            List<IPEndPoint> timedoutEndpoints = new List<IPEndPoint>();
            foreach (var peer in peers)
            {
                var peerIP = peer.Key;
                var peerData = peer.Value;

                peerData.ticksSinceLastPacketSent++;
                if (peerData.ticksSinceLastPacketSent > HEARTBEAT_TICKS)
                {
                    // Send to heartbeat, do not need an acknowledge if remote is doing the same
                    Send(peerIP, new byte[0], 0, PacketType.Unreliable);
                }

                peerData.ticksSinceLastPacketReceived++;
                if (peerData.ticksSinceLastPacketReceived > TIMEOUT_TICKS)
                {
                    // Peer timed out and assume disconnected
                    RainMeadow.Debug($"Peer {peerIP} timed out :c");
                    timedoutEndpoints.Add(peerIP);
                    continue;
                }

                // Try to resend packets that have not been acknowledge on the other end
                if (peerData.outgoingPackets.Count > 0)
                {
                    peerData.ticksToResend--;
                    if (peerData.ticksToResend <= 0)
                    {
                        SequencedPacket outgoingPacket = peerData.outgoingPackets.Peek();
                        byte[] packetData = outgoingPacket.packet;

                        RainMeadow.Debug($"Resending packet #{outgoingPacket.index}");
                        Send(packetData, peerIP);

                        outgoingPacket.attemptsLeft--;
                        if (outgoingPacket.attemptsLeft == 0)
                            peerData.outgoingPackets.Dequeue().OnFailed?.Invoke();

                        peerData.ticksToResend = RESEND_TICKS;
                    }
                }
            }

            foreach (IPEndPoint endPoint in timedoutEndpoints)
            {
                peers.Remove(endPoint);
            }
        }

        public static bool Read(out BinaryReader netReader, out IPEndPoint remoteEndpoint)
        {
            remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            MemoryStream netStream = new MemoryStream(udpClient.Receive(ref remoteEndpoint));
            netReader = new BinaryReader(netStream);

            PacketType type = (PacketType)netReader.ReadByte();
            //RainMeadow.Debug("Got packet meta-type: " + type);

            if (!peers.TryGetValue(remoteEndpoint, out RemotePeer peerData))
            {
                RainMeadow.Debug($"Communicating with new peer {remoteEndpoint}");
                peerData = new RemotePeer();
                peers[remoteEndpoint] = peerData;
            }

            if (simulatedLoss > 0)
            {
                if (simulatedLoss > random.NextDouble() || (peerData.loss && simulatedChainLoss > random.NextDouble()))
                {
                    // packet loss
                    peerData.loss = true;
                    return false;
                }
                peerData.loss = false;
            }

            peerData.ticksSinceLastPacketReceived = 0;

            ulong receivedPacketIndex;
            switch (type)
            {
                case PacketType.Reliable:
                    receivedPacketIndex = ReadReliableHeader(netReader);
                    SendAcknowledge(remoteEndpoint, receivedPacketIndex); // Return Message

                    // Process data if it is new
                    if (receivedPacketIndex > peerData.lastAckedPacketIndex)
                    {
                        peerData.lastAckedPacketIndex = receivedPacketIndex;
                        return true;
                    }
                    break;

                case PacketType.Acknowledge:
                    if (peerData.outgoingPackets.Count == 0)
                        // Nothing left to acknowledge
                        return false;

                    receivedPacketIndex = ReadAcknowledge(netReader);
                    if (peerData.outgoingPackets.Peek().index == receivedPacketIndex)
                    {
                        SequencedPacket acknowledgedPacket = peerData.outgoingPackets.Dequeue();
                        acknowledgedPacket.OnAcknowledged?.Invoke();

                        if (acknowledgedPacket == peerData.latestOutgoingPacket)
                        {
                            peerData.latestOutgoingPacket = null;
                        }

                        // Attempt to send the next reliable one if any
                        if (peerData.outgoingPackets.Count > 0)
                        {
                            byte[] packetData = peerData.outgoingPackets.Peek().packet;
                            Send(packetData, remoteEndpoint);
                            peerData.ticksToResend = RESEND_TICKS;
                        }
                    }
                    break;

                case PacketType.Unreliable:
                    return true;

                case PacketType.UnreliableOrdered:
                    receivedPacketIndex = ReadUnreliableOrderedHeader(netReader);

                    // Process data if it is latest
                    if (receivedPacketIndex > peerData.lastUnreliablePacketIndex)
                    {
                        peerData.lastUnreliablePacketIndex = receivedPacketIndex;
                        return true;
                    }
                    break;

                case PacketType.Termination:
                    receivedPacketIndex = ReadTerminationHeader(netReader);
                    SendAcknowledge(remoteEndpoint, receivedPacketIndex); // Return Message

                    // Do not need to check for order if peer really wants to leave now
                    peers.Remove(remoteEndpoint);
                    RainMeadow.Debug($"Peer {remoteEndpoint} terminated connection");
                    return false;

                case PacketType.Heartbeat:
                    break; // Do nothing
            }

            return false;
        }

        public static bool IsPacketAvailable()
        {
            return udpClient != null && udpClient.Available > 0;
        }

        public static void Send(IPEndPoint remoteEndpoint, byte[] data, int length, PacketType packetType)
        {
            if (udpClient == null || waitingForTermination)
                return;

            if (!peers.TryGetValue(remoteEndpoint, out RemotePeer peerData))
            {
                RainMeadow.Debug($"Communicating with new peer {remoteEndpoint}");
                peerData = new RemotePeer();
                peers[remoteEndpoint] = peerData;
            }

            peerData.ticksSinceLastPacketSent = 0;

            byte[] buffer = null;
            byte[] packetData;
            switch (packetType)
            {
                case PacketType.Reliable:
                    peerData.latestOutgoingPacket = new SequencedPacket(++peerData.packetIndex, data, length);
                    peerData.outgoingPackets.Enqueue(peerData.latestOutgoingPacket);

                    SequencedPacket outgoingPacket = peerData.outgoingPackets.Peek();
                    buffer = outgoingPacket.packet;
                    break;

                case PacketType.Unreliable:
                    buffer = new byte[1 + length];
                    MemoryStream stream = new MemoryStream(buffer);
                    BinaryWriter writer = new BinaryWriter(stream);

                    WriteUnreliableHeader(writer);
                    writer.Write(data, 0, length);

                    break;

                case PacketType.UnreliableOrdered:
                    buffer = new byte[9 + length];
                    stream = new MemoryStream(9 + length);
                    writer = new BinaryWriter(stream);

                    WriteUnreliableOrderedHeader(writer, ++peerData.unreliablePacketIndex);
                    writer.Write(data, 0, length);
                    break;
                default: throw new ArgumentException("UNHANDLED PACKETTYPE");
            }

            if (buffer == null)
                return;

            Send(buffer, remoteEndpoint);
        }

        private static void Send(byte[] packet, IPEndPoint endPoint)
        {
            if (simulatedLatency > 0)
            {
                delayedPackets.Enqueue(new DelayedPacket(endPoint, packet, TimeSpan.FromMilliseconds(simulatedLatency + simulatedJitter * Mathf.Pow((float)random.NextDouble(), simulatedJitterPower))));
            }
            else
            {
                udpClient.Send(packet, packet.Length, endPoint);
                //RainMeadow.Debug("sent: " + packet.Length);
            }
        }

        private static void SendAcknowledge(IPEndPoint remoteEndpoint, ulong index)
        {
            RainMeadow.Debug($"Sending acknowledge for packet #{index}");

            byte[] buffer = new byte[9];
            MemoryStream stream = new MemoryStream(buffer);
            BinaryWriter writer = new BinaryWriter(stream);

            WriteAcknowledge(writer, index);
            Send(buffer, remoteEndpoint);
        }

        private static void SendTerminationHeader(IPEndPoint remoteEndpoint, ulong index)
        {
            RainMeadow.Debug($"Sending acknowledge for packet #{index}");

            byte[] buffer = new byte[9];
            MemoryStream stream = new MemoryStream(buffer);
            BinaryWriter writer = new BinaryWriter(stream);

            WriteTerminationHeader(writer, index);
            Send(buffer, remoteEndpoint);
        }

        private static void SendTermination()
        {
            RainMeadow.Debug($"Sending all known peers a final message!");

            foreach (var peer in peers)
            {
                var peerIP = peer.Key;
                var peerData = peer.Value;

                peerData.outgoingPackets.Clear();

                peerData.latestOutgoingPacket = new SequencedPacket(++peerData.packetIndex, new byte[0], 0, 10, true);
                peerData.outgoingPackets.Enqueue(peerData.latestOutgoingPacket);

                peerData.latestOutgoingPacket.OnAcknowledged += CleanUp;
                peerData.latestOutgoingPacket.OnFailed += CleanUp;

                byte[] packetData = peerData.outgoingPackets.Peek().packet;
                Send(packetData, peerIP);
            }

            waitingForTermination = true;
        }

        //

        /// <summary>Writes 9 bytes</summary>
        private static void WriteReliableHeader(BinaryWriter writer, ulong index)
        {
            writer.Write((byte)PacketType.Reliable);
            writer.Write(index);
        }

        private static ulong ReadReliableHeader(BinaryReader reader)
        {
            // Ignore type
            ulong index = reader.ReadUInt64();
            return index;
        }

        //

        /// <summary>Writes 1 byte</summary>
        private static void WriteUnreliableHeader(BinaryWriter writer)
        {
            writer.Write((byte)PacketType.Unreliable);
        }

        private static void ReadUnreliableHeader(BinaryReader reader)
        {
            // Ignore type
        }

        //

        /// <summary>Writes 9 bytes</summary>
        private static void WriteUnreliableOrderedHeader(BinaryWriter writer, ulong index)
        {
            writer.Write((byte)PacketType.UnreliableOrdered);
            writer.Write(index);
        }

        private static ulong ReadUnreliableOrderedHeader(BinaryReader reader)
        {
            // Ignore type
            ulong index = reader.ReadUInt64();
            return index;
        }

        //

        /// <summary>Writes 9 bytes</summary>
        private static void WriteAcknowledge(BinaryWriter writer, ulong index)
        {
            writer.Write((byte)PacketType.Acknowledge);
            writer.Write(index);
        }

        private static ulong ReadAcknowledge(BinaryReader reader)
        {
            // Ignore type
            ulong index = reader.ReadUInt64();
            return index;
        }

        //

        /// <summary>Writes 9 bytes</summary>
        private static void WriteTerminationHeader(BinaryWriter writer, ulong index)
        {
            writer.Write((byte)PacketType.Termination);
            writer.Write(index);
        }

        private static ulong ReadTerminationHeader(BinaryReader reader)
        {
            // Ignore type
            ulong index = reader.ReadUInt64();
            return index;
        }
    }
}