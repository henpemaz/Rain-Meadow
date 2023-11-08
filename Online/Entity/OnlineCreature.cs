using RWCustom;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class OnlineCreature : OnlinePhysicalObject
    {
        public bool enteringShortCut;

        public OnlineCreature(AbstractCreature ac, int seed, bool realized, OnlinePlayer owner, EntityId id, bool isTransferable) : base(ac, seed, realized, owner, id, isTransferable)
        {
            // ? anything special?
        }

        public static OnlineEntity FromEvent(NewCreatureEvent newCreatureEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newCreatureEvent.seed;

            RainMeadow.Debug("serializedObject: " + newCreatureEvent.serializedObject);
            AbstractCreature ac = SaveState.AbstractCreatureFromString(inResource.World, newCreatureEvent.serializedObject, false);
            ac.ID = id;

            var oe = new OnlineCreature(ac, newCreatureEvent.seed, newCreatureEvent.realized, OnlineManager.lobby.PlayerFromId(newCreatureEvent.owner), newCreatureEvent.entityId, newCreatureEvent.isTransferable);
            try
            {
                map.Add(ac, oe);
                OnlineManager.recentEntities.Add(oe.id, oe);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                RainMeadow.Error(Environment.StackTrace);
            }
            return oe;
        }

        public override NewEntityEvent AsNewEntityEvent(OnlineResource inResource)
        {
            RainMeadow.Debug($"serializing {this} in {apo.pos} as {SaveState.AbstractCreatureToStringStoryWorld(apo as AbstractCreature)}");
            return new NewCreatureEvent(seed, realized, SaveState.AbstractCreatureToStringStoryWorld(apo as AbstractCreature), inResource, this, null);
        }

        protected override EntityState MakeState(uint tick, OnlineResource resource)
        {
            if (resource is WorldSession ws && !OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) throw new InvalidOperationException("asked for world state, not synched");
            if (resource is RoomSession rs && !OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) throw new InvalidOperationException("asked for room state, not synched");
            var realizedState = resource is RoomSession;
            if (realizedState) { if (apo.realizedObject != null && !realized) RainMeadow.Error($"have realized object, but not entity not marked as realized??: {this} in resource {resource}"); }
            if (realizedState && !realized)
            {
                //throw new InvalidOperationException("asked for realized state, not realized");
                RainMeadow.Error($"asked for realized state, not realized: {this} in resource {resource}");
                realizedState = false;
            }
            return new AbstractCreatureState(this, tick, realizedState);
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
