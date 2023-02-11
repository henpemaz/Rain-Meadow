namespace RainMeadow
{
    public abstract class ResultEvent : PlayerEvent
    {
        public RequestEvent referencedRequest;

        protected ResultEvent(RequestEvent referencedRequest)
        {
            this.referencedRequest = referencedRequest;
        }

        internal override void Process()
        {
            referencedRequest.Resolve(this);
        }
    }
}