using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using Steamworks;
using UnityEngine;

namespace RainMeadow {
	public static class NetIO {
		public static void SendP2P(OnlinePlayer player, byte[] data, uint length, SendType sendType, PacketDataType dataType) {
			if (PlayersManager.mePlayer.isUsingSteam && player.isUsingSteam) // Make sure both sides are using steam
				// unsafe {
				// 	fixed (byte* dataPointer = &data[0]) {
				// 		SteamNetworkingMessages.SendMessageToUser(ref player.steamNetId, (IntPtr)dataPointer, length,
				// 		sendType switch
				// 		{
				// 			SendType.Reliable => Constants.k_nSteamNetworkingSend_Reliable,
				// 			SendType.Unreliable => Constants.k_nSteamNetworkingSend_Unreliable,
				// 			_ => Constants.k_nSteamNetworkingSend_Unreliable,
				// 		},
				// 		dataType switch
				// 		{
				// 			PacketDataType.GameInfo => 0,
				// 			PacketDataType.PlayerInfo => 1,
				// 			_ => 0,
				// 		});
				// 	}
				// }

				SteamNetworking.SendP2PPacket(player.steamId, data, length,
				sendType switch
				{
					SendType.Reliable => EP2PSend.k_EP2PSendReliable,
					SendType.Unreliable => EP2PSend.k_EP2PSendUnreliableNoDelay,
					_ => EP2PSend.k_EP2PSendUnreliableNoDelay,
				},
				dataType switch
				{
					PacketDataType.GameInfo => 0,
					PacketDataType.PlayerInfo => 1,
					_ => 0,
				});
			else
				UdpPeer.Send(player.endpoint, data, (int)length,
				sendType switch
				{
					SendType.Reliable => UdpPeer.PacketType.Reliable,
					SendType.Unreliable => UdpPeer.PacketType.Unreliable,
					_ => UdpPeer.PacketType.Unreliable,
				},
				dataType);
		}

		public static void SendP2P(OnlinePlayer player, Packet packet, SendType sendType, PacketDataType dataType) {
			MemoryStream memory = new MemoryStream(128);
			BinaryWriter writer = new BinaryWriter(memory);

			Packet.Encode(packet, writer, player.steamId, player.endpoint);

			byte[] bytes = memory.GetBuffer();

			SendP2P(player, bytes, (uint)bytes.Length, sendType, dataType);
		}

		public static void Update() {
			// int n;
			// IntPtr[] messagePtrs = new IntPtr[32];
			
			// do { 
			// 	n = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messagePtrs, messagePtrs.Length);
			// 	for (int i = 0; i < n; i++) {
			// 		var message = SteamNetworkingMessage_t.FromIntPtr(messagePtrs[i]);
			// 		Marshal.Copy(message.m_pData, OnlineManager.serializer.buffer, 0, message.m_cbSize);

            //    		OnlineManager.serializer.ReceiveDataSteam();
			// 		SteamNetworkingMessage_t.Release(messagePtrs[i]);
			// 	}
			// } while (n > 0);

			while (SteamNetworking.IsP2PPacketAvailable(out uint size, 0)) {
                if (!SteamNetworking.ReadP2PPacket(OnlineManager.serializer.buffer, size, out uint bytesRead, out CSteamID remoteSteamId))
                    continue;
                
               	OnlineManager.serializer.ReceiveDataSteam();
            }

			// do { 
			// 	n = SteamNetworkingMessages.ReceiveMessagesOnChannel(1, messagePtrs, messagePtrs.Length);
			// 	for (int i = 0; i < n; i++) {
			// 		var message = SteamNetworkingMessage_t.FromIntPtr(messagePtrs[i]);
			// 		byte[] buffer = new byte[message.m_cbSize];
			// 		Marshal.Copy(message.m_pData, buffer, 0, message.m_cbSize);

			// 		MemoryStream stream = new MemoryStream(buffer);
			// 		BinaryReader reader = new BinaryReader(stream);

            //    		PlayersManager.OnReceiveData(reader, null, message.m_identityPeer.GetSteamID());
			// 		SteamNetworkingMessage_t.Release(messagePtrs[i]);
			// 	}
			// } while (n > 0);

			while (SteamNetworking.IsP2PPacketAvailable(out uint size, 1)) {
                byte[] buffer = new byte[size];

                if (!SteamNetworking.ReadP2PPacket(buffer, size, out uint bytesRead, out CSteamID remoteSteamId, 1))
                    continue;

                MemoryStream stream = new MemoryStream(buffer);
                BinaryReader reader = new BinaryReader(stream);

				Packet.Decode(reader, fromSteamID: remoteSteamId);
            }

			UdpPeer.Update();

			while (UdpPeer.IsPacketAvailable()) {
				if (!UdpPeer.Read(out BinaryReader netReader, out IPEndPoint remoteEndpoint))
					continue;

				switch ((PacketDataType)netReader.ReadByte()) {
					case PacketDataType.PlayerInfo:		
						Packet.Decode(netReader, fromIpEndpoint: remoteEndpoint);
						break;

					case PacketDataType.GameInfo:
						byte[] data = netReader.ReadBytes((int)(netReader.BaseStream.Length - netReader.BaseStream.Position));
						OnlineManager.serializer.ReceiveDataDebug(remoteEndpoint, data);
						break;
				}
			}
		}
	}

	public enum SendType : byte {
		Reliable,
		Unreliable,
	}

	public enum PacketDataType : byte {
		Internal,
		GameInfo,
		PlayerInfo,
	}

	public interface ISerializable {
		public void Serialize(BinaryWriter writer);
		public void Deserialize(BinaryReader reader);
	}

	public static class NetIOExtensions {
		public static void EncodeVLQ(this BinaryWriter writer, uint value) {
			while (value > 0x7f) {
				writer.Write((byte)(value & 0x7f));
				value >>= 7;
			}
				
			writer.Write((byte)(value | 0x80));
		}

		public static uint DecodeVLQ(this BinaryReader reader) {
			byte offset = 0;
			uint value = 0;
			byte part;
			do {
				part = reader.ReadByte();
				value |= (uint)(part & 0x7f) << (offset++ * 7);
			} while ((part & 0x80) == 0);
				
			return value;
		}

		public static void Write(this BinaryWriter writer, OnlinePlayer players) {
			writer.Write(players.netId);
		}

		public static OnlinePlayer ReadPlayer(this BinaryReader reader, bool create = false) {
			return PlayersManager.PlayerFromId(reader.ReadInt32());
		}

		public static void Write<T>(this BinaryWriter writer, T item) where T : ISerializable {
			item.Serialize(writer);
		}

		public static void Read<T>(this BinaryReader reader, T item) where T : class, ISerializable {
			item.Deserialize(reader);
		}

		public static void WriteArray<T>(this BinaryWriter writer, T[] array) where T : ISerializable {
			writer.EncodeVLQ((uint)array.Length);
			for (int i = 0; i < array.Length; i++) {
				array[i].Serialize(writer);
			}
		}
	}
}
