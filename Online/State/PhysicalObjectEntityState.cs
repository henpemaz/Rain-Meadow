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

        public override long EstimatedSize(Serializer serializer)
        {
            var size = base.EstimatedSize(serializer);
            if (IsDelta) size += 2;
            if (!IsDelta || hasPosValue) size += 9;
            if (!IsDelta || hasRealizedValue) size += realizedObjectState.EstimatedSize(serializer);
            return size;
        }

        public override EntityState Delta(EntityState other)
        {
            var otherpoes = (PhysicalObjectEntityState)other;
            var delta = (PhysicalObjectEntityState)base.Delta(other);
            delta.pos = pos;
            delta.realized = realized;
            delta.hasPosValue = pos != otherpoes.pos || realized != otherpoes.realized;
            delta.realizedObjectState = realizedObjectState?.Delta(otherpoes.realizedObjectState);
            delta.hasRealizedValue = realizedObjectState != null;
            return delta;
        }
        public override EntityState ApplyDelta(EntityState other)
        {
            var result = (PhysicalObjectEntityState)base.ApplyDelta(other);
            var otherp = (PhysicalObjectEntityState)other;
            result.pos = otherp.hasPosValue ? otherp.pos : pos;
            result.realized = otherp.hasPosValue ? otherp.realized : realized;
            result.realizedObjectState = otherp.hasRealizedValue ? realizedObjectState?.ApplyDelta(otherp.realizedObjectState) ?? otherp.realizedObjectState : null;
            return result;
        }
    }
}
