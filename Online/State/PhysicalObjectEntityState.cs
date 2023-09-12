using Mono.Cecil;
using System;
using System.Text;

namespace RainMeadow
{
    public class PhysicalObjectEntityState : EntityState
    {
        [OnlineField]
        public WorldCoordinate pos;
        [OnlineField]
        public bool realized;
        [OnlineField(group = "realized", nullable = true, polymorphic = true)]
        public RealizedPhysicalObjectState realizedObjectState;

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
            if (onlineObject.apo.realizedObject is DangleFruit) { return new RealizedDangleFruitState(onlineObject); }
            return new RealizedPhysicalObjectState(onlineObject);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var onlineObject = onlineEntity as OnlinePhysicalObject;
            onlineObject.beingMoved = true;
            var wasPos = onlineObject.apo.pos;
            try
            {
                onlineObject.apo.Move(pos);
            }
            catch (Exception e)
            {
                RainMeadow.Error($"failed to move from {wasPos} to {pos}: " + e);
                //throw;
            }
            
            if(!pos.NodeDefined && !wasPos.CompareDisregardingNode(pos))onlineObject.apo.pos = pos; // pos isn't updated if compareDisregardingTile, but please, do
            onlineObject.beingMoved = false;
            onlineObject.realized = this.realized;
            if (onlineObject.apo.realizedObject != null)
            {
                realizedObjectState?.ReadTo(onlineEntity);
            }
        }
    }
}
