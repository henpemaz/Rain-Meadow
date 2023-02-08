namespace RainMeadow
{
    public abstract class ResultEvent : PlayerEvent
    {
        public ulong referencedEventId;

        protected ResultEvent(ulong referencedEventId)
        {
            this.referencedEventId = referencedEventId;
        }
    }
}