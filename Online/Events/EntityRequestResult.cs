namespace RainMeadow
{
    internal abstract class EntityRequestResult : ResultEvent
    {
        public EntityRequestResult() { }
        public EntityRequestResult(EntityRequest request) : base(request) { }

        internal override void Process()
        {
            (referencedEvent as EntityEvent).oe.ResolveRequest(this);
        }

        internal class Ok : EntityRequestResult
        {
            public Ok() { }
            public Ok(EntityRequest request) : base(request) { }

            public override EventTypeId eventType => EventTypeId.EntityRequestResultOk;
        }

        internal class Error : EntityRequestResult
        {
            public Error() { }
            public Error(EntityRequest request) : base(request) { }

            public override EventTypeId eventType => EventTypeId.EntityRequestResultError;
        }
    }
}