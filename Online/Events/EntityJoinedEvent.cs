namespace RainMeadow
{
    internal class EntityJoinedEvent : EntityResourceEvent
    {

        public EntityJoinedEvent(OnlineEntity oe, OnlineResource onlineResource, PlayerTickReference tickReference) : base(onlineResource, oe.id, tickReference)
        {
        }

        public override EventTypeId eventType => throw new System.NotImplementedException();

        public override void Process()
        {
            onlineResource.OnEntityJoined(this);
        }
    }
}