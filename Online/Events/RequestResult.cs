namespace RainMeadow
{
    public abstract class RequestResult : ResultEvent
    {
        protected RequestResult() { }

        protected RequestResult(ResourceRequest referencedEvent) : base(referencedEvent) { }

        public override void Process()
        {
            (referencedEvent as ResourceEvent).onlineResource.ResolveRequest(this);
        }
        public class Subscribed : RequestResult
        {
            public Subscribed() { }

            public Subscribed(ResourceRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.RequestResultSubscribed;
        }

        public class Leased : RequestResult
        {
            public Leased() { }

            public Leased(ResourceRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.RequestResultLeased;
        }

        public class Error : RequestResult
        {
            public Error() { }

            public Error(ResourceRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.RequestResultError;
        }
    }
}