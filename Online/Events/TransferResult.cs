using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract class TransferResult : ResultEvent
    {
        public TransferResult(ulong referencedEventId) : base(referencedEventId) { }

        public class Error : TransferResult
        {
            public Error(ulong referencedEventId) : base(referencedEventId) { }

            public override EventTypeId eventType => EventTypeId.TransferResultError;
        }

        public class Ok : TransferResult
        {
            public List<OnlinePlayer> subscribers;

            public Ok(ulong referencedEventId, List<OnlinePlayer> subscribers) : base(referencedEventId)
            {
                this.subscribers = subscribers;
            }

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                throw new NotImplementedException();
            }

            public override EventTypeId eventType => EventTypeId.TransferResultOk;
        }
    }
}