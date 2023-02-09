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

            public Ok(ulong referencedEventId) : base(referencedEventId) { }


            public override EventTypeId eventType => EventTypeId.TransferResultOk;
        }
    }
}