namespace RainMeadow
{
    public abstract class TransferResult : ResultEvent
    {
        public TransferResult(ResourceTransfer referencedRequest) : base(referencedRequest) { }

        protected TransferResult() { }

        public override void Process()
        {
            (referencedEvent as ResourceEvent).onlineResource.ResolveTransfer(this);
        }

        public class Error : TransferResult
        {
            public Error() { }

            public Error(ResourceTransfer referencedRequest) : base(referencedRequest) { }

            public override EventTypeId eventType => EventTypeId.TransferResultError;
        }

        public class Ok : TransferResult
        {
            public Ok() { }

            public Ok(ResourceTransfer referencedRequest) : base(referencedRequest) { }

            public override EventTypeId eventType => EventTypeId.TransferResultOk;
        }
    }
}