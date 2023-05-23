namespace RainMeadow
{
    public class EntityJoinRequest : EntityResourceEvent, ResolvableEvent
    {
        public EntityJoinRequest() { }

        public EntityJoinRequest(OnlineResource resource, OnlineEntity.EntityId entityId, TickReference tickReference) : base(resource, entityId, tickReference) { }

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