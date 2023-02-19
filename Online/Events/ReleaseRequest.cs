using System.Collections.Generic;

namespace RainMeadow
{
    public class ReleaseRequest : ResourceEvent
    {
        public List<OnlinePlayer> subscribers;

        public ReleaseRequest(OnlineResource resource, List<OnlinePlayer> subscribers) : base(resource)
        {
            this.subscribers = subscribers;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref subscribers);
        }

        internal override void Process()
        {
            onlineResource.Released(this);
        }

        public override EventTypeId eventType => EventTypeId.ReleaseRequest;
    }
}