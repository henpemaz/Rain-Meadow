using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class TransferRequest : RequestEvent
    {
        public List<OnlinePlayer> subscribers;

        public TransferRequest(OnlineResource resource, List<OnlinePlayer> subscribers) : base(resource)
        {
            this.subscribers = subscribers;
        }
        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            throw new NotImplementedException();
        }

        internal override void Process()
        {
            onlineResource.Transfered(this);
        }

        internal override void Resolve(ResultEvent resultEvent)
        {
            onlineResource.ResolveTransfer(resultEvent as TransferResult);
        }

        public override EventTypeId eventType => EventTypeId.TransferRequest;
    }
}