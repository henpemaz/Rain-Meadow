namespace RainMeadow
{
    public abstract class EntityRequestResult : ResultEvent
    {
        public EntityRequestResult() { }
        public EntityRequestResult(EntityRequest request) : base(request) { }

        public override void Process()
        {
            (referencedEvent as EntityEvent).oe.ResolveRequest(this);
        }

        public class Ok : EntityRequestResult
        {
            public Ok() { }
            public Ok(EntityRequest request) : base(request) { }

            public override EventTypeId eventType => EventTypeId.EntityRequestResultOk;
        }

        public class Error : EntityRequestResult
        {
            public Error() { }
            public Error(EntityRequest request) : base(request) { }

            public override EventTypeId eventType => EventTypeId.EntityRequestResultError;
        }
    }
}