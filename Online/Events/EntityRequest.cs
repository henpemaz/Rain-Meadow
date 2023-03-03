namespace RainMeadow
{
    internal class EntityRequest : EntityEvent
    {
        public EntityRequest(OnlineEntity oe) : base(oe)
        {
        }

        public override EventTypeId eventType => throw new System.NotImplementedException();

        internal override void Process()
        {
            this.oe.Requested(this);
        }
    }
}