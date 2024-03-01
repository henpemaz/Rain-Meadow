namespace RainMeadow
{
    public interface ResolvableEvent
    {
        public void Resolve(GenericResult genericResult);
    }

    public abstract class GenericResult : ResultEvent
    {
        public GenericResult() { }

        public GenericResult(OnlineEvent referencedEvent) : base(referencedEvent) { }

        public override void Process()
        {
            (referencedEvent as ResolvableEvent).Resolve(this);
        }

        public class Ok : GenericResult
        {
            public Ok() { }
            public Ok(ResolvableEvent resolvableEvent) : base((OnlineEvent)resolvableEvent) { }

            public override EventTypeId eventType => EventTypeId.GenericResultOk;
        }
        public class Fail : GenericResult
        {
            public Fail() { }
            public Fail(ResolvableEvent resolvableEvent) : base((OnlineEvent)resolvableEvent) { }
            public override EventTypeId eventType => EventTypeId.GenericResultFail;
        }
        public class Error : GenericResult
        {
            public Error() { }
            public Error(ResolvableEvent resolvableEvent) : base((OnlineEvent)resolvableEvent) { }

            public override EventTypeId eventType => EventTypeId.GenericResultError;
        }
    }
}