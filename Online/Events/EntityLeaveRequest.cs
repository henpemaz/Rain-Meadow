using Mono.Cecil;

namespace RainMeadow
{
    public class EntityLeaveRequest : EntityResourceEvent, ResolvableEvent
    {
        public EntityLeaveRequest() { }

        public EntityLeaveRequest(OnlineResource resource, OnlineEntity.EntityId entityId, TickReference tickReference) : base(resource, entityId, tickReference) { }

        public override EventTypeId eventType => EventTypeId.EntityLeaveRequest;

        public override void Process()
        {
            this.onlineResource.OnEntityLeaveRequest(this);
        }

        public void Resolve(GenericResult genericResult)
        {
            this.onlineResource.OnEntityLeaveResolve(genericResult);
        }
    }
}