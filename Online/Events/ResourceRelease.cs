using System.Collections.Generic;

namespace RainMeadow
{
    public class ResourceRelease : ResourceEvent
    {
        public List<OnlinePlayer> participants;
        public List<OnlineEntity.EntityId> abandonedEntities;

        public ResourceRelease() { }
        public ResourceRelease(OnlineResource resource, List<OnlinePlayer> participants, List<OnlineEntity.EntityId> abandonedEntities) : base(resource)
        {
            this.participants = participants;
            this.abandonedEntities = abandonedEntities;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref participants);
            serializer.Serialize(ref abandonedEntities);
        }

        public override void Process()
        {
            onlineResource.Released(this);
        }

        public override EventTypeId eventType => EventTypeId.ResourceRelease;
    }
}