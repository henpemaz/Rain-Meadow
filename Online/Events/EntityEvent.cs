namespace RainMeadow
{
    // event betwen players that have the full state of an entity
    // because needs a reference to oe on the receiving side as well
    public abstract class EntityEvent : OnlineEvent
    {
        public OnlineEntity.EntityId entityId;

        public EntityEvent() { }

        public EntityEvent(OnlineEntity oe)
        {
            this.entityId = oe.id;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref entityId);
        }
    }
}