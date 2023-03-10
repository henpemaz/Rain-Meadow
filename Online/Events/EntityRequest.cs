namespace RainMeadow
{
    internal class EntityRequest : EntityEvent
    {
        public EntityRequest() { }
        public EntityRequest(OnlineEntity oe) : base(oe) { }

        public override EventTypeId eventType => EventTypeId.EntityRequest;

        internal override void Process()
        {
            this.oe.Requested(this);
        }
    }
}