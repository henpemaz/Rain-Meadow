namespace RainMeadow
{
    public abstract class RequestResult : ResourceResultEvent
    {
        protected RequestResult(ResourceRequest referencedEvent) : base(referencedEvent) { }

        internal override void Process()
        {
            referencedEvent.onlineResource.ResolveRequest(this);
        }
        public class Subscribed : RequestResult
        {
            public Subscribed(ResourceRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.RequestResultSubscribed;
        }

        public class Leased : RequestResult
        {
            public Leased(ResourceRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.RequestResultLeased;
        }

        public class Error : RequestResult
        {
            public Error(ResourceRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.RequestResultError;
        }
    }
}