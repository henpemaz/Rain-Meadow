namespace RainMeadow
{
    public partial class OnlineEntity
    {
        public class EntityId : System.IEquatable<EntityId>, Serializer.ICustomSerializable // How we refer to a game entity online
        {
            public enum IdType : byte
            {
                none,
                settings,
                apo,
                uad,
                custom,
            }

            public ushort originalOwner;
            public byte type;
            public int id;
            public string userName;

            public EntityId() { }
            public EntityId(ushort originalOwner, IdType type, int id)
            {
                this.originalOwner = originalOwner;
                this.type = (byte)type;
                this.id = id;
                this.userName = OnlineManager.mePlayer.id.name;
            }

            public OnlineEntity FindEntity(bool quiet=false)
            {
                if (type == (byte)IdType.none) return null;
                if (OnlineManager.recentEntities.TryGetValue(this, out var entity)) return entity;
                if (!quiet) RainMeadow.Error("Entity not found: " + this);
                return null;
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref originalOwner);
                serializer.Serialize(ref type);
                serializer.Serialize(ref id);
                serializer.Serialize(ref userName);

            }

            public override string ToString()
            {
                return $"#{id}:{(IdType)type}:{originalOwner:D4}";
            }
            public override bool Equals(object obj) => this.Equals(obj as EntityId);
            public bool Equals(EntityId other)
            {
                return other != null && id == other.id && type == other.type && originalOwner == other.originalOwner;
            }
            public override int GetHashCode() => id.GetHashCode()*1024 + type.GetHashCode()*1024*1024 + originalOwner.GetHashCode();

            public static bool operator ==(EntityId lhs, EntityId rhs)
            {
                return lhs is null ? rhs is null : lhs.Equals(rhs);
            }
            public static bool operator !=(EntityId lhs, EntityId rhs) => !(lhs == rhs);
        }
    }
}
