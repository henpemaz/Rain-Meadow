using RWCustom;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedSpearState : RealizedWeaponState
    {
        [OnlineFieldHalf(group = "spear", nullable = true)]
        private StuckSpearState? stuck;
        [OnlineField(group = "spear")]
        private bool needleActive = true;
        [OnlineFieldHalf(group = "spear")]
        private float spearDamageBonus;

        public RealizedSpearState() { }
        public RealizedSpearState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var spear = (Spear)onlineEntity.apo.realizedObject;
            needleActive = spear.spearmasterNeedle_hasConnection;
            spearDamageBonus = spear.spearDamageBonus;

            if (spear.stuckInObject != null)
            {
                stuck = new();
                stuck.stuckInChunk = BodyChunkRef.FromBodyChunk(spear.stuckInChunk);
                stuck.stuckInAppendage = spear.stuckInAppendage != null ? new AppendageRef(spear.stuckInAppendage) : null;
                stuck.stuckBodyPart = (sbyte)spear.stuckBodyPart;
                stuck.stuckRotation = spear.stuckRotation;
            }
            else if (spear.stuckInWall != null)
            {
                stuck = new();
                stuck.stuckInWall = spear.stuckInWall;
                stuck.stuckInWallCycles = (sbyte)spear.abstractSpear.stuckInWallCycles;
                stuck.stuckInChunk = null;
            }
            else
            {
                stuck = null;
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            var spear = (Spear)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            if (stuck != null)
            {
                spear.stuckInWall = stuck.stuckInWall;
                spear.abstractSpear.stuckInWallCycles = stuck.stuckInWallCycles;
                spear.spearDamageBonus = spearDamageBonus;
                if (!stuck.stuckInWall.HasValue)
                    spear.addPoles = false;
                if (stuck.stuckInChunk is not null)
                {
                    spear.stuckInObject = stuck.stuckInChunk.owner;
                    spear.stuckInChunkIndex = stuck.stuckInChunk.index;
                    spear.stuckInAppendage = spear.stuckInObject != null ? stuck.stuckInAppendage?.GetAppendagePos(spear.stuckInObject) : null;
                    spear.stuckBodyPart = stuck.stuckBodyPart;
                    spear.stuckRotation = stuck.stuckRotation;
                }
            }
            spear.spearmasterNeedle_hasConnection = needleActive;

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

    public class StuckSpearState : RealizedWeaponState
    {
        [OnlineFieldHalf(group = "stuck", nullable = true)]
        public Vector2? stuckInWall;
        [OnlineField(group = "stuck", nullable = true)]
        public BodyChunkRef? stuckInChunk;
        [OnlineField(group = "stuck", nullable = true)]
        public AppendageRef? stuckInAppendage;
        [OnlineField(group = "stuck")]
        public sbyte stuckBodyPart;
        [OnlineFieldHalf(group = "stuck")]
        public float stuckRotation;
        [OnlineField(group = "stuck")]
        public sbyte stuckInWallCycles;
    }
}
