namespace RainMeadow
{
    public partial class OnlineEntity
    {
        public class EntityId : System.IEquatable<EntityId>, Serializer.ICustomSerializable // How we refer to a game entity online
        {
            public ushort originalOwner;
            public int id;

            public EntityId() { }
            public EntityId(ushort originalOwner, int id)
            {
                this.originalOwner = originalOwner;
                this.id = id;
            }

            public OnlineEntity FindEntity()
            {
                if (OnlineManager.recentEntities.TryGetValue(this, out var entity)) return entity;
                RainMeadow.Error("Entity not found: " + this);
                return null;
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref originalOwner);
                serializer.Serialize(ref id);
            }

            public override string ToString()
            {
                return $"#{id}:{originalOwner:D4}";
            }
            public override bool Equals(object obj) => this.Equals(obj as EntityId);
            public bool Equals(EntityId other)
            {
                return other != null && id == other.id && originalOwner == other.originalOwner;
            }
            public override int GetHashCode() => id.GetHashCode() + originalOwner.GetHashCode();

            public static bool operator ==(EntityId lhs, EntityId rhs)
            {
                return lhs is null ? rhs is null : lhs.Equals(rhs);
            }
            public static bool operator !=(EntityId lhs, EntityId rhs) => !(lhs == rhs);
        }
    }
}
