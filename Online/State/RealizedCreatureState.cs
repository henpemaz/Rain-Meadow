using RainMeadow.Generics;
using System;
using System.Linq;

namespace RainMeadow
{
    public class RealizedCreatureState : RealizedPhysicalObjectState
    {
        private Generics.AddRemoveUnsortedCustomSerializables<GraspRef> Grasps;

        public override RealizedPhysicalObjectState EmptyDelta() => new RealizedCreatureState();
        public RealizedCreatureState() { }
        public RealizedCreatureState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            Grasps = new((onlineCreature.apo.realizedObject as Creature).grasps?.Where(g => g != null).Select(GraspRef.FromGrasp).ToList() ?? new());
        }
        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (onlineEntity.owner.isMe || onlineEntity.isPending) return; // Don't sync if pending, reduces visibility and effect of lag
            if (onlineEntity is not OnlineCreature onlineCreature) return;
            if (onlineCreature.apo.realizedObject is not Creature creature) return;

            if (creature.grasps == null) return;
            for (var i = 0; i < creature.grasps.Length; i++)
            {
                var newGrasp = Grasps.list.FirstOrDefault(x => x.GraspUsed == i);
                if (newGrasp != null)
                {
                    onlineCreature.ForceGrab(newGrasp);
                }
                else
                {
                    creature.grasps[i]?.Release();
                }
            }
        }

        public override StateType stateType => StateType.RealizedCreatureState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeNullableDelta(ref Grasps);
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            return base.EstimatedSize(inDeltaContext) 
                + (IsDelta ? 1 : 0) 
                + (Grasps != null ? 1 + Grasps.list.Count * 20 : 0);
        }

        public override RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedCreatureState)_other;
            var delta = (RealizedCreatureState)base.Delta(_other);
            delta.Grasps = Grasps.Delta(other.Grasps);
            delta.IsEmptyDelta &= Grasps == null;
            return delta;
        }

        public override RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedCreatureState) _other;
            var result = (RealizedCreatureState)base.ApplyDelta(_other);
            result.Grasps = Grasps.ApplyDelta(other.Grasps);
            return result;
        }
    }

    public class GraspRef : Serializer.ICustomSerializable, IIdentifiable<byte>, IEquatable<GraspRef>
    {
        public OnlineEntity.EntityId OnlineGrabber; // unused and could be removed?
        public byte GraspUsed;
        public OnlineEntity.EntityId OnlineGrabbed;
        public byte ChunkGrabbed;
        public byte Shareability;
        public float Dominance;
        public bool Pacifying;

        public byte ID => GraspUsed;

        public GraspRef() { }
        public GraspRef(OnlinePhysicalObject onlineGrabber, OnlinePhysicalObject onlineGrabbed, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            OnlineGrabber = onlineGrabber.id;
            GraspUsed = (byte)graspUsed;
            OnlineGrabbed = onlineGrabbed.id;
            ChunkGrabbed = (byte)chunkGrabbed;
            Shareability = (byte)shareability;
            Dominance = dominance;
            Pacifying = pacifying;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref OnlineGrabber);
            serializer.Serialize(ref GraspUsed);
            serializer.Serialize(ref OnlineGrabbed);
            serializer.Serialize(ref ChunkGrabbed);
            serializer.Serialize(ref Shareability);
            serializer.Serialize(ref Dominance);
            serializer.Serialize(ref Pacifying);
        }

        public static GraspRef FromGrasp(Creature.Grasp grasp)
        {
            if (!OnlinePhysicalObject.map.TryGetValue(grasp.grabber.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");
            if (!OnlinePhysicalObject.map.TryGetValue(grasp.grabbed.abstractPhysicalObject, out var onlineGrabbed)) throw new InvalidOperationException("Grabbed tjing doesn't exist in online space!");
            return new GraspRef(onlineGrabber, onlineGrabbed, grasp.graspUsed, grasp.chunkGrabbed, grasp.shareability, grasp.dominance, grasp.pacifying);
        }

        public bool Equals(GraspRef other)
        {
            return other != null
                && OnlineGrabber == other.OnlineGrabber
                && GraspUsed == other.GraspUsed
                && OnlineGrabbed == other.OnlineGrabbed
                && ChunkGrabbed == other.ChunkGrabbed
                && Shareability == other.Shareability
                && Dominance == other.Dominance
                && Pacifying == other.Pacifying;
        }

        public override bool Equals(object obj) => Equals(obj as GraspRef);
        public override int GetHashCode() => GraspUsed + OnlineGrabbed.GetHashCode();
    }
}