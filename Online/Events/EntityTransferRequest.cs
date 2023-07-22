namespace RainMeadow
{
    public class EntityTransferRequest : EntityResourceEvent, ResolvableEvent
    {
        public ushort newOwner;
        public EntityTransferRequest() { }

        public EntityTransferRequest(OnlineResource resource, OnlineEntity.EntityId entityId, OnlinePlayer newOwner) : base(resource, entityId, null)
        {
            this.newOwner = newOwner.inLobbyId;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref newOwner);
        }

        public override EventTypeId eventType => EventTypeId.EntityTransfererRequest;

        public override void Process()
        {
            onlineResource.OnEntityTransferRequest(this);
        }

        public void Resolve(GenericResult genericResult)
        {
            onlineResource.OnEntityTransferResolve(genericResult);
        }
    }
}