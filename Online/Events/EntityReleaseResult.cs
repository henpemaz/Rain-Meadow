namespace RainMeadow
{
    public abstract class EntityReleaseResult : ResultEvent
    {
        public EntityReleaseResult() { }

        public EntityReleaseResult(EntityReleaseEvent referencedEvent) : base(referencedEvent) { }

        public override void Process()
        {
            (referencedEvent as EntityEvent).oe.ResolveRelease(this);
        }

        public class Ok : EntityReleaseResult
        {
            public Ok() { }

            public Ok(EntityReleaseEvent referencedEvent) : base(referencedEvent) { }

            public override EventTypeId eventType => EventTypeId.EntityReleaseResultOk;
        }

        public class Error : EntityReleaseResult
        {
            public Error() { }

            public Error(EntityReleaseEvent referencedEvent) : base(referencedEvent) { }

            public override EventTypeId eventType => EventTypeId.EntityReleaseResultError;
        }
    }
}