namespace RainMeadow
{
    internal class EntityRequest : EntityEvent
    {
        internal int newId;

        public EntityRequest() { }
        public EntityRequest(OnlineEntity oe, int newId) : base(oe)
        {
            this.newId = newId;
        }


        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref newId);
        }

        public override EventTypeId eventType => EventTypeId.EntityRequest;

        internal override void Process()
        {
            this.oe.Requested(this);
        }
    }
}