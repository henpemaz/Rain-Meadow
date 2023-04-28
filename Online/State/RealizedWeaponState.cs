using UnityEngine;

namespace RainMeadow
{

    public class RealizedWeaponState : RealizedObjectState
    {
        private byte mode;
        private Vector2 rotation;
        private Vector2 lastRotation;
        private float rotationSpeed;
        
        public RealizedWeaponState()
        {
        }

        public RealizedWeaponState(OnlineEntity onlineEntity) : base(onlineEntity)
        {
            var weapon = (Weapon)onlineEntity.entity.realizedObject;
            mode = (byte)weapon.mode;
            rotation = weapon.rotation;
            lastRotation = weapon.lastRotation;
            rotationSpeed = weapon.rotationSpeed;
        }

        public override StateType stateType => StateType.RealizedWeaponState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref mode);
            serializer.Serialize(ref rotation);
            serializer.Serialize(ref lastRotation);
            serializer.Serialize(ref rotationSpeed);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var weapon = (Weapon)onlineEntity.entity.realizedObject;
            weapon.ChangeMode(new Weapon.Mode(Weapon.Mode.values.GetEntry(mode)));
            weapon.rotation = rotation;
            weapon.lastRotation = lastRotation;
            weapon.rotationSpeed = rotationSpeed;
        }
    }
}