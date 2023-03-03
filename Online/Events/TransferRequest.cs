using System.Collections.Generic;

namespace RainMeadow
{
    public class TransferRequest : ResourceEvent
    {
        public List<OnlinePlayer> participants;

        public TransferRequest(OnlineResource resource, List<OnlinePlayer> subscribers) : base(resource)
        {
            this.participants = subscribers;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref participants);
        }

        internal override void Process()
        {
            onlineResource.Transfered(this);
        }

        public override EventTypeId eventType => EventTypeId.TransferRequest;
    }
}