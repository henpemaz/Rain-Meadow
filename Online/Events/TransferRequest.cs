using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class TransferRequest : ResourceEvent
    {
        public List<OnlinePlayer> subscribers;

        public TransferRequest(OnlinePlayer from, OnlinePlayer to, OnlineResource resource, List<OnlinePlayer> subscribers) : base(from, to, resource)
        {
            this.subscribers = subscribers;
        }
        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            throw new NotImplementedException();
        }

        public override EventTypeId eventType => EventTypeId.TransferRequest;
    }
}