using UnityEngine;

namespace RainMeadow
{

    public class RealizedWeaponState : RealizedPhysicalObjectState
    {
        // is all this data really necessary?
        [OnlineField]
        protected byte mode;
        [OnlineField(nullable = true)]
        private Vector2? setRotation;
        [OnlineField]
        private Vector2 rotation;
        [OnlineField]
        private float rotationSpeed;
        [OnlineField(nullable = true)]
        private OnlineEntity.EntityId? thrownBy;

        public RealizedWeaponState() { }
        public RealizedWeaponState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var weapon = (Weapon)onlineEntity.apo.realizedObject;
            mode = (byte)weapon.mode;
            setRotation = weapon.setRotation;
            rotation = weapon.rotation;
            rotationSpeed = weapon.rotationSpeed;
            thrownBy = (weapon.thrownBy?.abstractCreature != null && OnlineCreature.map.TryGetValue(weapon.thrownBy.abstractCreature, out var oc)) ? oc?.id : null;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            if (!onlineEntity.owner.isMe && onlineEntity.isPending) return; // Don't sync if pending, reduces visibility and effect of lag
            base.ReadTo(onlineEntity);
            var weapon = (Weapon)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            var newMode = new Weapon.Mode(Weapon.Mode.values.GetEntry(mode));
            if (weapon.room != null && weapon.mode != newMode)
            {
                RainMeadow.Debug($"{onlineEntity} new mode : {newMode}");
                weapon.ChangeMode(newMode);
                weapon.throwModeFrames = -1; // not synched, behaves as "infinite"
            }
            weapon.setRotation = setRotation;
            weapon.rotation = rotation;
            weapon.rotationSpeed = rotationSpeed;
            weapon.thrownBy = (thrownBy?.FindEntity() as OnlineCreature)?.realizedCreature;
        }
    }
}
