namespace RainMeadow
{
    internal abstract class EntityReleaseResult : ResultEvent
    {
        public EntityReleaseResult() { }

        public EntityReleaseResult(EntityReleaseEvent referencedEvent) : base(referencedEvent) { }

        internal override void Process()
        {
            (referencedEvent as EntityEvent).oe.ResolveRelease(this);
        }

        internal class Ok : EntityReleaseResult
        {
            public Ok() { }

            public Ok(EntityReleaseEvent referencedEvent) : base(referencedEvent) { }

            public override EventTypeId eventType => EventTypeId.EntityReleaseResultOk;
        }

        internal class Error : EntityReleaseResult
        {
            public Error() { }

            public Error(EntityReleaseEvent referencedEvent) : base(referencedEvent) { }

            public override EventTypeId eventType => EventTypeId.EntityReleaseResultError;
        }
    }
}