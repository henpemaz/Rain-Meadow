﻿using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow
{
    public class OnlineCreature : OnlinePhysicalObject
    {
        [OmitFields("apoType")]
        public class OnlineCreatureDefinition : OnlinePhysicalObjectDefinition
        {
            [OnlineField]
            private CreatureTemplate.Type creatureType;

            public OnlineCreatureDefinition() { }

            public OnlineCreatureDefinition(OnlineCreature onlineCreature, OnlineResource inResource) : base(onlineCreature, inResource)
            {
                this.creatureType = onlineCreature.creature.creatureTemplate.type;
            }

            protected override int ExtrasIndex => 3;

            protected override void StoreSerializedObject(OnlinePhysicalObject onlinePhysicalObject)
            {
                var onlineCreature = (OnlineCreature)onlinePhysicalObject;

                string serializedObject = null;
                if (RainMeadow.isArenaMode(out var _))
                {
                    serializedObject = SaveState.AbstractCreatureToStringSingleRoomWorld(onlineCreature.abstractCreature);
                }
                else
                {
                    serializedObject = SaveState.AbstractCreatureToStringStoryWorld(onlineCreature.abstractCreature);
                }
                RainMeadow.Debug("Data is " + serializedObject);
                int index = 0;
                int count = ExtrasIndex;
                for (int i = 0; i < count; i++) index = serializedObject.IndexOf("<cA>", index + 4); // the first X fields are already saved
                if (index == -1) // no extra data
                {
                    RainMeadow.Debug("no extra");
                    extraData = "";
                }
                else
                {
                    RainMeadow.Debug("extra is  " + serializedObject.Substring(index + 4));
                    CreatureSaveExtras(serializedObject.Substring(index + 4));
                }


                this.creatureType = onlineCreature.creature.creatureTemplate.type; // we sneak this in here since this is called from apodef ctor before our own ctor
                RainMeadow.Debug("resulting object would be: " + MakeSerializedObject(new PhysicalObjectEntityState() { pos = onlinePhysicalObject.apo.pos }));
            }

            protected void CreatureSaveExtras(string extras)
            {
                extraData = extras.Replace("<cA>", "\u0001").Replace("<cB>", "\u0002").Replace("<cC>", "\u0003");
            }

            protected string CreatureBuildExtras()
            {
                return extraData.Replace("\u0001", "<cA>").Replace("\u0002", "<cB>").Replace("\u0003", "<cC>");
            }

            public override string MakeSerializedObject(PhysicalObjectEntityState initialState)
            {
                if (string.IsNullOrEmpty(extraData))
                {
                    return MakeSerializedObjectNoExtras(initialState);
                }
                else
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}<cA>{1}", MakeSerializedObjectNoExtras(initialState), CreatureBuildExtras());
                }
            }

            protected override string MakeSerializedObjectNoExtras(PhysicalObjectEntityState initialState)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}<cA>{1}<cA>{2}.{3}<cA>",
                    creatureType.ToString(),
                    new EntityID(apoSpawn == ushort.MaxValue ? -1 : apoSpawn, apoId).ToString(),
                    initialState.pos.ResolveRoomName(), // this uses story index, doesn't work in arena but we use pos directly anyways
                    initialState.pos.abstractNode
                    );
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlineCreature(this, inResource, (AbstractCreatureState)initialState);
            }
        }

        public bool enteringShortCut;
        public AbstractCreature creature => apo as AbstractCreature;
        public Creature realizedCreature => apo.realizedObject as Creature;
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

        public static AbstractCreature AbstractCreatureFromString(World world, string creatureString, WorldCoordinate pos)
        {
            // copy-paste-addapt from vanilla
            // vanilla has annoyiances with 1. non-unique roomnames such as gates
            // 2. arena room names being completelly unsupported
            // completely ignore pos in the string, it's the initialstates anyways
            // and parsing room names doesn't work in arena, stick with indexes
            string[] array = Regex.Split(creatureString, "<cA>");
            CreatureTemplate.Type type = new CreatureTemplate.Type(array[0], false);
            if (type.Index == -1)
            {
                RainMeadow.Debug("Unknown creature: " + array[0] + " creature not spawning");
                return null;
            }
            string[] array2 = array[2].Split('.');
            EntityID id = EntityID.FromString(array[1]);
            AbstractCreature abstractCreature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, pos, id);

            foreach (var item in abstractCreature.stuckObjects.ToArray()) // Some (dropbug) creatures spawn with random items attached
            {
                item.Deactivate();
            }

            abstractCreature.state.LoadFromString(Regex.Split(array[3], "<cB>"));
            abstractCreature.setCustomFlags();
            return abstractCreature;
        }

        protected override AbstractPhysicalObject ApoFromDef(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource, PhysicalObjectEntityState initialState)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();

            string serializedObject = newObjectEvent.MakeSerializedObject(initialState);
            RainMeadow.Debug("serializedObject: " + serializedObject);

            var apo = AbstractCreatureFromString(world, serializedObject, initialState.pos);
            id.altSeed = apo.ID.RandomSeed;
            apo.ID = id;
            apo.pos = initialState.pos;
            return apo;
        }

        public override void Deregister()
        {
            base.Deregister();
            if (apo.realizedObject is Creature critter && critter.grasps != null)
            {
                foreach (var item in critter.grasps)
                {
                    if (item != null)
                    {
                        GraspRef.map.Remove(item);
                    }
                }
            }
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new AbstractCreatureState(this, inResource, tick);
        }

        public void RPCCreatureViolence(OnlinePhysicalObject? onlineVillain, int? hitchunkIndex, PhysicalObject.Appendage.Pos hitappendage, Vector2? directionAndMomentum, Creature.DamageType type, float damage, float stunbonus)
        {
            byte chunkIndex = (byte)(hitchunkIndex ?? 255);
            this.owner.InvokeRPC(this.CreatureViolence, onlineVillain, chunkIndex, hitappendage is null ? null : new AppendageRef(hitappendage), directionAndMomentum, type, damage, stunbonus);
        }

        [RPCMethod]
        public void CreatureViolence(OnlinePhysicalObject? onlineVillain, byte victimChunkIndex, AppendageRef? victimAppendageRef, Vector2? directionAndMomentum, Creature.DamageType damageType, float damage, float stunBonus)
        {
            if (this.realizedCreature is not Creature creature) return;

            BodyChunk? hitChunk = victimChunkIndex < 255 ? creature.bodyChunks[victimChunkIndex] : null;
            creature.Violence(onlineVillain?.apo.realizedObject.firstChunk, directionAndMomentum, hitChunk, victimAppendageRef?.GetAppendagePos(creature), damageType, damage, stunBonus);
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
            if (creature != null && creature.room != null)
            {
                try
                {
                    var room = creature.room;
                    creature.SuckedIntoShortCut(entrancePos, carriedByOther);
                    if (creature.graphicsModule != null)
                    {
                        Vector2 vector = room.MiddleOfTile(entrancePos) + Custom.IntVector2ToVector2(room.ShorcutEntranceHoleDirection(entrancePos)) * -5f;
                        creature.graphicsModule.SuckedIntoShortCut(vector);
                    }

                    // required since noramally objects are removed "imediatelly after"
                    // switching camera into a room with an object with obj.room = null crashes
                    List<AbstractPhysicalObject> allConnectedObjects = this.abstractCreature.GetAllConnectedObjects();
                    for (int i = 0; i < allConnectedObjects.Count; i++)
                    {
                        AbstractPhysicalObject obj = allConnectedObjects[i];
                        if (obj.realizedObject != null)
                        {
                            if (obj.realizedObject is Creature)
                            {
                                (obj.realizedObject as Creature).inShortcut = true;
                            }
                            room.RemoveObject(obj.realizedObject);
                            room.CleanOutObjectNotInThisRoom(obj.realizedObject); // very important
                        }
                    }
                }
                catch (Exception)
                {
                    enteringShortCut = false;
                    throw;
                }
            }
            enteringShortCut = false;
        }

        public override string ToString()
        {
            return $"{abstractCreature.creatureTemplate.type} {base.ToString()}";
        }
    }
}
