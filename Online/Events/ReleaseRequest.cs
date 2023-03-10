using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class ReleaseRequest : ResourceEvent
    {
        public List<OnlinePlayer> participants;
        public List<OnlineEntity.EntityId> abandonedEntities;

        public ReleaseRequest() { }
        public ReleaseRequest(OnlineResource resource, List<OnlinePlayer> participants, List<OnlineEntity.EntityId> entityIds) : base(resource)
        {
            this.participants = participants;
            this.abandonedEntities = entityIds;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref participants);
            serializer.Serialize(ref abandonedEntities);
        }

        internal override void Process()
        {
            onlineResource.Released(this);
        }

        public override EventTypeId eventType => EventTypeId.ReleaseRequest;
    }
}