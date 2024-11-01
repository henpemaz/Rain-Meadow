using UnityEngine;

namespace RainMeadow
{

    public class RealizedWeaponState : RealizedPhysicalObjectState
    {
        // is all this data really necessary?
        [OnlineField]
        protected byte mode;
        [OnlineFieldHalf]
        private Vector2 rotation;
        [OnlineFieldHalf]
        private float rotationSpeed;
        [OnlineField(nullable = true)]
        private OnlineEntity.EntityId? thrownBy;

        public RealizedWeaponState() { }
        public RealizedWeaponState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var weapon = (Weapon)onlineEntity.apo.realizedObject;
            mode = (byte)weapon.mode;
            rotation = weapon.rotation;
            rotationSpeed = weapon.rotationSpeed;
            thrownBy = (weapon.thrownBy?.abstractCreature != null && OnlineCreature.map.TryGetValue(weapon.thrownBy.abstractCreature, out var oc)) ? oc?.id : null;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var weapon = (Weapon)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            var newMode = new Weapon.Mode(Weapon.Mode.values.GetEntry(mode));
            if (weapon.room != null && weapon.mode != newMode)
            {
                RainMeadow.Debug($"{onlineEntity} new mode : {newMode}");
                weapon.ChangeMode(newMode);
                weapon.throwModeFrames = -1; // not synched, behaves as "infinite"
            }
            weapon.thrownBy = (thrownBy?.FindEntity() as OnlineCreature)?.realizedCreature;
            if (weapon.grabbedBy != null && weapon.grabbedBy.Count > 0) { RainMeadow.Trace($"Skipping state because grabbed"); return; }
            weapon.rotation = rotation;
            weapon.rotationSpeed = rotationSpeed;
        }
    }
}
