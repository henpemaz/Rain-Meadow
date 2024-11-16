using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        // Track entities joining/leaving resources
        // customization stuff reused some hooks
        private void EntityHooks()
        {
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded; // creature moving between WORLDS

            On.AbstractRoom.MoveEntityToDen += AbstractRoom_MoveEntityToDen; // maybe leaving room, maybe entering world
            On.AbstractWorldEntity.Destroy += AbstractWorldEntity_Destroy; // creature moving between rooms
            On.AbstractRoom.RemoveEntity_AbstractWorldEntity += AbstractRoom_RemoveEntity; // creature moving between rooms
            On.AbstractRoom.AddEntity += AbstractRoom_AddEntity; // creature moving between rooms

            On.AbstractPhysicalObject.ChangeRooms += AbstractPhysicalObject_ChangeRooms;
            On.AbstractCreature.ChangeRooms += AbstractCreature_ChangeRooms1;

            On.AbstractCreature.Abstractize += AbstractCreature_Abstractize; // get real
            On.AbstractPhysicalObject.Abstractize += AbstractPhysicalObject_Abstractize; // get real
            On.AbstractCreature.Realize += AbstractCreature_Realize; // get real, also customization happens here
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize; // get real

            On.AbstractCreature.Move += AbstractCreature_Move; // I'm watching your every step
            On.AbstractPhysicalObject.Move += AbstractPhysicalObject_Move; // I'm watching your every step

            IL.AbstractCreature.IsExitingDen += AbstractCreature_IsExitingDen;

            new Hook(typeof(AbstractCreature).GetProperty("Quantify").GetGetMethod(), this.AbstractCreature_Quantify);
        }

        private bool AbstractCreature_Quantify(Func<AbstractCreature, bool> orig, AbstractCreature self)
        {
            if (!self.IsLocal()) return false; // do not attempt to delete remote creatures
            return orig(self);
        }

        private void AbstractCreature_ChangeRooms1(On.AbstractCreature.orig_ChangeRooms orig, AbstractCreature self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && !self.CanMove()) return;
            orig(self, newCoord);
        }

        private void AbstractPhysicalObject_ChangeRooms(On.AbstractPhysicalObject.orig_ChangeRooms orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && !self.CanMove(newCoord)) return;
            orig(self, newCoord);
        }

        // I'm watching your every step
        // remotes that aren't being moved can only move if going into the right roomSession
        private void AbstractPhysicalObject_Move(On.AbstractPhysicalObject.orig_Move orig, AbstractPhysicalObject self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && !self.CanMove(newCoord)) return;
            var oldCoord = self.pos;
            orig(self, newCoord);
            if (OnlineManager.lobby != null && oldCoord.room != newCoord.room)
            {
                // leaving room is handled in absroom.removeentity
                // adding to room is handled here so the position is updated properly
                self.world.GetResource().ApoEnteringWorld(self);
                self.world.GetAbstractRoom(newCoord.room).GetResource()?.ApoEnteringRoom(self, newCoord);
            }
        }

        // I'm watching your every step
        private void AbstractCreature_Move(On.AbstractCreature.orig_Move orig, AbstractCreature self, WorldCoordinate newCoord)
        {
            if (OnlineManager.lobby != null && !self.CanMove(newCoord)) return;
            orig(self, newCoord);
        }

        private void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (OnlineManager.lobby != null)
            {
                UnityEngine.Random.seed = self.ID.RandomSeed;
            }
            orig(self);
            if (OnlineManager.lobby != null && self.GetOnlineObject(out var oe))
            {
                if (self.type == AbstractPhysicalObject.AbstractObjectType.Oracle)
                {
                    // apo.realize doesn't handle oracles
                    self.realizedObject = new Oracle(self, self.Room.realizedRoom);
                }
                RainMeadow.Debug(self.type);
                if (!oe.isMine && !oe.realized && oe.isTransferable && !oe.isPending)
                {
                    if (oe.roomSession == null || !oe.roomSession.participants.Contains(oe.owner)) //if owner of oe is subscribed (is participant) do not request
                    {
                        oe.Request();
                    }
                }
                if (oe.isMine)
                {
                    oe.realized = true;
                }
            }
        }

        // get real, and customize
        private void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
        {
            if(OnlineManager.lobby != null)
            {
                UnityEngine.Random.seed = self.ID.RandomSeed;
            }
            var wasCreature = self.realizedCreature;
            orig(self);
            if (OnlineManager.lobby != null && self.GetOnlineObject(out var oe))
            {
                if (!oe.isMine && !oe.realized && oe.isTransferable && !oe.isPending)
                {
                    if (oe.roomSession == null || !oe.roomSession.participants.Contains(oe.owner)) //if owner of oe is subscribed (is participant) do not request
                    {
                        oe.Request();
                    }
                }
                if (oe.isMine)
                {
                    oe.realized = self.realizedObject != null;
                }
                if (self.realizedCreature != null && self.realizedCreature != wasCreature && oe is OnlineCreature oc)
                {
                    OnlineManager.lobby.gameMode.Customize(self.realizedCreature, oc);
                }
            }
        }

        // get real
        private void AbstractPhysicalObject_Abstractize(On.AbstractPhysicalObject.orig_Abstractize orig, AbstractPhysicalObject self, WorldCoordinate coord)
        {
            if (OnlineManager.lobby != null && !self.CanMove()) return;
            orig(self, coord);
            if (OnlineManager.lobby != null && self.GetOnlineObject(out var oe) && oe.isMine)
            {
                if (oe.realized && oe.isTransferable && !oe.isPending) oe.Release();
                oe.realized = false;
            }
        }

        // get real
        private void AbstractCreature_Abstractize(On.AbstractCreature.orig_Abstractize orig, AbstractCreature self, WorldCoordinate coord)
        {
            if (OnlineManager.lobby != null && !self.CanMove()) return;
            orig(self, coord);
            if (OnlineManager.lobby != null && self.GetOnlineObject(out var oe) && oe.isMine)
            {
                if (oe.realized && oe.isTransferable && !oe.isPending) oe.Release();
                oe.realized = false;
            }
        }


        // not the main entry-point for room entities moving around
        // apo.move doesn't set the new pos until after it has moved, that's the issue
        // this is only for things that are ADDED directly to the room
        private void AbstractRoom_AddEntity(On.AbstractRoom.orig_AddEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            var apo = ent as AbstractPhysicalObject;
            if (OnlineManager.lobby != null && apo is not null && !apo.CanMove()) return;
            orig(self, ent);
            if (OnlineManager.lobby != null && apo is not null && apo.pos.room == self.index) // skips apos being apo.Move'd
            {
                self.world.GetResource().ApoEnteringWorld(apo);
                self.GetResource()?.ApoEnteringRoom(apo, apo.pos);
            }
        }

        // called from several places, thus handled here rather than in apo.move
        private void AbstractRoom_RemoveEntity(On.AbstractRoom.orig_RemoveEntity_AbstractWorldEntity orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            var apo = ent as AbstractPhysicalObject;
            if (OnlineManager.lobby != null && apo is not null && !apo.CanMove()) return;
            orig(self, ent);
            if (OnlineManager.lobby != null && apo is not null)
            {
                self.GetResource()?.ApoLeavingRoom(apo);
            }
        }

        private void AbstractWorldEntity_Destroy(On.AbstractWorldEntity.orig_Destroy orig, AbstractWorldEntity self)
        {
            var apo = self as AbstractPhysicalObject;
            if (OnlineManager.lobby != null && apo is not null && !apo.CanMove()) return;
            orig(self);
            if (OnlineManager.lobby != null && apo is not null)
            {
                self.Room.GetResource()?.ApoLeavingRoom(apo);
                self.world.GetResource().ApoLeavingWorld(apo);
            }
        }

        // maybe leaving room, maybe entering world
        private void AbstractRoom_MoveEntityToDen(On.AbstractRoom.orig_MoveEntityToDen orig, AbstractRoom self, AbstractWorldEntity ent)
        {
            var apo = ent as AbstractPhysicalObject;
            if (OnlineManager.lobby != null && apo is not null && !apo.CanMove()) return;
            orig(self, ent);
            if (OnlineManager.lobby != null && apo is not null)
            {
                self.world.GetResource().ApoEnteringWorld(apo);
                self.GetResource()?.ApoLeavingRoom(apo); // rs might not be registered yet
            }
        }

        // world transition at gates
        private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            if (OnlineManager.lobby != null)
            {
                AbstractRoom oldAbsroom = self.reportBackToGate.room.abstractRoom;
                AbstractRoom newAbsroom = self.worldLoader.world.GetAbstractRoom(oldAbsroom.name);
                WorldSession oldWorldSession = WorldSession.map.GetValue(oldAbsroom.world, (w) => throw new KeyNotFoundException());
                WorldSession newWorldSession = WorldSession.map.GetValue(newAbsroom.world, (w) => throw new KeyNotFoundException());
                List<AbstractWorldEntity> entitiesFromNewRoom = newAbsroom.entities; // these get ovewritten and need handling
                List<AbstractCreature> creaturesFromNewRoom = newAbsroom.creatures;

                Room room = null;

                // Regular gate switch
                // pre: remove remote entities
                if (self.reportBackToGate != null && RoomSession.map.TryGetValue(self.reportBackToGate.room.abstractRoom, out var roomSession))
                {
                    // we go over all APOs in the room
                    Debug("Gate switchery 1");
                    room = self.reportBackToGate.room;
                    var entities = room.abstractRoom.entities;
                    for (int i = entities.Count - 1; i >= 0; i--)
                    {
                        if (entities[i] is AbstractPhysicalObject apo && apo.GetOnlineObject(out var opo))
                        {
                            // if they're not ours, they need to be removed from the room SO THE GAME DOESN'T MOVE THEM
                            // if they're the overseer and it isn't the host moving it, that's bad as well
                            if (!opo.isMine || (apo is AbstractCreature ac && ac.creatureTemplate.type == CreatureTemplate.Type.Overseer && !newWorldSession.isOwner))
                            {
                                // not-online-aware removal
                                Debug("removing remote entity from game " + opo);
                                opo.beingMoved = true;
                                if (apo.realizedObject is Creature c && c.inShortcut)
                                {
                                    c.RemoveFromShortcuts();
                                }
                                entities.Remove(apo);
                                room.abstractRoom.creatures.Remove(apo as AbstractCreature);
                                room.RemoveObject(apo.realizedObject);
                                room.CleanOutObjectNotInThisRoom(apo.realizedObject);
                                opo.beingMoved = false;
                            }
                        }
                    }
                }

                orig(self); // this replace the list of entities in new world with that from old world

                // post: we add our entities to the new world
                if (room != null && RoomSession.map.TryGetValue(room.abstractRoom, out var roomSession2))
                {
                    room.abstractRoom.entities.AddRange(entitiesFromNewRoom); // re-add overwritten entities
                    room.abstractRoom.creatures.AddRange(creaturesFromNewRoom);
                    roomSession2.Activate();

                    foreach (var absplayer in self.game.Players)
                    {
                        if (absplayer.realizedCreature is Player player && player.objectInStomach is AbstractPhysicalObject apo)
                        {
                            newWorldSession.ApoEnteringWorld(apo);
                        }
                    }

                    oldWorldSession.Deactivate();
                    oldWorldSession.NotNeeded(); // done? let go
                }
                if (OnlineManager.lobby.gameMode is StoryGameMode storyGameMode)
                {
                    storyGameMode.changedRegions = true;
                    storyGameMode.readyForGate = 2;
                }
            }
            else
            {
                orig(self);
            }
        }

        private void AbstractCreature_IsExitingDen(ILContext il)
        {
            try
            {
                // if pos not NodeDefined (means is stomach object) then RealizeInRoom
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                ILLabel end = null;
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt<AbstractWorldEntity>("get_Room"),
                    i => i.MatchLdfld<AbstractRoom>("realizedRoom"),
                    i => i.MatchBrfalse(out end)
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((AbstractCreature ac) => OnlineManager.lobby != null && !ac.pos.NodeDefined && ac.GetOnlineObject(out _));
                c.Emit(OpCodes.Brfalse, skip);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<AbstractCreature>(OpCodes.Callvirt, "RealizeInRoom");
                c.Emit(OpCodes.Br, end);
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}
