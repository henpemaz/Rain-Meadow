namespace RainMeadow
{
    public abstract class RequestResult : ResultEvent
    {
        protected RequestResult(ResourceRequest referencedRequest) : base(referencedRequest) { }

        public class Subscribed : RequestResult
        {
            public Subscribed(ResourceRequest referencedRequest) : base(referencedRequest) { }
            public override EventTypeId eventType => EventTypeId.RequestResultSubscribed;
        }

        public class Leased : RequestResult
        {
            public Leased(ResourceRequest referencedRequest) : base(referencedRequest) { }
            public override EventTypeId eventType => EventTypeId.RequestResultLeased;
        }

        public class Error : RequestResult
        {
            public Error(ResourceRequest referencedRequest) : base(referencedRequest) { }
            public override EventTypeId eventType => EventTypeId.RequestResultError;
        }
    }
}