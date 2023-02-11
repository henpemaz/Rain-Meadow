using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract class TransferResult : ResultEvent
    {
        public TransferResult(TransferRequest referencedRequest) : base(referencedRequest) { }

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