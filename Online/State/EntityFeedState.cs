namespace RainMeadow
{
    public class EntityFeedState : OnlineState
    {
        public EntityState entityState;
        public OnlineResource inResource;

        public EntityFeedState() { }

        public EntityFeedState(EntityState entityState, OnlineResource inResource, uint ts) : base(ts)
        {
            this.entityState = entityState;
            this.inResource = inResource;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializePolyState(ref entityState);
            serializer.SerializeResourceByReference(ref inResource);
        }

        public override StateType stateType => StateType.EntityInResourceState;
    }
}