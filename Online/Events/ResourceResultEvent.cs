namespace RainMeadow
{
    public abstract class ResourceResultEvent : PlayerEvent
    {
        public ResourceEvent referencedEvent;

        protected ResourceResultEvent(ResourceEvent referencedEvent)
        {
            this.referencedEvent = referencedEvent;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeReferencedEvent(ref referencedEvent);
        }
    }
}