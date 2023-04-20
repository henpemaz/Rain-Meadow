namespace RainMeadow
{
    public abstract class ResultEvent : OnlineEvent
    {
        public OnlineEvent referencedEvent;

        protected ResultEvent() { }

        protected ResultEvent(OnlineEvent referencedEvent)
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