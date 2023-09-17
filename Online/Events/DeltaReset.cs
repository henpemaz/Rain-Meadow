namespace RainMeadow
{
    internal class DeltaReset : OnlineEvent
    {
        public OnlineResource onlineResource;
        public OnlineEntity.EntityId entity;

        public DeltaReset() { }
        public DeltaReset(OnlineResource onlineResource, OnlineEntity.EntityId entity)
        {
            this.onlineResource = onlineResource;
            this.entity = entity;
        }

        public override EventTypeId eventType => EventTypeId.DeltaReset;
        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeResourceByReference(ref onlineResource);
            serializer.SerializeNullable(ref entity);
        }

        public override void Process()
        {
            if (entity != null)
            {
                foreach (var feed in OnlineManager.feeds)
                {
                    if (feed.player == from && feed.entity.id == entity && feed.resource == onlineResource)
                    {
                        feed.ResetDeltas();
                        return;
                    }
                }
            }
            else
            {
                foreach (var subscription in OnlineManager.subscriptions)
                {
                    if (subscription.player == from && subscription.resource == onlineResource)
                    {
                        subscription.ResetDeltas();
                        return;
                    }
                }
            }
        }
    }
}