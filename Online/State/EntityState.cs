namespace RainMeadow
{
    public abstract class EntityState : OnlineState // Is this class completely redundant? everything inherits from PhysicalObjectEntityState
    {
        public OnlineEntity.EntityId entityId;
        public bool realizedState;

        protected EntityState() : base() { }
        protected EntityState(OnlineEntity onlineEntity, ulong ts, bool realizedState) : base(ts)
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
