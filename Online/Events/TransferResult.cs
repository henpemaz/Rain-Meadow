using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract class TransferResult : ResourceResultEvent
    {
        public TransferResult(TransferRequest referencedRequest) : base(referencedRequest) { }

        internal override void Process()
        {
            referencedEvent.onlineResource.ResolveTransfer(this);
        }

        public class Error : TransferResult
        {
            public Error(TransferRequest referencedRequest) : base(referencedRequest) { }

            public override EventTypeId eventType => EventTypeId.TransferResultError;
        }

        public class Ok : TransferResult
        {
            public Ok(TransferRequest referencedRequest) : base(referencedRequest) { }

            public override EventTypeId eventType => EventTypeId.TransferResultOk;
        }
    }
}