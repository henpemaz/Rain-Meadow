using System;

namespace RainMeadow
{
    public class PhysicalObjectEntityState : EntityState
    {
        public WorldCoordinate pos;
        public bool realized;
        public OnlineState realizedObjectState;

        public OnlinePhysicalObject onlineObject => onlineEntity as OnlinePhysicalObject;

        public PhysicalObjectEntityState() : base() { }
        public PhysicalObjectEntityState(OnlinePhysicalObject onlineEntity, ulong ts, bool realizedState) : base(onlineEntity, ts, realizedState)
        {
            this.pos = onlineEntity.apo.pos;
            this.realized = onlineEntity.realized; // now now, oe.realized means its realized in the owners world
                                                   // not necessarily whether we're getting a real state or not
            if (realizedState) this.realizedObjectState = GetRealizedState();
        }

        protected virtual RealizedPhysicalObjectState GetRealizedState()
        {
            if (onlineObject.apo.realizedObject == null) throw new InvalidOperationException("not realized");
            if (onlineObject.apo.realizedObject is Spear) return new RealizedSpearState(onlineObject);
            if (onlineObject.apo.realizedObject is Weapon) return new RealizedWeaponState(onlineObject);
            return new RealizedPhysicalObjectState(onlineObject);
        }

        public override StateType stateType => StateType.PhysicalObjectEntityState;

        public override void ReadTo(OnlineEntity onlineEntity) // idk why this has a param if it also stores a ref to it
        {
            var onlineObject = onlineEntity as OnlinePhysicalObject;
            //onlineEntity.entity.pos = pos;
            onlineObject.beingMoved = true;
            onlineObject.apo.Move(pos);
            onlineObject.beingMoved = false;
            onlineObject.realized = this.realized;
            if(onlineObject.apo.realizedObject != null)
            {
                (realizedObjectState as RealizedPhysicalObjectState)?.ReadTo(onlineEntity);
            }
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeNoStrings(ref pos);
            serializer.Serialize(ref realized);
            serializer.SerializeNullable(ref realizedObjectState);
        }
    }
}
