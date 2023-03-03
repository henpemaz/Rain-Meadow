namespace RainMeadow
{
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