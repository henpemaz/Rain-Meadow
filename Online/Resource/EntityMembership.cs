using RainMeadow.Generics;
using System;

namespace RainMeadow
{
    public class EntityMembership : Serializer.ICustomSerializable, IEquatable<EntityMembership>
    {
        public OnlineEntity.EntityId entityId;
        public uint version;

        public EntityMembership() { }
        public EntityMembership(OnlineEntity entity)
        {
            this.entityId = entity.id;
            this.version = entity.version;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref entityId);
            serializer.Serialize(ref version);
        }

        public bool Equals(EntityMembership other)
        {
            return other != null && entityId == other.entityId && version == other.version;
        }

        public override bool Equals(object obj) => this.Equals(obj as EntityMembership);

        public override int GetHashCode() => entityId.GetHashCode() * 1024 + (int)version;

        public static bool operator ==(EntityMembership lhs, EntityMembership rhs)
        {
            return lhs is null ? rhs is null : lhs.Equals(rhs);
        }
        public static bool operator !=(EntityMembership lhs, EntityMembership rhs) => !(lhs == rhs);
    }
}