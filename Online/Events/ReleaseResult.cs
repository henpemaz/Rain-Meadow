namespace RainMeadow
{
    public abstract class ReleaseResult : ResultEvent
    {
        protected ReleaseResult() { }

        protected ReleaseResult(ReleaseRequest referencedEvent) : base(referencedEvent) { }

        internal override void Process()
        {
            (referencedEvent as ResourceEvent).onlineResource.ResolveRelease(this);
        }
        public class Unsubscribed : ReleaseResult
        {
            public Unsubscribed() { }

            public Unsubscribed(ReleaseRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultUnsubscribed;
        }

        public class Error : ReleaseResult
        {
            public Error() { }

            public Error(ReleaseRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultError;
        }

        internal class Released : ReleaseResult
        {
            public Released() { }

            public Released(ReleaseRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultReleased;
        }
    }
}