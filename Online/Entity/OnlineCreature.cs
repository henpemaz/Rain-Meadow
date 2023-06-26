using System;
using RWCustom;
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

        internal static OnlineEntity FromEvent(NewCreatureEvent newCreatureEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newCreatureEvent.seed;

            AbstractCreature ac = SaveState.AbstractCreatureFromString(inResource.World, newCreatureEvent.serializedObject, false);
            ac.ID = id;

            var oe = new OnlineCreature(ac, newCreatureEvent.seed, newCreatureEvent.realized, newCreatureEvent.owner, newCreatureEvent.entityId, newCreatureEvent.isTransferable);
            OnlinePhysicalObject.map.Add(ac, oe);
            OnlineManager.recentEntities.Add(oe.id, oe);

            newCreatureEvent.initialState.ReadTo(oe);
            return oe;
        }

        internal override NewEntityEvent AsNewEntityEvent(OnlineResource inResource)
        {
            RainMeadow.Debug($"serializing {this} in {apo.pos} as {SaveState.AbstractCreatureToStringStoryWorld(apo as AbstractCreature)}");
            return new NewCreatureEvent(seed, realized, SaveState.AbstractCreatureToStringStoryWorld(apo as AbstractCreature), inResource, this, null);
        }

        protected override EntityState MakeState(ulong tick, OnlineResource resource)
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

        public void CreatureViolence(OnlinePhysicalObject onlineVillain, int hitchunkIndex, PhysicalObject.Appendage.Pos hitappendage, Vector2? directionandmomentum, Creature.DamageType type, float damage, float stunbonus)
        {
            this.owner.QueueEvent(new CreatureEvent.Violence(onlineVillain, this, hitchunkIndex, hitappendage, directionandmomentum, type, damage, stunbonus));
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
            ForceGrab(graspRef.OnlineGrabbed, graspRef.GraspUsed, graspRef.ChunkGrabbed, castShareability, graspRef.Dominance, graspRef.Pacifying);
        }

        public void SuckedIntoShortCut(IntVector2 entrancePos, bool carriedByOther)
        {
            foreach (var participant in currentlyJoinedResource.participants)
            {
                participant.Key.QueueEvent(new CreatureEvent.SuckedIntoShortCut(this, entrancePos, carriedByOther));
            }
        }

        public override string ToString()
        {
            return (this.apo as AbstractCreature).creatureTemplate.ToString() + base.ToString();
        }
    }
}
