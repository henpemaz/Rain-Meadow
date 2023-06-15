namespace RainMeadow
{
    public class EntityRequest : EntityEvent, ResolvableEvent
    {
        public EntityRequest() { }
        public EntityRequest(OnlineEntity oe) : base(oe) { }

        public override EventTypeId eventType => EventTypeId.EntityRequest;

        public override void Process()
        {
            this.oe.Requested(this);
        }

        public void Resolve(GenericResult genericResult)
        {
            this.oe.ResolveRequest(genericResult);
        }
    }
}