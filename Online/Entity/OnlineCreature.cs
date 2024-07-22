using RWCustom;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow
{
    public class OnlineCreature : OnlinePhysicalObject
    {
        public class OnlineCreatureDefinition : OnlinePhysicalObjectDefinition
        {
            public OnlineCreatureDefinition() { }

            public OnlineCreatureDefinition(OnlineCreature onlineCreature, OnlineResource inResource) : base(onlineCreature, inResource)
            {
                if (RainMeadow.isArenaMode(out var _)) {
                this.serializedObject = SaveState.AbstractCreatureToStringSingleRoomWorld(onlineCreature.abstractCreature);


                } else {
                this.serializedObject = SaveState.AbstractCreatureToStringStoryWorld(onlineCreature.abstractCreature);
                }
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlineCreature(this, inResource, (AbstractCreatureState)initialState);
            }
        }

        public bool enteringShortCut;
        internal AbstractCreature creature => apo as AbstractCreature;
        internal Creature realizedCreature => apo.realizedObject as Creature;
        public AbstractCreature abstractCreature => apo as AbstractCreature;

        public OnlineCreature(AbstractCreature ac, EntityId id, OnlinePlayer owner, bool isTransferable) : base(ac, id, owner, isTransferable)
        {

        }

        public OnlineCreature(OnlineCreatureDefinition onlineCreatureDefinition, OnlineResource inResource, AbstractCreatureState initialState) : base(onlineCreatureDefinition, inResource, initialState)
        {

        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new OnlineCreatureDefinition(this, onlineResource);
        }

        public static AbstractCreature AbstractCreatureFromString(World world, string creatureString)
        {
            string[] array = Regex.Split(creatureString, "<cA>");
            CreatureTemplate.Type type = new CreatureTemplate.Type(array[0], false);
            if (type.Index == -1)
            {
                RainMeadow.Debug("Unknown creature: " + array[0] + " creature not spawning");
                return null;
            }
            string[] array2 = array[2].Split(new char[]
            {
            '.'
            });
            EntityID id = EntityID.FromString(array[1]);
            int? num = BackwardsCompatibilityRemix.ParseRoomIndex(array2[0]);
            if (num == null || !world.IsRoomInRegion(num.Value))
            {
                num = world.GetAbstractRoom(array2[0]).index;
            }
            WorldCoordinate den = new WorldCoordinate(num.Value, -1, -1, int.Parse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture));
            AbstractCreature abstractCreature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, den, id);
            if (world != null)
            {
                abstractCreature.state.LoadFromString(Regex.Split(array[3], "<cB>"));
                if (abstractCreature.Room == null)
                {
                    RainMeadow.Debug(string.Concat(new string[]
                        {
                        "Spawn room does not exist: ",
                        array2[0],
                        " ~ ",
                        id.spawner.ToString(),
                        " creature not spawning"
                        }));
                    return null;
                }
                abstractCreature.setCustomFlags();
            }
            return abstractCreature;
        }

        protected override AbstractPhysicalObject ApoFromDef(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource, PhysicalObjectEntityState initialState)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);

            var apo = AbstractCreatureFromString(world, newObjectEvent.serializedObject);
            id.altSeed = apo.ID.RandomSeed;
            apo.ID = id;
            apo.pos = initialState.pos;
            return apo;
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new AbstractCreatureState(this, inResource, tick);
        }

        public void RPCCreatureViolence(OnlinePhysicalObject onlineVillain, int? hitchunkIndex, PhysicalObject.Appendage.Pos hitappendage, Vector2? directionandmomentum, Creature.DamageType type, float damage, float stunbonus)
        {
            byte chunkIndex = (byte)(hitchunkIndex ?? 255);
            this.owner.InvokeRPC(this.CreatureViolence, onlineVillain, chunkIndex, hitappendage == null ? null : new AppendageRef(hitappendage), directionandmomentum, type, damage, stunbonus);
        }

        [RPCMethod]
        public void CreatureViolence(OnlinePhysicalObject? onlineVillain, byte victimChunkIndex, AppendageRef? victimAppendageRef, Vector2? directionAndMomentum, Creature.DamageType damageType, float damage, float stunBonus)
        {
            var victimAppendage = victimAppendageRef?.GetAppendagePos(this);
            var creature = (this.apo.realizedObject as Creature);
            if (creature == null) return;

            BodyChunk? hitChunk = victimChunkIndex < 255 ? creature.bodyChunks[victimChunkIndex] : null;
            creature.Violence(onlineVillain?.apo.realizedObject.firstChunk, directionAndMomentum, hitChunk, victimAppendage, damageType, damage, stunBonus);
        }

        //public void ForceGrab(GraspRef graspRef)
        //{
        //    var castShareability = new Creature.Grasp.Shareability(Creature.Grasp.Shareability.values.GetEntry(graspRef.Shareability));
        //    var other = graspRef.OnlineGrabbed.FindEntity(quiet: true) as OnlinePhysicalObject;
        //    if (other != null && other.apo.realizedObject != null)
        //    {
        //        var grabber = (Creature)this.apo.realizedObject;
        //        var grabbedThing = other.apo.realizedObject;
        //        var graspUsed = graspRef.GraspUsed;

        //        if (grabber.grasps[graspUsed] != null)
        //        {
        //            if (grabber.grasps[graspUsed].grabbed == grabbedThing) return;
        //            grabber.grasps[graspUsed].Release();
        //        }
        //        grabber.grasps[graspUsed] = new Creature.Grasp(grabber, grabbedThing, graspUsed, graspRef.ChunkGrabbed, castShareability, graspRef.Dominance, graspRef.Pacifying);
        //        grabbedThing.room = grabber.room;
        //        grabbedThing.Grabbed(grabber.grasps[graspUsed]);
        //        new AbstractPhysicalObject.CreatureGripStick(grabber.abstractCreature, grabbedThing.abstractPhysicalObject, graspUsed, graspRef.Pacifying || grabbedThing.TotalMass < grabber.TotalMass);
        //    }
        //}

        public void BroadcastSuckedIntoShortCut(IntVector2 entrancePos, bool carriedByOther)
        {
            if (currentlyJoinedResource is RoomSession room)
            {
                RainMeadow.Debug(this);
                if (id.type == 0) throw new InvalidProgrammerException("here");
                foreach (var participant in room.participants)
                {
                    if (!participant.isMe)
                    {
                        participant.InvokeRPC(this.SuckedIntoShortCut, entrancePos, carriedByOther);
                    }
                }
            }
        }

        [RPCMethod]
        public void SuckedIntoShortCut(IntVector2 entrancePos, bool carriedByOther)
        {
            RainMeadow.Debug(this);
            enteringShortCut = true;
            var creature = (apo.realizedObject as Creature);
            var room = creature.room;
            creature.SuckedIntoShortCut(entrancePos, carriedByOther);
            if (creature.graphicsModule != null)
            {
            {
                Vector2 vector = room.MiddleOfTile(entrancePos) + Custom.IntVector2ToVector2(room.ShorcutEntranceHoleDirection(entrancePos)) * -5f;
                creature.graphicsModule.SuckedIntoShortCut(vector);
            }
            }
            enteringShortCut = false;
        }

        public override string ToString()
        {
            return (this.apo as AbstractCreature).creatureTemplate.ToString() + base.ToString();
        }
    }
}