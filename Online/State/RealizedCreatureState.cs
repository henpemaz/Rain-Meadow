using RainMeadow.Generics;
using RWCustom;
using System;
using System.Linq;

namespace RainMeadow
{
    public class RealizedCreatureState : RealizedPhysicalObjectState
    {
        [OnlineField(nullable = true)]
        private DynamicOrderedStates<GraspRef> grasps;
        [OnlineField(group = "counters")]
        public short stun;
        [OnlineField(nullable = true)]
        private IntVector2? enteringShortcut;
        [OnlineField]
        private WorldCoordinate transportationDestination;
        [OnlineField(nullable = true)]
        public ArtificialIntelligenceState? artificialIntelligenceState;

        public RealizedCreatureState() { }
        public RealizedCreatureState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var creature = onlineCreature.apo.realizedObject as Creature;
            grasps = new(creature.grasps?.Where(g => g != null && OnlinePhysicalObject.map.TryGetValue(g.grabbed.abstractPhysicalObject, out _))
                                        .Select(g => GraspRef.map.GetValue(g, GraspRef.FromGrasp))
                                        .ToList() ?? new());
            stun = (short)creature.stun;
            enteringShortcut = creature.enteringShortCut;
            transportationDestination = creature.NPCTransportationDestination;
            artificialIntelligenceState = GetCreatureAIState(onlineCreature);
        }

        private bool ShouldSyncAI(CreatureTemplate.Type type) {
            if (type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC) return true;
            return false;
        }

        protected virtual ArtificialIntelligenceState? GetCreatureAIState(OnlineCreature onlineCreature)
        {
            if (onlineCreature.apo is AbstractCreature creature && creature.creatureTemplate.AI) {
                if (ShouldSyncAI(creature.creatureTemplate.type)) {
                    return new ArtificialIntelligenceState(onlineCreature);
                }
            }
            return null;
        }

        // see RealizedPhysicalObject constructor.
        override public bool ShouldSyncChunks(PhysicalObject po) {
            if(po is Creature critter) {
                if (critter.inShortcut) return false;
            }

            return true;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (onlineEntity is not OnlineCreature onlineCreature) { RainMeadow.Error("target not onlinecreature: " + onlineEntity); return; }
            if (onlineCreature.apo.realizedObject is not Creature creature) { RainMeadow.Trace("target not realized: " + onlineEntity); return; }

            creature.stun = stun;
            creature.enteringShortCut = enteringShortcut;
            creature.NPCTransportationDestination = transportationDestination;

            if (creature.grasps != null)
            {
                bool[] found = new bool[creature.grasps.Length];
                RainMeadow.Trace($"incoming grasps for {onlineEntity}: " + grasps.list.Count);
                for (int i = 0; i < grasps.list.Count; i++)
                {
                    var grasp = grasps.list[i];
                    var grabbed = (grasp.onlineGrabbed.FindEntity() as OnlinePhysicalObject)?.apo.realizedObject; // lookup once, use multiple times
                    if (grabbed == null) continue;
                    var foundat = Array.FindIndex(creature.grasps, s => grasp.EqualsGrasp(s, grabbed));
                    if (foundat == -1)
                    {
                        RainMeadow.Trace("incoming grasps not found: " + grasp);
                        grasp.MakeGrasp(creature, grabbed);
                        found[grasp.graspUsed] = true;
                    }
                    else
                    {
                        RainMeadow.Trace("incoming grasps found: " + grasp + " at index " + foundat);
                        found[foundat] = true;
                    }
                }
                for (int i = found.Length - 1; i >= 0; i--)
                {
                    if (!found[i] && creature.grasps[i] != null && OnlinePhysicalObject.map.TryGetValue(creature.grasps[i].grabbed.abstractPhysicalObject, out _))
                    {
                        RainMeadow.Trace("releasing grasp because not found at index " + i);
                        GraspRef.map.GetValue(creature.grasps[i], GraspRef.FromGrasp).Release(creature.grasps[i]);
                    }
                }
            }
        }
    }
}