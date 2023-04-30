using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class RealizedSpearState : RealizedWeaponState
    {
        private Vector2? stuckInWall;
        private AppendageRef stuckInAppendage;
        private OnlineEntity stuckInObject;
        private byte stuckInChunkIndex;
        private sbyte stuckBodyPart;
        private float stuckRotation;
        
        
        public RealizedSpearState() { }
        public RealizedSpearState(OnlineEntity onlineEntity) : base(onlineEntity)
        {
            var spear = (Spear)onlineEntity.entity.realizedObject;
            stuckInWall = spear.stuckInWall;

            if (spear.stuckInObject != null)
            {
                if (!OnlineEntity.map.TryGetValue(spear.stuckInObject.abstractPhysicalObject, out var onlineStuckEntity)) throw new InvalidOperationException("Stuck to a non-synced creature!");
                stuckInObject = onlineStuckEntity;
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
            var spear = (Spear)onlineEntity.entity.realizedObject;
            spear.stuckInWall = stuckInWall;
            if (!stuckInWall.HasValue) 
                spear.addPoles = false;

            spear.stuckInObject = stuckInObject?.entity.realizedObject;
            if (spear.stuckInObject != null)
            {
                spear.stuckInChunkIndex = stuckInChunkIndex;
                spear.stuckInAppendage = GetAppendagePos(stuckInAppendage);
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

        private PhysicalObject.Appendage.Pos GetAppendagePos(AppendageRef appendageRef)
        {
            if (appendageRef == null || stuckInObject == null) return null;
            var physicalObject = stuckInObject.entity.realizedObject;
            var appendage = physicalObject.appendages[appendageRef.appIndex];
            return new PhysicalObject.Appendage.Pos(appendage, appendageRef.prevSegment, appendageRef.distanceToNext);
        }
    }

    public class AppendageRef : Serializer.ICustomSerializable
    {
        public byte appIndex;
        public byte prevSegment;
        public float distanceToNext;

        public AppendageRef() { }
        public AppendageRef(PhysicalObject.Appendage.Pos spearStuckInAppendage)
        {
            appIndex = (byte)spearStuckInAppendage.appendage.appIndex;
            prevSegment = (byte)spearStuckInAppendage.prevSegment;
            distanceToNext = spearStuckInAppendage.distanceToNext;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref appIndex);
            serializer.Serialize(ref prevSegment);
            serializer.Serialize(ref distanceToNext);
        }
    }
}