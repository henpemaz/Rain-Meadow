namespace RainMeadow
{
    public partial class OnlineEntity
    {
        public class EntityId : System.IEquatable<EntityId> // How we refer to a game entity online
        {
            public ulong originalOwner;
            public int id;

            public EntityId(ulong originalOwner, int id)
            {
                this.originalOwner = originalOwner;
                this.id = id;
            }

            public override string ToString()
            {
                return $"#{id}:{originalOwner:6}";
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
