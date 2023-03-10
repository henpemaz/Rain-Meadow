using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class TransferRequest : ResourceEvent
    {
        public List<OnlinePlayer> participants;
        public List<OnlineEntity.EntityId> abandonedEntities;

        public TransferRequest() { }
        public TransferRequest(OnlineResource resource, List<OnlinePlayer> subscribers, List<OnlineEntity.EntityId> abandonedEntities) : base(resource)
        {
            this.participants = subscribers;
            this.abandonedEntities = abandonedEntities;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref participants);
            serializer.Serialize(ref abandonedEntities);
        }

        internal override void Process()
        {
            onlineResource.Transfered(this);
        }

        public override EventTypeId eventType => EventTypeId.TransferRequest;
    }
}