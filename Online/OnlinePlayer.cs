using Steamworks;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public partial class OnlinePlayer : System.IEquatable<OnlinePlayer>
    {
        public CSteamID id;
        private List<PlayerEvent> OutgoingEvents;

        public OnlinePlayer(CSteamID id)
        {
            this.id = id;
        }

        internal ResourceRequest RequestResource(OnlineResource onlineResource)
        {
            var req = new ResourceRequest(OnlineManager.mePlayer, this, onlineResource);
            this.OutgoingEvents.Add(req);
            return req;
        }

        internal TransferRequest TransferResource(OnlineResource onlineResource)
        {
            var req = new TransferRequest(OnlineManager.mePlayer, this, onlineResource);
            this.OutgoingEvents.Add(req);
            return req;
        }

        internal void ReleaseResource(OnlineResource onlineResource)
        {
            var req = new ReleaseRequest(OnlineManager.mePlayer, this, onlineResource);
            this.OutgoingEvents.Add(req);
            return req;
        }
    }
}
