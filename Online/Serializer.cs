using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace RainMeadow
{
    public partial class Serializer
    {
        private readonly byte[] buffer;
        private readonly long capacity;
        private long margin;
        private long Position => stream.Position;

        public bool isWriting { get; private set; }
        public bool isReading { get; private set; }
        public bool Aborted { get; private set; }

        private MemoryStream stream;
        private BinaryWriter writer;
        private BinaryReader reader;
        private OnlinePlayer currPlayer;
        private int eventCount;
        private StringBuilder eventLog;
        private long eventHeader;
        private int stateCount;
        private long stateHeader;
        private bool warnOnSizeMissmatch = false;

        public Serializer(long bufferCapacity) 
        {
            this.capacity = bufferCapacity;
            margin = (long)(bufferCapacity * 0.25f);
            buffer = new byte[bufferCapacity];
            stream = new(buffer);
            writer = new(stream);
            reader = new(stream);
        }

        private void PlayerHeaders()
        {
            if (isWriting)
            {
                writer.Write(currPlayer.lastEventFromRemote);
                writer.Write(currPlayer.tick);
                writer.Write(PlayersManager.mePlayer.tick);
            }
            if (isReading)
            {
                currPlayer.EventAckFromRemote(reader.ReadUInt64());
                currPlayer.TickAckFromRemote(reader.ReadUInt64());
                var newTick = reader.ReadUInt64();
                if (!OnlineManager.IsNewer(newTick, currPlayer.tick))
                {
                    // abort reading
                    AbortRead();
                }
                else
                {
                    currPlayer.tick = newTick;
                }
            }
        }

        private void AbortRead()
        {
            RainMeadow.Debug("aborted read");
            currPlayer = null;
            isReading = false;
            Aborted = true;
        }

        private void BeginWrite(OnlinePlayer toPlayer)
        {
            currPlayer = toPlayer;
            if (isWriting || isReading) throw new InvalidOperationException("not done with previous operation");
            isWriting = true;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
        }

        private bool CanFit(OnlineEvent playerEvent)
        {
            return Position + playerEvent.EstimatedSize + margin < capacity;
        }

        private bool CanFit(OnlineState state)
        {
            return Position + state.EstimatedSize + margin < capacity;
        }

        private void BeginWriteEvents()
        {
            eventCount = 0;
            eventLog = new(64);
            eventHeader = stream.Position;
            writer.Write(eventCount); // fake write, we'll overwrite this later
        }

        private void WriteEvent(OnlineEvent playerEvent)
        {
            eventCount++;
            long prevPos = (int)Position;
            writer.Write((byte)playerEvent.eventType);
            playerEvent.CustomSerialize(this);
            eventLog.AppendLine(playerEvent.ToString());
            long effectiveSize = Position - prevPos;
            string msg = $"size:{effectiveSize} est:{playerEvent.EstimatedSize}";
            if (warnOnSizeMissmatch && (effectiveSize != playerEvent.EstimatedSize)) RainMeadow.Error(msg);
            eventLog.AppendLine(msg);
        }

        private void EndWriteEvents()
        {
            var temp = stream.Position;
            stream.Position = eventHeader;
            writer.Write(eventCount);
            stream.Position = temp;
        }

        private void BeginWriteStates()
        {
            stateCount = 0;
            stateHeader = stream.Position;
            writer.Write(stateCount);
        }

        private void WriteState(OnlineState state)
        {
            stateCount++;
            writer.Write((byte)state.stateType);
            state.CustomSerialize(this);
        }

        private void EndWriteStates()
        {
            var temp = stream.Position;
            stream.Position = stateHeader;
            writer.Write(stateCount);
            stream.Position = temp;
        }

        private void EndWrite()
        {
            //RainMeadow.Debug($"serializer wrote: {eventCount} events; {stateCount} states; total {stream.Position} bytes");
            currPlayer = null;
            if (!isWriting) throw new InvalidOperationException("not writing");
            isWriting = false;
            writer.Flush();
        }

        private void BeginRead(OnlinePlayer fromPlayer)
        {
            currPlayer = fromPlayer;
            if (isWriting || isReading) throw new InvalidOperationException("not done with previous operation");
            isReading = true;
            Aborted = false;
            stream.Seek(0, SeekOrigin.Begin);
        }

        private int BeginReadEvents()
        {
            return reader.ReadInt32();
        }

        private OnlineEvent ReadEvent()
        {
            OnlineEvent e = OnlineEvent.NewFromType((OnlineEvent.EventTypeId)reader.ReadByte());
            e.from = currPlayer;
            e.to = PlayersManager.mePlayer;
            e.CustomSerialize(this);
            return e;
        }

        private int BeginReadStates()
        {
            return reader.ReadInt32();
        }

        private OnlineState ReadState()
        {
            OnlineState s = OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
            s.from = currPlayer;
            s.ts = currPlayer.tick;
            s.CustomSerialize(this);
            return s;
        }

        private void EndRead()
        {
            currPlayer = null;
            isReading = false;
        }

        private void Free()
        {
            // unused
        }

        // Process all incoming messages
        public void ReceiveData()
        {
            lock (this)
            {
                int n;
                IntPtr[] messages = new IntPtr[32];
                do // process in batches
                {
                    n = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, messages, messages.Length);
                    for (int i = 0; i < n; i++)
                    {
                        var message = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
                        try
                        {
                            if (OnlineManager.lobby != null)
                            {
                                var fromPlayer = PlayersManager.PlayerFromId(message.m_identityPeer.GetSteamID());
                                if (fromPlayer == null)
                                {
                                    RainMeadow.Error("player not found: " + message.m_identityPeer + " " + message.m_identityPeer.GetSteamID());
                                    continue;
                                }
                                //RainMeadow.Debug($"Receiving message from {fromPlayer}");
                                Marshal.Copy(message.m_pData, buffer, 0, message.m_cbSize);
                                BeginRead(fromPlayer);

                                PlayerHeaders();
                                if (Aborted)
                                {
                                    RainMeadow.Debug("skipped packet");
                                    continue;
                                }

                                int ne = BeginReadEvents();
                                //RainMeadow.Debug($"Receiving {ne} events");
                                for (int ie = 0; ie < ne; ie++)
                                {
                                    OnlineManager.ProcessIncomingEvent(ReadEvent());
                                }

                                int ns = BeginReadStates();
                                //RainMeadow.Debug($"Receiving {ns} states");
                                for (int ist = 0; ist < ns; ist++)
                                {
                                    OnlineManager.ProcessIncomingState(ReadState());
                                }

                                EndRead();
                            }
                        }
                        catch (Exception e)
                        {
                            RainMeadow.Error("Error reading packet from player : " + message.m_identityPeer.GetSteamID());
                            RainMeadow.Error(e);
                            EndRead();
                            //throw;
                        }
                        finally
                        {
                            SteamNetworkingMessage_t.Release(messages[i]);
                        }
                    }
                }
                while (n > 0);
                Free();
            }
        }

        public void SendData(OnlinePlayer toPlayer)
        {
            //RainMeadow.Debug($"Sending message to {toPlayer}");
            lock (this)
            {
                BeginWrite(toPlayer);

                PlayerHeaders();

                BeginWriteEvents();
                //RainMeadow.Debug($"Writing {toPlayer.OutgoingEvents.Count} events");
                foreach (var e in toPlayer.OutgoingEvents)
                {
                    if (CanFit(e))
                    {
                        WriteEvent(e);
                    }
                    else
                    {
                        RainMeadow.Error("no buffer space for events");
                        RainMeadow.Error(eventLog.ToString());
                        break;
                    }
                }
                EndWriteEvents();

                BeginWriteStates();
                //RainMeadow.Debug($"Writing {toPlayer.OutgoingStates.Count} states");
                while (toPlayer.OutgoingStates.Count > 0 && CanFit(toPlayer.OutgoingStates.Peek()))
                {
                    var s = toPlayer.OutgoingStates.Dequeue();
                    WriteState(s);
                }
                // todo handle states overflow, planing a packet for maximum size and least stale states
                EndWriteStates();

                EndWrite();

                unsafe
                {
                    fixed (byte* ptr = buffer)
                    {
                        SteamNetworkingMessages.SendMessageToUser(ref toPlayer.oid, (IntPtr)ptr, (uint)Position, Constants.k_nSteamNetworkingSend_UnreliableNoDelay, 0);
                    }
                }

                Free();
            }
        }

        // serializes player.id and finds reference
        public void Serialize(ref OnlinePlayer player)
        {
            if (isWriting)
            {
                writer.Write(player is { } ? (ulong)player.id : 0ul);
            }
            if (isReading)
            {
                player = PlayersManager.PlayerFromId(new CSteamID(reader.ReadUInt64()));
            }
        }

        // serializes resource.id and finds reference
        public void Serialize(ref OnlineResource onlineResource)
        {
            if (isWriting)
            {
                // todo switch to bytes?
                writer.Write(onlineResource.Id());
            }
            if (isReading)
            {
                string r = reader.ReadString();
                onlineResource = OnlineManager.ResourceFromIdentifier(r);
            }
        }

        // serializes a list of players by id
        public void Serialize(ref List<OnlinePlayer> players)
        {
            if (isWriting)
            {
                writer.Write((byte)players.Count);
                foreach (var player in players)
                {
                    writer.Write(player is { } ? (ulong)player.id : 0ul);
                }
            }
            if (isReading)
            {
                byte count = reader.ReadByte();
                players = new List<OnlinePlayer>(count);
                for (int i = 0; i < count; i++)
                {
                    players.Add(PlayersManager.PlayerFromId(new CSteamID(reader.ReadUInt64())));
                }
            }
        }

        public void Serialize(ref byte data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadByte();
        }
        
        public void Serialize(ref sbyte data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadSByte();
        }

        public void Serialize(ref ushort data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadUInt16();
        }

        public void Serialize(ref short data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadInt16();
        }

        public void Serialize(ref int data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadInt32();
        }

        public void Serialize(ref uint data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadUInt32();
        }

        public void Serialize(ref bool data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadBoolean();
        }

        public void Serialize(ref ulong data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadUInt64();
        }
        
        public void Serialize(ref float data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadSingle();
        }

        // this one isnt exactly safe, can cause huge allocations
        public void Serialize(ref string data)
        {
            if (isWriting) writer.Write(data);
            if (isReading) data = reader.ReadString();
        }

        public void Serialize(ref Vector2 data)
        {
            if (isWriting)
            {
                writer.Write(data.x);
                writer.Write(data.y);
            }
            if (isReading)
            {
                data.x = reader.ReadSingle();
                data.y = reader.ReadSingle();
            }
        }
      
        public void Serialize(ref Vector2? data)
        {
            if (isWriting)
            {
                writer.Write(data.HasValue);
                if (data.HasValue)
                {
                    writer.Write(data.Value.x);
                    writer.Write(data.Value.y);
                }
            }
            if (isReading)
            {
                if (reader.ReadBoolean())
                {
                    data = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }
            }
        }

        public void SerializeNoStrings(ref WorldCoordinate pos)
        {
            if (isWriting)
            {
                writer.Write((short)pos.room);
                writer.Write((short)pos.x);
                writer.Write((short)pos.y);
                writer.Write((short)pos.abstractNode);
            }
            if(isReading)
            {
                pos = new WorldCoordinate()
                {
                    room = reader.ReadInt16(),
                    x = reader.ReadInt16(),
                    y = reader.ReadInt16(),
                    abstractNode = reader.ReadInt16(),
                };
            }
        }

        public void SerializeEntity<T>(ref T onlineEntity) where T : OnlineEntity
        {
            if (isWriting)
            {
                writer.Write(onlineEntity.id.originalOwner);
                writer.Write(onlineEntity.id.id);
            }
            if (isReading)
            {
                var id = new OnlineEntity.EntityId(reader.ReadUInt64(), reader.ReadInt32());
                OnlineManager.recentEntities.TryGetValue(id, out var temp);
                onlineEntity = temp as T;
            }
        }

        public void SerializeEntityNullable<T>(ref T onlineEntity) where T : OnlineEntity
        {
            if (isWriting)
            {
                writer.Write(onlineEntity != null);
                if(onlineEntity != null)
                {
                    writer.Write(onlineEntity.id.originalOwner);
                    writer.Write(onlineEntity.id.id);
                }
            }
            if (isReading)
            {
                if (reader.ReadBoolean())
                {
                    var id = new OnlineEntity.EntityId(reader.ReadUInt64(), reader.ReadInt32());
                    OnlineManager.recentEntities.TryGetValue(id, out var temp);
                    onlineEntity = temp as T;
                }
            }
        }

        public void Serialize(ref OnlineState state)
        {
            if (isWriting)
            {
                writer.Write((byte)state.stateType);
                state.CustomSerialize(this);
            }
            if (isReading)
            {
                state = OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
                state.from = currPlayer;
                state.ts = currPlayer.tick;
                state.CustomSerialize(this);
            }
        }

        public void SerializeNullable(ref OnlineState nullableState)
        {
            if (isWriting)
            {
                writer.Write(nullableState != null);
                if (nullableState != null)
                {
                    Serialize(ref nullableState);
                }
            }
            if (isReading)
            {
                if (reader.ReadBoolean())
                {
                    Serialize(ref nullableState);
                }
            }
        }

        public void SerializeStates<T>(ref T[] states) where T : OnlineState
        {
            if (isWriting)
            {
                // TODO dynamic length
                if (states.Length > 255) throw new OverflowException("too many states");
                writer.Write((byte)states.Length);
                foreach (var state in states)
                {
                    writer.Write((byte)state.stateType);
                    state.CustomSerialize(this);
                }
            }
            if (isReading)
            {
                byte count = reader.ReadByte();
                states = new T[count];
                for (int i = 0; i < count; i++)
                {
                    var s = OnlineState.NewFromType((OnlineState.StateType)reader.ReadByte());
                    s.from = currPlayer;
                    s.ts = currPlayer.tick;
                    s.CustomSerialize(this);
                    states[i] = s as T; // can throw an invalid cast? or will it just be null?
                }
            }
        }

        // a referenced event is something that must have been ack'd that frame
        public void SerializeReferencedEvent(ref OnlineEvent referencedEvent)
        {
            if (isWriting)
            {
                writer.Write(referencedEvent.eventId);
            }
            if (isReading)
            {
                referencedEvent = currPlayer.GetRecentEvent(reader.ReadUInt64());
            }
        }

        internal void SerializeEvent<T>(ref T playerEvent) where T : OnlineEvent
        {
            if (isWriting)
            {
                writer.Write((byte)playerEvent.eventType);
                playerEvent.CustomSerialize(this);
            }
            if (isReading)
            {
                playerEvent = (T)OnlineEvent.NewFromType((OnlineEvent.EventTypeId)reader.ReadByte());
                playerEvent.from = currPlayer;
                playerEvent.to = PlayersManager.mePlayer;
                playerEvent.CustomSerialize(this);
            }
        }
    }
}