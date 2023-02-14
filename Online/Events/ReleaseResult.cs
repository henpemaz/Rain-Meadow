namespace RainMeadow
{
    public abstract class ReleaseResult : ResourceResultEvent
    {
        protected ReleaseResult(ReleaseRequest referencedEvent) : base(referencedEvent) { }

        internal override void Process()
        {
            referencedEvent.onlineResource.ResolveRelease(this);
        }
        public class Unsubscribed : ReleaseResult
        {
            public Unsubscribed(ReleaseRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultUnsubscribed;
        }

        public class Error : ReleaseResult
        {
            public Error(ReleaseRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultError;
        }

        internal class Released : ReleaseResult
        {
            public Released(ReleaseRequest referencedEvent) : base(referencedEvent) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultReleased;
        }
    }
}