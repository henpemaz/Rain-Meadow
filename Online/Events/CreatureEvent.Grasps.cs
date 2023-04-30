using System.Linq;

namespace RainMeadow;

public partial class CreatureEvent
{
    public class GraspRequest : OnlineEvent
    {
        private OnlineEntity OnlineGrabber;
        private OnlineEntity OnlineGrabbed;
        private byte GraspUsed;
        private byte ChunkGrabbed;
        private byte Shareability;
        private float Dominance;
        private bool Pacifying;

        private bool EventResult = true;

        public GraspRequest()
        {
        }

        public GraspRequest(OnlineEntity onlineGrabber, OnlineEntity onlineGrabbed, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            OnlineGrabber = onlineGrabber;
            GraspUsed = (byte)graspUsed;
            OnlineGrabbed = onlineGrabbed;
            ChunkGrabbed = (byte)chunkGrabbed;
            Shareability = (byte)shareability;
            Dominance = dominance;
            Pacifying = pacifying;
        }

        public override EventTypeId eventType => EventTypeId.CreatureEventGraspRequest;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref EventResult);
            serializer.Serialize(ref OnlineGrabber);
            serializer.Serialize(ref GraspUsed);
            if (!EventResult) return;
            serializer.Serialize(ref OnlineGrabbed);
            serializer.Serialize(ref ChunkGrabbed);
            serializer.Serialize(ref Shareability);
            serializer.Serialize(ref Dominance);
            serializer.Serialize(ref Pacifying);
        }

        public override void Process()
        {
            // If result = OK;
            var CastShareability = new Creature.Grasp.Shareability(Creature.Grasp.Shareability.values.GetEntry(Shareability));

            var sendList = OnlineGrabber.roomSession.memberships.Select(x => x.Key);

            foreach (var onlinePlayer in sendList)
            {
                onlinePlayer.QueueEvent(new GraspRequestResolve(EventResult, OnlineGrabber, OnlineGrabbed, GraspUsed, ChunkGrabbed, CastShareability, Dominance, Pacifying));
            }
        }
    }

    public class GraspRequestResolve : OnlineEvent
    {
        private OnlineEntity OnlineGrabber;
        private OnlineEntity OnlineGrabbed;
        private byte GraspUsed;
        private byte ChunkGrabbed;
        private byte Shareability;
        private float Dominance;
        private bool Pacifying;
        private bool EventResult;

        public GraspRequestResolve()
        {
        }

        public GraspRequestResolve(bool eventResult, OnlineEntity onlineGrabber, OnlineEntity onlineGrabbed, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            EventResult = eventResult;
            OnlineGrabber = onlineGrabber;
            GraspUsed = (byte)graspUsed;
            if (!eventResult) return;
            OnlineGrabbed = onlineGrabbed;
            if (to == OnlineGrabber.owner) return;
            ChunkGrabbed = (byte)chunkGrabbed;
            Shareability = (byte)shareability;
            Dominance = dominance;
            Pacifying = pacifying;
        }

        public override EventTypeId eventType => EventTypeId.CreatureEventGraspRequestResolve;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref EventResult);
            serializer.Serialize(ref OnlineGrabber);
            serializer.Serialize(ref GraspUsed);
            if (!EventResult) return;
            serializer.Serialize(ref OnlineGrabbed);
            if (to == OnlineGrabber.owner) return;
            serializer.Serialize(ref ChunkGrabbed);
            serializer.Serialize(ref Shareability);
            serializer.Serialize(ref Dominance);
            serializer.Serialize(ref Pacifying);
        }

        public override void Process()
        {
            var CastShareability = new Creature.Grasp.Shareability(Creature.Grasp.Shareability.values.GetEntry(Shareability));

            var grabber = (Creature)OnlineGrabber.entity.realizedObject;

            if (EventResult)
            {
                if (!OnlineGrabber.owner.isMe) // Remote player
                {
                    grabber.Grab(OnlineGrabbed.entity.realizedObject, GraspUsed, ChunkGrabbed, CastShareability, Dominance, true, Pacifying);
                }

                if (OnlineGrabber.owner.isMe && !OnlineGrabbed.owner.isMe && OnlineGrabbed.isTransferable && !OnlineGrabbed.isPending)
                {
                    OnlineGrabbed.Request();
                }
            }
            else
            {
                grabber.ReleaseGrasp(GraspUsed);
            }
        }
    }
    
    public class GraspRelease : OnlineEvent
    {
        private OnlineEntity OnlineGrabber;
        private byte GraspUsed;

        public GraspRelease() { }

        public GraspRelease(OnlineEntity onlineGrabber, int graspUsed)
        {
            OnlineGrabber = onlineGrabber;
            GraspUsed = (byte)graspUsed;
        }

        public override EventTypeId eventType => EventTypeId.CreatureEventGraspRelease;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref OnlineGrabber);
            serializer.Serialize(ref GraspUsed);
        }

        public override void Process()
        {
            var grabber = (Creature)OnlineGrabber.entity.realizedObject;
            grabber.grasps[GraspUsed]?.Release();
        }
    }
}