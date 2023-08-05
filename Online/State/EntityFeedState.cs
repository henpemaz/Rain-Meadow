namespace RainMeadow
{
    public class EntityFeedState : OnlineState
    {
        public EntityState entityState;
        public OnlineResource inResource;

        public override StateType stateType => StateType.EntityInResourceState;

        public EntityFeedState() { }
        public EntityFeedState(EntityState entityState, OnlineResource inResource) : base()
        {
            this.entityState = entityState;
            this.inResource = inResource;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            serializer.SerializePolyState(ref entityState);
            serializer.SerializeResourceByReference(ref inResource);
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            return entityState.EstimatedSize(inDeltaContext) + inResource.SizeOfIdentifier();
        }
    }
}