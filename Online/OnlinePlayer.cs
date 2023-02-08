using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RainMeadow
{
    public partial class OnlinePlayer : System.IEquatable<OnlinePlayer>
    {
        public CSteamID id;
        public SteamNetworkingIdentity oid;
        public Queue<PlayerEvent> OutgoingEvents;
        public Queue<ResourceState> OutgoingStates;
        public ulong nextEvent;
        public ResourceState[] oldStates;

        public OnlinePlayer(CSteamID id)
        {
            this.id = id;
            this.oid = new SteamNetworkingIdentity();
            oid.SetSteamID(id);
        }

        internal void QueueEvent(PlayerEvent e)
        {
            e.eventId = this.nextEvent;
            nextEvent++;
            OutgoingEvents.Enqueue(e);
        }

        internal ResourceRequest RequestResource(OnlineResource onlineResource)
        {
            var req = new ResourceRequest(OnlineManager.mePlayer, this, onlineResource);
            QueueEvent(req);
            return req;
        }

        internal TransferRequest TransferResource(OnlineResource onlineResource)
        {
            var req = new TransferRequest(OnlineManager.mePlayer, this, onlineResource, onlineResource.subscribers);
            QueueEvent(req);
            return req;
        }

        internal ReleaseRequest ReleaseResource(OnlineResource onlineResource)
        {
            var req = new ReleaseRequest(OnlineManager.mePlayer, this, onlineResource);
            QueueEvent(req);
            return req;
        }

        internal void SendData()
        {
            lock (OnlineManager.serializer)
            {
                OnlineManager.serializer.BeginWrite();
                foreach (var e in OutgoingEvents)
                {
                    if (!OnlineManager.serializer.CanFit(e)) throw new IOException("no buffer space for events");
                    OnlineManager.serializer.WriteEvent(e);
                }

                while (OutgoingStates.Count > 1 && OnlineManager.serializer.CanFit(OutgoingStates.Peek()))
                {
                    var e = OutgoingStates.Dequeue();
                    OnlineManager.serializer.WriteState(e);
                }
                // todo handle states overflow, planing a packet for maximum size and least stale states

                OnlineManager.serializer.EndWrite();

                unsafe
                {
                    fixed (byte* ptr = OnlineManager.serializer.buffer)
                    {
                        SteamNetworkingMessages.SendMessageToUser(ref oid, (IntPtr)ptr, (uint)OnlineManager.serializer.Position, 0, 0);
                    }
                }

                OnlineManager.serializer.Free();
            }
        }
    }
}
