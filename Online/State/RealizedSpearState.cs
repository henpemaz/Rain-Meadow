using UnityEngine;

namespace RainMeadow
{
    public class RealizedSpearState : RealizedWeaponState
    {
        private Vector2? stuckInWall;
        
        public RealizedSpearState() { }
        public RealizedSpearState(OnlineEntity onlineEntity) : base(onlineEntity)
        {
            var spear = (Spear)onlineEntity.entity.realizedObject;
            stuckInWall = spear.stuckInWall;
        }

        public override StateType stateType => StateType.RealizedSpearState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref stuckInWall);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var spear = (Spear)onlineEntity.entity.realizedObject;
            spear.stuckInWall = stuckInWall;
            if (!stuckInWall.HasValue) 
                spear.addPoles = false;
            
            base.ReadTo(onlineEntity);
            
            var newMode = new Weapon.Mode(Weapon.Mode.values.GetEntry(mode));
            if (newMode == Weapon.Mode.StuckInWall && !stuckInWall.HasValue)
            {
                RainMeadow.Error("Stuck in wall but has no value!");
            }
        }
    }
}