namespace RainMeadow
{
    public abstract class ReleaseResult : ResultEvent
    {
        protected ReleaseResult(ReleaseRequest referencedRequest) : base(referencedRequest) { }

        public class Unsubscribed : ReleaseResult
        {
            public Unsubscribed(ReleaseRequest referencedRequest) : base(referencedRequest) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultUnsubscribed;
        }

        public class Error : ReleaseResult
        {
            public Error(ReleaseRequest referencedRequest) : base(referencedRequest) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultError;
        }

        internal class Released : ReleaseResult
        {
            public Released(ReleaseRequest referencedRequest) : base(referencedRequest) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultReleased;
        }
    }
}