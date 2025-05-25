using RWCustom;
using UnityEngine;

namespace RainMeadow
{

    public class RealizedWeaponState : RealizedPhysicalObjectState
    {
        [OnlineField]
        protected Weapon.Mode mode;
        [OnlineFieldHalf]
        private float rotation;
        [OnlineFieldHalf]
        private float rotationSpeed;  // is this really necessary?
        [OnlineField]
        private OnlineCreature? thrownBy;
        [OnlineField]
        private byte throwDir;  // 00> 01v 10< 11^

        public RealizedWeaponState() { }
        public RealizedWeaponState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var weapon = (Weapon)onlineEntity.apo.realizedObject;
            mode = weapon.mode;
            rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, weapon.rotation);
            rotationSpeed = weapon.rotationSpeed;
            thrownBy = weapon.thrownBy?.abstractCreature?.GetOnlineCreature();
            throwDir = (byte)((weapon.throwDir.x < 0 ? 0b10 : 0b00) | (weapon.throwDir.y < 0 ? 0b01 : 0b00));
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (onlineEntity.isTransfering)
            {
                RainMeadow.Debug($"Transferring {onlineEntity}, not updating state!");
                return;
            }

            var weapon = (Weapon)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            var newMode = mode;
            
            if (weapon.room != null && weapon.mode != newMode)
            {
                RainMeadow.Debug($"{onlineEntity} new mode : {newMode}");
                weapon.ChangeMode(newMode);
                weapon.throwModeFrames = -1; // not synched, behaves as "infinite"
            }

            weapon.thrownBy = thrownBy?.realizedCreature;
            if (weapon.grabbedBy != null && weapon.grabbedBy.Count > 0) { RainMeadow.Trace($"Skipping state because grabbed"); return; }
            if (!ShouldPosBeLenient(weapon)) {
                weapon.rotation = Custom.DegToVec(rotation);
                weapon.rotationSpeed = rotationSpeed;
                weapon.throwDir = new IntVector2((throwDir & 0b01)!=0 ? 0 : (throwDir & 0b10)!=0 ? -1 : 1, (throwDir & 0b01)==0 ? 0 : (throwDir & 0b10)!=0 ? -1 : 1);
            }
        }
    }
}
