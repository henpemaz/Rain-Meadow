using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class RealizedCreatureState : PhysicalObjectState
    {
        private List<GraspRef> Grasps = new List<GraspRef>();
        public RealizedCreatureState() { }
        public RealizedCreatureState(OnlineEntity onlineEntity) : base(onlineEntity)
        {
            if (onlineEntity.entity.realizedObject is not Creature creature) return;
            
            if (creature.grasps == null) return;
            foreach (var grasp in creature.grasps)
            {
                if (grasp == null) continue;
                Grasps.Add(GraspRef.FromGrasp(grasp));
            }
        }

        public override StateType stateType => StateType.RealizedCreatureState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref Grasps);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (onlineEntity.entity.realizedObject is not Creature creature) return;

            if (creature.grasps == null) return;
            for (var i = 0; i < creature.grasps.Length; i++)
            {
                var newGrasp = Grasps.FirstOrDefault(x => x.GraspUsed == i);
                if (newGrasp != null)
                {
                    onlineEntity.ForceGrab(newGrasp);
                }
                else
                {
                    creature.grasps[i]?.Release();
                }
            }
        }
    }
    
    public class GraspRef : Serializer.ICustomSerializable
    {
        public OnlineEntity OnlineGrabber;
        public byte GraspUsed;
        public OnlineEntity OnlineGrabbed;
        public byte ChunkGrabbed;
        public byte Shareability;
        public float Dominance;
        public bool Pacifying;
        public GraspRef() { }
        public GraspRef(OnlineEntity onlineGrabber, OnlineEntity onlineGrabbed, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            OnlineGrabber = onlineGrabber;
            GraspUsed = (byte)graspUsed;
            OnlineGrabbed = onlineGrabbed;
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
            if (!OnlineEntity.map.TryGetValue(grasp.grabbed.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");
            if (!OnlineEntity.map.TryGetValue(grasp.grabbed.abstractPhysicalObject, out var onlineGrabbed)) throw new InvalidOperationException("Grabbed tjing doesn't exist in online space!");
            return new GraspRef(onlineGrabber, onlineGrabbed, grasp.graspUsed, grasp.chunkGrabbed, grasp.shareability, grasp.dominance, grasp.pacifying);
        }
    }
}