using System;

namespace RainMeadow
{
    public class PhysicalObjectEntityState : EntityState
    {
        public WorldCoordinate pos;
        public bool realized;
        public OnlineState realizedObjectState;

        public PhysicalObjectEntityState() : base() { }
        public PhysicalObjectEntityState(OnlineEntity onlineEntity, ulong ts, bool realizedState) : base(onlineEntity, ts, realizedState)
        {
            this.pos = onlineEntity.entity.pos;
            this.realized = onlineEntity.realized; // now now, oe.realized means its realized in the owners world
                                                   // not necessarily whether we're getting a real state or not
            if (realizedState) this.realizedObjectState = GetRealizedState();
        }

        protected virtual PhysicalObjectState GetRealizedState()
        {
            if (onlineEntity.entity.realizedObject == null) throw new InvalidOperationException("not realized");
            if (onlineEntity.entity.realizedObject is Spear) return new RealizedSpearState(onlineEntity);
            if (onlineEntity.entity.realizedObject is Weapon) return new RealizedWeaponState(onlineEntity);
            return new PhysicalObjectState(onlineEntity);
        }

        public override StateType stateType => StateType.PhysicalObjectEntityState;

        public override void ReadTo(OnlineEntity onlineEntity) // idk why this has a param if it also stores a ref to it
        {
            //onlineEntity.entity.pos = pos;
            onlineEntity.entity.Move(pos);
            onlineEntity.realized = this.realized;
            (realizedObjectState as PhysicalObjectState)?.ReadTo(onlineEntity);
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
