namespace RainMeadow
{
    public abstract class ResultEvent : PlayerEvent
    {
        public PlayerEvent referencedEvent;

        protected ResultEvent() { }

        protected ResultEvent(PlayerEvent referencedEvent)
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