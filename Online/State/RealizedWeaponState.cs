using System.Linq;
using UnityEngine;

namespace RainMeadow
{

    public class RealizedWeaponState : RealizedPhysicalObjectState
    {
        // is all this data really necessary?
        [OnlineField]
        protected byte mode;
        [OnlineField]
        private Vector2 tailPos;
        [OnlineField(nullable = true)]
        private Vector2? setRotation;
        [OnlineField]
        private Vector2 rotation;
        [OnlineField]
        private Vector2 lastRotation;
        [OnlineField]
        private float rotationSpeed;
        [OnlineField(nullable = true)]
        private OnlineEntity.EntityId? thrownBy;

        public RealizedWeaponState() { }
        public RealizedWeaponState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var weapon = (Weapon)onlineEntity.apo.realizedObject;
            mode = (byte)weapon.mode;
            tailPos = weapon.tailPos;
            setRotation = weapon.setRotation;
            rotation = weapon.rotation;
            lastRotation = weapon.lastRotation;
            rotationSpeed = weapon.rotationSpeed;
            if (weapon.thrownBy != null)
            {
                OnlinePhysicalObject.map.TryGetValue(weapon.thrownBy.abstractCreature, out var onlineCreature);
                thrownBy = onlineCreature.id;
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            if (!onlineEntity.owner.isMe && onlineEntity.isPending) return; // Don't sync if pending, reduces visibility and effect of lag
            base.ReadTo(onlineEntity);
            var weapon = (Weapon)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            var newMode = new Weapon.Mode(Weapon.Mode.values.GetEntry(mode));
            if (weapon.room != null && weapon.mode != newMode) weapon.ChangeMode(newMode);
            weapon.tailPos = tailPos;
            weapon.setRotation = setRotation;
            weapon.rotation = rotation;
            weapon.lastRotation = lastRotation;
            weapon.rotationSpeed = rotationSpeed;
            if (thrownBy != null)
            {
                weapon.thrownBy = (thrownBy.FindEntity() as OnlineCreature).apo.realizedObject as Creature;
            }
        }
    }
}