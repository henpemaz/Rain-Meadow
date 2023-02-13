using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public partial class OnlinePlayer : System.IEquatable<OnlinePlayer>
    {
        public CSteamID id;
        public SteamNetworkingIdentity oid;
        public Queue<PlayerEvent> OutgoingEvents;
        public Queue<ResourceState> OutgoingStates;
        public ulong nextOutgoingEvent = 1;
        public ulong lastAckdEvent;
        public ulong lastIncomingEvent;
        public bool needsAck;

        public OnlinePlayer(CSteamID id)
        {
            this.id = id;
            this.oid = new SteamNetworkingIdentity();
            oid.SetSteamID(id);
        }

        internal void QueueEvent(PlayerEvent e)
        {
            e.eventId = this.nextOutgoingEvent;
            e.to = this;
            e.from = OnlineManager.mePlayer;
            nextOutgoingEvent++;
            OutgoingEvents.Enqueue(e);
        }

        internal void SetAck(ulong lastAck)
        {
            this.lastAckdEvent = lastAck;
            while (OutgoingEvents.Count > 0 && OnlineManager.IsNewerOrEqual(lastAck, OutgoingEvents.Peek().eventId)) OutgoingEvents.Dequeue();
        }

        internal ResourceRequest RequestResource(OnlineResource onlineResource)
        {
            var req = new ResourceRequest(onlineResource);
            QueueEvent(req);
            return req;
        }

        internal TransferRequest TransferResource(OnlineResource onlineResource)
        {
            var req = new TransferRequest(onlineResource, onlineResource.subscribers);
            QueueEvent(req);
            return req;
        }

        internal ReleaseRequest ReleaseResource(OnlineResource onlineResource)
        {
            var req = new ReleaseRequest(onlineResource);
            QueueEvent(req);
            return req;
        }
    }
}
