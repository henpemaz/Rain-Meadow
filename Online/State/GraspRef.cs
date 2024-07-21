using System.Runtime.CompilerServices;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.None)]
    public class GraspRef : OnlineState
    {
        public static ConditionalWeakTable<Creature.Grasp, GraspRef> map = new(); // these are generated "once per grasp" and reused with this map
        // this way we can have them be fed through the delta mechanism whith just plain referenceequals comparisons which is peak efficiency

        [OnlineField]
        public byte graspUsed;
        [OnlineField]
        public OnlineEntity.EntityId onlineGrabbed;
        [OnlineField]
        public byte chunkGrabbed;
        [OnlineField]
        public byte shareability;
        [OnlineFieldHalf]
        public float dominance;
        [OnlineField]
        public bool pacifying;

        public GraspRef() { }
        public GraspRef(OnlinePhysicalObject onlineGrabbed, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            this.graspUsed = (byte)graspUsed;
            this.onlineGrabbed = onlineGrabbed.id;
            this.chunkGrabbed = (byte)chunkGrabbed;
            this.shareability = (byte)shareability;
            this.dominance = dominance;
            this.pacifying = pacifying;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref graspUsed);
            serializer.Serialize(ref onlineGrabbed);
            serializer.Serialize(ref chunkGrabbed);
            serializer.Serialize(ref shareability);
            serializer.Serialize(ref dominance);
            serializer.Serialize(ref pacifying);
        }

        public static GraspRef FromGrasp(Creature.Grasp grasp)
        {
            if (!OnlinePhysicalObject.map.TryGetValue(grasp.grabbed.abstractPhysicalObject, out var onlineGrabbed)) { RainMeadow.Error("Grabbed tjing doesn't exist in online space!"); return null; }
            return new GraspRef(onlineGrabbed, grasp.graspUsed, grasp.chunkGrabbed, grasp.shareability, grasp.dominance, grasp.pacifying);
        }

        public bool EqualsGrasp(Creature.Grasp other, PhysicalObject grabbed)
        {
            return other != null
                && graspUsed == other.graspUsed
                && grabbed  == other.grabbed
                && chunkGrabbed == other.chunkGrabbed
                && shareability == (byte)other.shareability
                && dominance == other.dominance
                && pacifying == other.pacifying;
        }

        public void MakeGrasp(Creature creature, PhysicalObject obj)
        {
            RainMeadow.Debug(this);
            for (int i = obj.grabbedBy.Count - 1; i >= 0; i--) // handle already grabbed
            {
                Creature.Grasp grasp = obj.grabbedBy[i];
                if (grasp.grabber == creature)
                {
                    creature.ReleaseGrasp(grasp.graspUsed);
                }
            }
            creature.Grab(obj, graspUsed, chunkGrabbed, new Creature.Grasp.Shareability(Creature.Grasp.Shareability.values.GetEntry(shareability)), dominance, false, pacifying);
        }

        internal void Release(Creature.Grasp grasp)
        {
            RainMeadow.Debug(this);
            grasp.grabber.ReleaseGrasp(grasp.graspUsed);
        }
    }
}