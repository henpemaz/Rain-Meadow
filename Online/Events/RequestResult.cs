namespace RainMeadow
{
    public abstract class RequestResult : ResultEvent
    {
        protected RequestResult(ulong referencedEventId) : base(referencedEventId) { }

        public class Subscribed : RequestResult
        {
            public Subscribed(ulong referencedEventId) : base(referencedEventId) { }
            public override EventTypeId eventType => EventTypeId.RequestResultSubscribed;
        }

        public class Leased : RequestResult
        {
            public Leased(ulong referencedEventId) : base(referencedEventId) { }
            public override EventTypeId eventType => EventTypeId.RequestResultLeased;
        }

        public class Error : RequestResult
        {
            public Error(ulong referencedEventId) : base(referencedEventId) { }
            public override EventTypeId eventType => EventTypeId.RequestResultError;
        }
    }
}