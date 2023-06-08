namespace RainMeadow
{
    public class EntityJoinRequest : EntityJoinedEvent, ResolvableEvent
    {
        public EntityJoinRequest() { }

        public EntityJoinRequest(OnlineResource resource, OnlineEntity entity, TickReference tickReference) : base(resource, entity, tickReference) { }

        public override EventTypeId eventType => EventTypeId.EntityJoinRequest;

        public override void Process()
        {
            this.onlineResource.OnEntityJoinRequest(this);
        }

        public void Resolve(GenericResult genericResult)
        {
            this.onlineResource.OnJoinResolve(genericResult);
        }
    }
}