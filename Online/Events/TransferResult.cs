using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract class TransferResult : ResultEvent
    {
        public TransferResult(TransferRequest referencedRequest) : base(referencedRequest) { }

        protected TransferResult() { }

        internal override void Process()
        {
            (referencedEvent as ResourceEvent).onlineResource.ResolveTransfer(this);
        }

        public class Error : TransferResult
        {
            public Error() { }

            public Error(TransferRequest referencedRequest) : base(referencedRequest) { }

            public override EventTypeId eventType => EventTypeId.TransferResultError;
        }

        public class Ok : TransferResult
        {
            public Ok() { }

            public Ok(TransferRequest referencedRequest) : base(referencedRequest) { }

            public override EventTypeId eventType => EventTypeId.TransferResultOk;
        }
    }
}