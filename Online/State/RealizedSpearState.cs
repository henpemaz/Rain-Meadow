using RWCustom;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedSpearState : RealizedWeaponState
    {
        [OnlineFieldHalf(group = "spear", nullable = true)]
        private Vector2? stuckInWall;
        [OnlineField(group = "spear", nullable = true)]
        private BodyChunkRef? stuckInChunk;
        [OnlineField(group = "spear", nullable = true)]
        private AppendageRef? stuckInAppendage;
        [OnlineField(group = "spear")]
        private sbyte stuckBodyPart;
        [OnlineFieldHalf(group = "spear")]
        private float stuckRotation;
        [OnlineField(group = "spear")]
        private sbyte stuckInWallCycles;
        [OnlineField(group = "spear")]
        private bool needleActive = true;
        [OnlineField]
        private float spearDamageBonus;

        public RealizedSpearState() { }
        public RealizedSpearState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var spear = (Spear)onlineEntity.apo.realizedObject;
            stuckInWall = spear.stuckInWall;
            stuckInWallCycles = (sbyte)spear.abstractSpear.stuckInWallCycles;
            needleActive = spear.spearmasterNeedle_hasConnection;
            spearDamageBonus = spear.spearDamageBonus;

            if (spear.stuckInObject != null)
            {
                stuckInChunk = BodyChunkRef.FromBodyChunk(spear.stuckInChunk);
                stuckInAppendage = spear.stuckInAppendage != null ? new AppendageRef(spear.stuckInAppendage) : null;
                stuckBodyPart = (sbyte)spear.stuckBodyPart;
                stuckRotation = spear.stuckRotation;
            }
            else stuckInChunk = null;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var spear = (Spear)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            spear.stuckInWall = stuckInWall;
            spear.abstractSpear.stuckInWallCycles = stuckInWallCycles;
            spear.spearDamageBonus = spearDamageBonus;
            if (!stuckInWall.HasValue)
                spear.addPoles = false;
            spear.spearmasterNeedle_hasConnection = needleActive;

            if (stuckInChunk is not null)
            {
                spear.stuckInObject = stuckInChunk.owner;
                spear.stuckInChunkIndex = stuckInChunk.index;
                spear.stuckInAppendage = spear.stuckInObject != null ? stuckInAppendage?.GetAppendagePos(spear.stuckInObject) : null;
                spear.stuckBodyPart = stuckBodyPart;
                spear.stuckRotation = stuckRotation;
            }

            base.ReadTo(onlineEntity);
            if (spear.mode == Weapon.Mode.StuckInWall && !spear.stuckInWall.HasValue)
            {
                RainMeadow.Error("Stuck in wall but has no value!");
                spear.ChangeMode(Weapon.Mode.Free);
            }
            if (spear.mode == Weapon.Mode.StuckInCreature && spear.stuckInObject == null)
            {
                RainMeadow.Error("Stuck in creature but no creature");
                spear.ChangeMode(Weapon.Mode.Free);
            }
        }
    }

    public class AppendageRef : Serializer.ICustomSerializable, IEquatable<AppendageRef>
    {
        public byte appIndex;
        public byte prevSegment;
        public float distanceToNext;

        public AppendageRef() { }
        public AppendageRef(PhysicalObject.Appendage.Pos appendagePos)
        {
            appIndex = (byte)appendagePos.appendage.appIndex;
            prevSegment = (byte)appendagePos.prevSegment;
            distanceToNext = appendagePos.distanceToNext;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref appIndex);
            serializer.Serialize(ref prevSegment);
            serializer.SerializeHalf(ref distanceToNext);
        }

        public bool Equals(AppendageRef other)
        {
            return other.appIndex == appIndex && other.prevSegment == prevSegment && other.distanceToNext == distanceToNext;
        }

        public override bool Equals(object obj) => obj is AppendageRef other && Equals(other);

        public static bool operator ==(AppendageRef lhs, AppendageRef rhs) => lhs is not null && lhs.Equals(rhs);

        public static bool operator !=(AppendageRef lhs, AppendageRef rhs) => !(lhs == rhs);

        public override int GetHashCode() => appIndex + prevSegment + (int)(1024 * distanceToNext);

        public PhysicalObject.Appendage.Pos GetAppendagePos(PhysicalObject appendageOwner)
        {
            return new PhysicalObject.Appendage.Pos(appendageOwner.appendages[appIndex], prevSegment, distanceToNext);
        }
    }
}
