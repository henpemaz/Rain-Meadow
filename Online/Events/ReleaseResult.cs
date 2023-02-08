namespace RainMeadow
{
    public abstract class ReleaseResult : ResultEvent
    {
        protected ReleaseResult(ulong referencedEventId) : base(referencedEventId) { }

        public class Unsubscribed : ReleaseResult
        {
            public Unsubscribed(ulong referencedEventId) : base(referencedEventId) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultUnsubscribed;
        }

        public class Error : ReleaseResult
        {
            public Error(ulong referencedEventId) : base(referencedEventId) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultError;
        }

        internal class Released : ReleaseResult
        {
            public Released(ulong referencedEventId) : base(referencedEventId) { }
            public override EventTypeId eventType => EventTypeId.ReleaseResultReleased;
        }
    }
}