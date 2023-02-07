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
        private SteamNetworkingIdentity oid;
        private Queue<PlayerEvent> OutgoingEvents;
        private List<ResourceState> outgoingStates;
        private ulong nextEvent;

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
            var req = new TransferRequest(OnlineManager.mePlayer, this, onlineResource);
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
                while (OutgoingEvents.Count > 1 && OnlineManager.serializer.CanFit(OutgoingEvents.Peek()))
                {
                    var e = OutgoingEvents.Dequeue();
                    OnlineManager.serializer.Serialize(ref e);
                }
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
