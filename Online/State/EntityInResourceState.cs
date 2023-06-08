namespace RainMeadow
{
    public class EntityInResourceState : OnlineState
    {
        public EntityState entityState;
        public OnlineResource inResource;

        public EntityInResourceState() { }

        public EntityInResourceState(EntityState entityState, OnlineResource inResource, ulong ts) : base(ts)
        {
            this.entityState = entityState;
            this.inResource = inResource;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializePolyState(ref entityState);
            serializer.Serialize(ref inResource);
        }

        public override StateType stateType => StateType.EntityInResourceState;
    }
}