using RainMeadow.Generics;

namespace RainMeadow
{
    public abstract class EntityState : RootDeltaState, IPrimaryDelta<EntityState>, IIdentifiable<OnlineEntity.EntityId>
    {
        // if sent "standalone" tracks deltafromtick
        // if sent inside another delta, doesn't

        public OnlineEntity.EntityId entityId;
        public OnlineEntity.EntityId ID => entityId;

        protected EntityState() : base() { }
        protected EntityState(OnlineEntity onlineEntity, uint ts) : base(ts)
        {
            this.entityId = onlineEntity.id;
        }

        public abstract void ReadTo(OnlineEntity onlineEntity);

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref entityId);
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            return base.EstimatedSize(inDeltaContext) + 6;
        }

        public abstract EntityState EmptyDelta();
        public bool IsEmptyDelta { get; set; }

        public virtual EntityState Delta(EntityState _other)
        {
            if(_other == null) throw new InvalidProgrammerException("null");
            if(_other.IsDelta) throw new InvalidProgrammerException("other is delta");
            var delta = EmptyDelta();
            delta.IsDelta = true;
            delta.DeltaFromTick = _other.tick;
            delta.entityId = entityId;
            delta.IsEmptyDelta = true;
            return delta;
        }

        public virtual EntityState ApplyDelta(EntityState _other)
        {
            if (_other == null) throw new InvalidProgrammerException("null");
            if (!_other.IsDelta) throw new InvalidProgrammerException("other not delta");
            var result = EmptyDelta();
            result.tick = _other.tick;
            result.from = _other.from;
            result.entityId = entityId;
            return result;
        }
    }
}
