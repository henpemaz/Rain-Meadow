namespace RainMeadow
{
    public class ResourceRequest : RequestEvent
    {
        public ResourceRequest(OnlineResource onlineResource) : base(onlineResource) { }

        public override EventTypeId eventType => EventTypeId.ResourceRequest;

        internal override void Process()
        {
            onlineResource.Requested(this);
        }

        internal override void Resolve(ResultEvent resultEvent)
        {
            onlineResource.ResolveRequest(resultEvent as RequestResult);
        }
    }
}