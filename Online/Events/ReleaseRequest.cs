using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class ReleaseRequest : ResourceEvent
    {
        public List<OnlinePlayer> participants;

        public ReleaseRequest() { }
        public ReleaseRequest(OnlineResource resource, List<OnlinePlayer> participants) : base(resource)
        {
            this.participants = participants;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref participants);
        }

        internal override void Process()
        {
            onlineResource.Released(this);
        }

        public override EventTypeId eventType => EventTypeId.ReleaseRequest;
    }
}