namespace RainMeadow
{
    public abstract class ReleaseResult : ResultEvent
    {
        protected ReleaseResult() { }

        protected ReleaseResult(ResourceRelease referencedEvent) : base(referencedEvent) { }

        public override void Process()
        {
            (referencedEvent as ResourceEvent).onlineResource.ResolveRelease(this);
        }
        public class Unsubscribed : ReleaseResult
        {
            public Unsubscribed() { }

            public Unsubscribed(ResourceRelease referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultUnsubscribed;
        }

        public class Error : ReleaseResult
        {
            public Error() { }

            public Error(ResourceRelease referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultError;
        }

        public class Released : ReleaseResult
        {
            public Released() { }

            public Released(ResourceRelease referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultReleased;
        }
    }
}