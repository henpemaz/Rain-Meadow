using System;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedSpearState : RealizedWeaponState
    {
        private Vector2? stuckInWall;
        private AppendageRef stuckInAppendage;
        private OnlineEntity.EntityId stuckInObject;
        private byte stuckInChunkIndex;
        private sbyte stuckBodyPart;
        private float stuckRotation;


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

        public override StateType stateType => StateType.RealizedSpearState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref stuckInWall);
            serializer.SerializeNullable(ref stuckInObject);
            if (stuckInObject != null)
            {
                serializer.Serialize(ref stuckInChunkIndex);
                serializer.SerializeNullable(ref stuckInAppendage);
                serializer.Serialize(ref stuckBodyPart);
                serializer.Serialize(ref stuckRotation);
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
    }

    public class AppendageRef : Serializer.ICustomSerializable
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
            serializer.Serialize(ref distanceToNext);
        }

        public PhysicalObject.Appendage.Pos GetAppendagePos(OnlinePhysicalObject appendageOwner)
        {
            if (appendageOwner == null) return null;
            var physicalObject = appendageOwner.apo.realizedObject;
            var appendage = physicalObject.appendages[appIndex];
            return new PhysicalObject.Appendage.Pos(appendage, prevSegment, distanceToNext);
        }
    }
}