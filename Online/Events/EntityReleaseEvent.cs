using System.Collections.Generic;

namespace RainMeadow
{
    internal class EntityReleaseEvent : EntityEvent
    {
        public OnlineResource inResource;

        public EntityReleaseEvent() { }

        public EntityReleaseEvent(OnlineEntity oe, OnlineResource inResource) : base(oe)
        {
            this.inResource = inResource;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref inResource);
        }

        public override EventTypeId eventType => EventTypeId.EntityReleaseEvent;

        internal override void Process()
        {
            oe.Released(this);
        }
    }
}