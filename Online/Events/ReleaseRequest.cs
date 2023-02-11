namespace RainMeadow
{
    public class ReleaseRequest : RequestEvent
    {
        public ReleaseRequest(OnlineResource onlineResource) : base(onlineResource) { }

        public override EventTypeId eventType => EventTypeId.ReleaseRequest;

        internal override void Process()
        {
            onlineResource.Released(this);
        }

        internal override void Resolve(ResultEvent resultEvent)
        {
            onlineResource.ResolveRelease(resultEvent as ReleaseResult);
        }
    }
}