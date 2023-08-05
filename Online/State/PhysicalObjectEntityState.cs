using System;

namespace RainMeadow
{
    public class PhysicalObjectEntityState : EntityState
    {
        public WorldCoordinate pos;
        public bool realized;
        public RealizedPhysicalObjectState realizedObjectState;

        private bool hasPosValue; // delta
        private bool hasRealizedValue; // delta

        public override EntityState EmptyDelta() => new PhysicalObjectEntityState();
        public PhysicalObjectEntityState() : base() { }
        public PhysicalObjectEntityState(OnlinePhysicalObject onlineEntity, uint ts, bool realizedState) : base(onlineEntity, ts)
        {
            this.pos = onlineEntity.apo.pos;
            this.realized = onlineEntity.realized; // now now, oe.realized means its realized in the owners world
                                                   // not necessarily whether we're getting a real state or not
            if (realizedState) this.realizedObjectState = GetRealizedState(onlineEntity);
        }

        protected virtual RealizedPhysicalObjectState GetRealizedState(OnlinePhysicalObject onlineObject)
        {
            if (onlineObject.apo.realizedObject == null) throw new InvalidOperationException("not realized");
            if (onlineObject.apo.realizedObject is Spear) return new RealizedSpearState(onlineObject);
            if (onlineObject.apo.realizedObject is Weapon) return new RealizedWeaponState(onlineObject);
            return new RealizedPhysicalObjectState(onlineObject);
        }

        public override StateType stateType => StateType.PhysicalObjectEntityState;

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var onlineObject = onlineEntity as OnlinePhysicalObject;
            onlineObject.beingMoved = true;
            var wasPos = onlineObject.apo.pos;
            onlineObject.apo.Move(pos);
            if(!pos.NodeDefined && !wasPos.CompareDisregardingNode(pos))onlineObject.apo.pos = pos; // pos isn't updated if compareDisregardingTile, but please, do
            onlineObject.beingMoved = false;
            onlineObject.realized = this.realized;
            if (onlineObject.apo.realizedObject != null)
            {
                realizedObjectState?.ReadTo(onlineEntity);
            }
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            if (IsDelta) serializer.Serialize(ref hasPosValue);
            if (!IsDelta || hasPosValue)
            {
                serializer.Serialize(ref pos);
                serializer.Serialize(ref realized);
            }
            if (IsDelta) serializer.Serialize(ref hasRealizedValue);
            if (!IsDelta || hasRealizedValue)
            {
                serializer.SerializeNullablePolyState(ref realizedObjectState);
            }
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            var size = base.EstimatedSize(inDeltaContext);
            if (IsDelta) size += 2;
            if (!IsDelta || hasPosValue) size += 9;
            if (!IsDelta || hasRealizedValue) size += realizedObjectState.EstimatedSize(inDeltaContext);
            return size;
        }

        public override EntityState Delta(EntityState _other)
        {
            var delta = (PhysicalObjectEntityState)base.Delta(_other);
            var other = (PhysicalObjectEntityState)_other;
            delta.pos = pos;
            delta.realized = realized;
            delta.hasPosValue = pos != other.pos || realized != other.realized;
            delta.realizedObjectState = other.realizedObjectState != null ? realizedObjectState?.Delta(other.realizedObjectState) : realizedObjectState;
            delta.hasRealizedValue = realizedObjectState != other.realizedObjectState && !delta.realizedObjectState.IsEmptyDelta;
            delta.IsEmptyDelta &= (!delta.hasPosValue && !delta.hasRealizedValue);
            return delta;
        }
        public override EntityState ApplyDelta(EntityState _other)
        {
            var result = (PhysicalObjectEntityState)base.ApplyDelta(_other);
            var other = (PhysicalObjectEntityState)_other;
            result.pos = other.hasPosValue ? other.pos : pos;
            result.realized = other.hasPosValue ? other.realized : realized;
            result.realizedObjectState = other.hasRealizedValue ? realizedObjectState?.ApplyDelta(other.realizedObjectState) ?? other.realizedObjectState : realizedObjectState;
            return result;
        }
    }
}
