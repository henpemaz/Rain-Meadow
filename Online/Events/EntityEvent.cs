namespace RainMeadow
{
    // event betwen players that have the full state of an entity
    // because needs a reference to oe on the receiving side as well
    internal abstract class EntityEvent : PlayerEvent
    {
        public OnlineEntity oe;

        public EntityEvent(){}

        public EntityEvent(OnlineEntity oe)
        {
            this.oe = oe;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref oe);
        }
    }
}