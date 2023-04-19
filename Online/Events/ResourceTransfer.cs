namespace RainMeadow
{
    public class ResourceTransfer : ResourceEvent
    {
        public ResourceTransfer() { }
        public ResourceTransfer(OnlineResource resource) : base(resource) { }

        public override void Process()
        {
            onlineResource.Transfered(this);
        }

        public override EventTypeId eventType => EventTypeId.ResourceTransfer;
    }
}