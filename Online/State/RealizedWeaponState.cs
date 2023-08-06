using System.Linq;
using UnityEngine;

namespace RainMeadow
{

    public class RealizedWeaponState : RealizedPhysicalObjectState
    {
        // is all this data really necessary?
        protected byte mode;
        private Vector2 tailPos;
        private Vector2? setRotation;
        private Vector2 rotation;
        private Vector2 lastRotation;
        private float rotationSpeed;

        bool hasWeaponData;

        public override RealizedPhysicalObjectState EmptyDelta() => new RealizedWeaponState();
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
        }

        public override StateType stateType => StateType.RealizedWeaponState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            if (IsDelta) serializer.Serialize(ref hasWeaponData);
            if (!IsDelta || hasWeaponData)
            {
                serializer.Serialize(ref mode);
                serializer.Serialize(ref tailPos);
                serializer.SerializeNullable(ref setRotation);
                serializer.Serialize(ref rotation);
                serializer.Serialize(ref lastRotation);
                serializer.Serialize(ref rotationSpeed);
            }
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            var val = base.EstimatedSize(inDeltaContext);
            if (IsDelta) val += 1;
            if (!IsDelta || hasWeaponData)
            {
                val += 30;
                if (setRotation != null) val += 4;
            }
            return val;
        }

        public override RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedWeaponState)_other;
            var delta = (RealizedWeaponState)base.Delta(_other);
            delta.mode = mode;
            delta.tailPos = tailPos;
            delta.setRotation = setRotation;
            delta.rotation = rotation;
            delta.lastRotation = lastRotation;
            delta.rotationSpeed = rotationSpeed;
            delta.hasWeaponData = mode != other.mode
                || !tailPos.CloseEnoughZeroSnap(other.tailPos, 1f)
                || setRotation != other.setRotation
                || !rotation.CloseEnough(other.rotation, 0.01f)
                || !lastRotation.CloseEnough(other.lastRotation, 0.01f)
                || rotationSpeed != other.rotationSpeed;
            delta.IsEmptyDelta &= !delta.hasWeaponData;
            return delta;
        }

        public override RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedWeaponState)_other;
            var result = (RealizedWeaponState)base.ApplyDelta(_other);
            if (other.hasWeaponData)
            {
                result.mode = other.mode;
                result.tailPos = other.tailPos;
                result.setRotation = other.setRotation;
                result.rotation = other.rotation;
                result.lastRotation = other.lastRotation;
                result.rotationSpeed = other.rotationSpeed;
            }
            else
            {
                result.mode = mode;
                result.tailPos = tailPos;
                result.setRotation = setRotation;
                result.rotation = rotation;
                result.lastRotation = lastRotation;
                result.rotationSpeed = rotationSpeed;
            }
            return result;
        }
    }
}