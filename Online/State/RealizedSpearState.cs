using System;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedSpearState : RealizedWeaponState
    {
        private Vector2? stuckInWall;
        private OnlineEntity.EntityId stuckInObject;
        private AppendageRef stuckInAppendage;
        private byte stuckInChunkIndex;
        private sbyte stuckBodyPart;
        private float stuckRotation;

        bool hasSpearData;

        public override RealizedPhysicalObjectState EmptyDelta() => new RealizedSpearState();
        public RealizedSpearState() { }
        public RealizedSpearState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var spear = (Spear)onlineEntity.apo.realizedObject;
            stuckInWall = spear.stuckInWall;

            if (spear.stuckInObject != null)
            {
                if (!OnlinePhysicalObject.map.TryGetValue(spear.stuckInObject.abstractPhysicalObject, out var onlineStuckEntity)) throw new InvalidOperationException("Stuck to a non-synced creature!");
                stuckInObject = onlineStuckEntity?.id;
                stuckInChunkIndex = (byte)spear.stuckInChunkIndex;
                stuckInAppendage = spear.stuckInAppendage != null ? new AppendageRef(spear.stuckInAppendage) : null;
                stuckBodyPart = (sbyte)spear.stuckBodyPart;
                stuckRotation = spear.stuckRotation;
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            if (!onlineEntity.owner.isMe && onlineEntity.isPending) return; // Don't sync if pending, reduces visibility and effect of lag
            var spear = (Spear)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;
            spear.stuckInWall = stuckInWall;
            if (!stuckInWall.HasValue)
                spear.addPoles = false;

            spear.stuckInObject = (stuckInObject?.FindEntity() as OnlinePhysicalObject)?.apo.realizedObject;
            if (spear.stuckInObject != null)
            {
                spear.stuckInChunkIndex = stuckInChunkIndex;
                spear.stuckInAppendage = stuckInAppendage?.GetAppendagePos(stuckInObject.FindEntity() as OnlinePhysicalObject);
                spear.stuckBodyPart = stuckBodyPart;
                spear.stuckRotation = stuckRotation;
            }

            base.ReadTo(onlineEntity);
            var newMode = new Weapon.Mode(Weapon.Mode.values.GetEntry(mode));
            if (newMode == Weapon.Mode.StuckInWall && !stuckInWall.HasValue)
            {
                RainMeadow.Error("Stuck in wall but has no value!");
            }
        }

        public override StateType stateType => StateType.RealizedSpearState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            if (IsDelta) serializer.Serialize(ref hasSpearData);
            if (!IsDelta || hasSpearData)
            {
                serializer.SerializeNullable(ref stuckInWall);
                serializer.SerializeNullable(ref stuckInObject);
                if (stuckInObject != null)
                {
                    serializer.Serialize(ref stuckInChunkIndex);
                    serializer.SerializeNullable(ref stuckInAppendage);
                    serializer.Serialize(ref stuckBodyPart);
                    serializer.Serialize(ref stuckRotation);
                }
            }
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            var val = base.EstimatedSize(inDeltaContext);
            if (IsDelta) val += 1;
            if (!IsDelta || hasSpearData)
            {
                val += 2;
                if (stuckInWall != null) val += 8;
                if (stuckInObject != null) val += 7;
                if (stuckInObject != null && stuckInAppendage != null) val += 4;
            }
            return val;
        }

        public override RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedSpearState)_other;
            var delta = (RealizedSpearState)base.Delta(_other);
            delta.stuckInWall = stuckInWall;
            delta.stuckInObject = stuckInObject;
            delta.stuckInChunkIndex = stuckInChunkIndex;
            delta.stuckInAppendage = stuckInAppendage;
            delta.stuckBodyPart = stuckBodyPart;
            delta.stuckRotation = stuckRotation;
            delta.hasSpearData = stuckInWall != other.stuckInWall
                || stuckInObject != other.stuckInObject
                || stuckInChunkIndex != other.stuckInChunkIndex
                || stuckInAppendage.Equals(other.stuckInAppendage)
                || stuckBodyPart != other.stuckBodyPart
                || stuckRotation != other.stuckRotation;
            delta.IsEmptyDelta &= !delta.hasSpearData;
            return delta;
        }

        public override RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedSpearState)_other;
            var result = (RealizedSpearState)base.ApplyDelta(_other);
            if (other.hasSpearData)
            {
                result.stuckInWall = other.stuckInWall;
                result.stuckInObject = other.stuckInObject;
                result.stuckInChunkIndex = other.stuckInChunkIndex;
                result.stuckInAppendage = other.stuckInAppendage;
                result.stuckBodyPart = other.stuckBodyPart;
                result.stuckRotation = other.stuckRotation;
            }
            else
            {
                result.stuckInWall = stuckInWall;
                result.stuckInObject = stuckInObject;
                result.stuckInChunkIndex = stuckInChunkIndex;
                result.stuckInAppendage = stuckInAppendage;
                result.stuckBodyPart = stuckBodyPart;
                result.stuckRotation = stuckRotation;
            }
            return result;
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
            return other != null && other.appIndex == appIndex && other.prevSegment == prevSegment && other.distanceToNext == distanceToNext;
        }

        public override bool Equals(object obj) => Equals(obj as AppendageRef);

        public override int GetHashCode() => appIndex + prevSegment + (int)(1024 * distanceToNext);

        public PhysicalObject.Appendage.Pos GetAppendagePos(OnlinePhysicalObject appendageOwner)
        {
            if (appendageOwner == null) return null;
            var physicalObject = appendageOwner.apo.realizedObject;
            var appendage = physicalObject.appendages[appIndex];
            return new PhysicalObject.Appendage.Pos(appendage, prevSegment, distanceToNext);
        }
    }
}