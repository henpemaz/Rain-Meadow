using RWCustom;
using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow
{
    public class OnlineCreature : OnlinePhysicalObject
    {
        public bool enteringShortCut;
        internal Creature realizedCreature => apo.realizedObject as Creature;

        public OnlineCreature(OnlineCreatureDefinition def, AbstractCreature ac) : base(def, ac)
        {
            // ? anything special?
        }

        public static OnlineEntity FromDefinition(OnlineCreatureDefinition newCreatureEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newCreatureEvent.seed;

            if (OnlineManager.lobby.gameMode is ArenaCompetitiveGameMode)
            {
                string[] array = Regex.Split(newCreatureEvent.serializedObject, "<cA>");
                EntityID entityID = EntityID.FromString(array[1]);

                CreatureTemplate.Type type = new CreatureTemplate.Type(array[0], false);
                RainMeadow.Debug("serializedObject: " + newCreatureEvent.serializedObject);
                AbstractCreature ac = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, new WorldCoordinate(0, -1, -1, -1), entityID);
                ac.ID = id;

                return new OnlineCreature(newCreatureEvent, ac);
            } else
            {

                RainMeadow.Debug("serializedObject: " + newCreatureEvent.serializedObject);
                AbstractCreature ac = SaveState.AbstractCreatureFromString(inResource.World, newCreatureEvent.serializedObject, false);
                ac.ID = id;

                return new OnlineCreature(newCreatureEvent, ac);
            }
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new AbstractCreatureState(this, inResource, tick);
        }

        public void RPCCreatureViolence(OnlinePhysicalObject onlineVillain, int hitchunkIndex, PhysicalObject.Appendage.Pos hitappendage, Vector2? directionandmomentum, Creature.DamageType type, float damage, float stunbonus)
        {
            this.owner.InvokeRPC(this.CreatureViolence, onlineVillain, (byte)hitchunkIndex, hitappendage == null ? null : new AppendageRef(hitappendage), directionandmomentum, type, damage, stunbonus);
        }

        [RPCMethod]
        public void CreatureViolence(OnlinePhysicalObject? onlineVillain, byte victimChunkIndex, AppendageRef? victimAppendageRef, Vector2? directionAndMomentum, Creature.DamageType damageType, float damage, float stunBonus)
        {
            var victimAppendage = victimAppendageRef?.GetAppendagePos(this);
            var creature = (this.apo.realizedObject as Creature);
            if (creature == null) return;
            creature.Violence(onlineVillain?.apo.realizedObject.firstChunk, directionAndMomentum, creature.bodyChunks[victimChunkIndex], victimAppendage, damageType, damage, stunBonus);
        }

        public void ForceGrab(OnlinePhysicalObject onlineGrabbed, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            var grabber = (Creature)this.apo.realizedObject;
            var grabbedThing = onlineGrabbed.apo.realizedObject;

            if (grabber.grasps[graspUsed] != null)
            {
                if (grabber.grasps[graspUsed].grabbed == grabbedThing) return;
                grabber.grasps[graspUsed].Release();
            }
            // Will I need to also include the shareability conflict here, too? Idk.
            grabber.grasps[graspUsed] = new Creature.Grasp(grabber, grabbedThing, graspUsed, chunkGrabbed, shareability, dominance, pacifying);
            grabbedThing.Grabbed(grabber.grasps[graspUsed]);
            new AbstractPhysicalObject.CreatureGripStick(grabber.abstractCreature, grabbedThing.abstractPhysicalObject, graspUsed, pacifying || grabbedThing.TotalMass < grabber.TotalMass);
        }
        public void ForceGrab(GraspRef graspRef)
        {
            var castShareability = new Creature.Grasp.Shareability(Creature.Grasp.Shareability.values.GetEntry(graspRef.Shareability));
            ForceGrab(graspRef.OnlineGrabbed.FindEntity() as OnlinePhysicalObject, graspRef.GraspUsed, graspRef.ChunkGrabbed, castShareability, graspRef.Dominance, graspRef.Pacifying);
        }

        public void BroadcastSuckedIntoShortCut(IntVector2 entrancePos, bool carriedByOther)
        {
            if (currentlyJoinedResource == null) return;
            foreach (var participant in currentlyJoinedResource.participants)
            {
                participant.Key.InvokeRPC(this.SuckedIntoShortCut, entrancePos, carriedByOther);
            }
        }

        [RPCMethod]
        public void SuckedIntoShortCut(IntVector2 entrancePos, bool carriedByOther)
        {
            enteringShortCut = true;
            (apo.realizedObject as Creature)?.SuckedIntoShortCut(entrancePos, carriedByOther);
        }

        public override string ToString()
        {
            return (this.apo as AbstractCreature).creatureTemplate.ToString() + base.ToString();
        }
    }
}
