namespace RainMeadow
{
    public class EntityRequest : EntityEvent
    {
        public EntityRequest() { }
        public EntityRequest(OnlineEntity oe) : base(oe) { }

        public override EventTypeId eventType => EventTypeId.EntityRequest;

        public override void Process()
        {
            this.oe.Requested(this);
        }
    }
}