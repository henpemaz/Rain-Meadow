using System;

namespace RainMeadow
{
    public class BodyChunkRef : Serializer.ICustomSerializable, IEquatable<BodyChunkRef>
    {
        public OnlineEntity.EntityId onlineOwner;
        public byte index;

        public PhysicalObject? owner => (onlineOwner?.FindEntity() as OnlinePhysicalObject)?.apo.realizedObject;

        public BodyChunkRef() { }
        public BodyChunkRef(OnlinePhysicalObject owner, int index)
        {
            this.onlineOwner = owner.id;
            this.index = (byte)index;
        }

        public static BodyChunkRef FromBodyChunk(BodyChunk bodyChunk)
        {
            if (!OnlinePhysicalObject.map.TryGetValue(bodyChunk.owner.abstractPhysicalObject, out var oe))
                throw new InvalidProgrammerException("body chunk owner doesn't exist in online space! " + bodyChunk.owner.abstractPhysicalObject);
            return new BodyChunkRef(oe, bodyChunk.index);
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref onlineOwner);
            serializer.Serialize(ref index);
        }

        public bool Equals(BodyChunkRef other)
        {
            return other is not null && other.onlineOwner == onlineOwner && other.index == index;
        }

        public override bool Equals(object obj) => obj is BodyChunkRef other && Equals(other);

        public static bool operator ==(BodyChunkRef lhs, BodyChunkRef rhs) => lhs is not null && lhs.Equals(rhs);

        public static bool operator !=(BodyChunkRef lhs, BodyChunkRef rhs) => !(lhs == rhs);

        public override int GetHashCode() => onlineOwner.GetHashCode() * 256 + index;
    }
}
