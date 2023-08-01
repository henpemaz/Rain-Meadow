using System;

namespace RainMeadow
{
    public class PhysicalObjectEntityState : EntityState
    {
        public WorldCoordinate pos;
        public bool realized;
        public RealizedPhysicalObjectState realizedObjectState;

        public PhysicalObjectEntityState() : base() { }
        public PhysicalObjectEntityState(OnlinePhysicalObject onlineEntity, uint ts, bool realizedState) : base(onlineEntity, ts, realizedState)
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
            serializer.SerializeNoStrings(ref pos); // todo make nullable if delta
            serializer.Serialize(ref realized);
            serializer.SerializeNullablePolyState(ref realizedObjectState);
        }

        public override bool SupportsDelta => true;
        public virtual PhysicalObjectEntityState NewInstance() => new();

    }
}
