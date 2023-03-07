using Steamworks;
using System;
using System.Collections;
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
        public Queue<PlayerEvent> OutgoingEvents = new(16);
        public List<PlayerEvent> recentlyAckedEvents = new(16);
        public Queue<OnlineState> OutgoingStates = new(128);
        private ulong nextOutgoingEvent = 1;
        public ulong lastEventFromRemote; // the last event I've received from them, I'll write it back on headers as an ack
        private ulong lastAckFromRemote; // the last event they've ack'd to me
        public ulong tick; // the last tick I've received from them, I'll write it back on headers as an ack
        public ulong lastAckdTick; // the last tick they've ack'd to me
        public bool needsAck;
        public bool isMe;
        public string name;
        public Dictionary<int, OnlineEntity> recentEntities = new(); // this is very handy, but is it correct? when should this be cleaned up?

        //public List<OnlineResource> resourcesOwned = new();

        public OnlinePlayer(CSteamID id)
        {
            this.id = id;
            this.oid = new SteamNetworkingIdentity();
            oid.SetSteamID(id);
            isMe = id == OnlineManager.me;
            name = SteamFriends.GetFriendPersonaName(id);
        }

        internal void QueueEvent(PlayerEvent e)
        {
            RainMeadow.Debug($"Queued event {nextOutgoingEvent} {e.eventType} to player {this}");
            e.eventId = this.nextOutgoingEvent;
            e.to = this;
            e.from = OnlineManager.mePlayer;
            nextOutgoingEvent++;
            OutgoingEvents.Enqueue(e);
        }

        internal PlayerEvent GetRecentEvent(ulong id)
        {
            return recentlyAckedEvents.FirstOrDefault(e => e.eventId == id);
        }

        internal void AckFromRemote(ulong lastAck)
        {
            //RainMeadow.Debug(this);
            this.recentlyAckedEvents.Clear();
            this.lastAckFromRemote = lastAck;
            while (OutgoingEvents.Count > 0 && OnlineManager.IsNewerOrEqual(lastAck, OutgoingEvents.Peek().eventId))
            {
                var e = OutgoingEvents.Dequeue();
                RainMeadow.Debug(this.ToString() + e.ToString());
                recentlyAckedEvents.Add(e);
            }
        }

        internal void TickAckFromRemote(ulong lastTick)
        {
            this.lastAckdTick = lastTick;
        }

        internal void RequestResource(OnlineResource onlineResource)
        {
            RainMeadow.Debug($"Requesting player {this.name} for resource {onlineResource.Identifier()}");
            var req = new ResourceRequest(onlineResource);
            onlineResource.pendingRequest = req;
            QueueEvent(req);
        }

        internal void TransferResource(OnlineResource onlineResource, List<OnlinePlayer> subscribers)
        {
            RainMeadow.Debug($"Requesting player {this.name} for transfer of {onlineResource.Identifier()}");
            var req = new TransferRequest(onlineResource, subscribers);
            onlineResource.pendingRequest = req;
            QueueEvent(req);
        }

        internal void ReleaseResource(OnlineResource onlineResource)
        {
            RainMeadow.Debug($"Requesting player {this.name} for release of resource {onlineResource.Identifier()}");
            var req = new ReleaseRequest(onlineResource, 
                onlineResource.isOwner ? onlineResource.participants : null);
            onlineResource.pendingRequest = req;
            QueueEvent(req);
        }

        internal void RequestEntity(OnlineEntity oe, int newId)
        {
            RainMeadow.Debug($"Requesting player {this.name} for entity {oe}");
            var req = new EntityRequest(oe, newId);
            oe.pendingRequest = req;
            QueueEvent(req);
        }

        internal void ReleaseEntity(OnlineEntity oe)
        {
            RainMeadow.Debug($"Releasing entity {oe} back to player {this.name}");
            var req = new EntityReleaseEvent(oe);
            oe.pendingRequest = req;
            QueueEvent(req);
        }

        public override string ToString()
        {
            return $"{id} - {name}";
        }
        public override bool Equals(object obj) => this.Equals(obj as OnlinePlayer);
        public bool Equals(OnlinePlayer other)
        {
            return other != null && id == other.id;
        }
        public override int GetHashCode() => id.GetHashCode();

        public static bool operator ==(OnlinePlayer lhs, OnlinePlayer rhs)
        {
            return lhs is null ? rhs is null : lhs.Equals(rhs);
        }
        public static bool operator !=(OnlinePlayer lhs, OnlinePlayer rhs) => !(lhs == rhs);
    }
}
