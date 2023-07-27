namespace RainMeadow
{
    public abstract class EntityState : OnlineState, Generics.IIdentifiable<OnlineEntity.EntityId>
    {
        public OnlineEntity.EntityId entityId;
        public bool realizedState;
        public OnlineEntity.EntityId ID => entityId;

        protected EntityState() : base() { }
        protected EntityState(OnlineEntity onlineEntity, uint ts, bool realizedState) : base(ts)
        {
            this.entityId = onlineEntity.id;
            this.realizedState = realizedState;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref entityId);
            serializer.Serialize(ref realizedState);
        }

        public abstract void ReadTo(OnlineEntity onlineEntity);
    }
}
