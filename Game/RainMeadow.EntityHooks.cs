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
            On.OverWorld.InitiateSpecialWarp_WarpPoint += OverWorld_InitiateSpecialWarp_WarpPoint;
            On.OverWorld.InitiateSpecialWarp_SingleRoom += OverWorld_InitiateSpecialWarp_SingleRoom;

            On.Watcher.WarpPoint.Update += Watcher_WarpPoint_Update;
            On.Watcher.PrinceBehavior.InitateConversation += Watcher_PrinceBehavior_InitateConversation;

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

        public void Watcher_WarpPoint_Update(On.Watcher.WarpPoint.orig_Update orig, Watcher.WarpPoint self, bool eu)
        {
            if (OnlineManager.lobby != null && !OnlineManager.lobby.isOwner)
            {
                self.triggerTime = 0; // clients cant activate or update warp points
                self.lastTriggerTime = 0;
            }
            orig(self, eu); // either host or singleplayer
        }

        public void Watcher_PrinceBehavior_InitateConversation(On.Watcher.PrinceBehavior.orig_InitateConversation orig, Watcher.PrinceBehavior self)
        {
            orig(self);
            if (OnlineManager.lobby != null)
            {
                int newValue = self.prince.room.game.GetStorySession.saveState.miscWorldSaveData.highestPrinceConversationSeen;
                RainMeadow.Debug("prince yap acknowledged");
                if (OnlineManager.lobby.isOwner)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeOnceRPC(StoryRPCs.PrinceSetHighestConversation, newValue);
                        }
                    }
                }
                else if (RPCEvent.currentRPCEvent is null)
                {
                    // tell host to move everyone else
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.PrinceSetHighestConversation, newValue);
                }
                StoryRPCs.PrinceSetHighestConversation(null, newValue);
            }
        }

        // echo warps from the waher
        public void OverWorld_InitiateSpecialWarp_WarpPoint(On.OverWorld.orig_InitiateSpecialWarp_WarpPoint orig, OverWorld self, MoreSlugcats.ISpecialWarp callback, Watcher.WarpPoint.WarpPointData warpData, bool useNormalWarpLoader)
        {
            if (isStoryMode(out var _))
            {
                if (OnlineManager.lobby != null)
                {
                    if (OnlineManager.lobby.isOwner)
                    {
                        RainMeadow.Debug("warp-initiate: first send rpc");
                        foreach (var player in OnlineManager.players)
                        {
                            if (!player.isMe)
                            {
                                player.InvokeOnceRPC(StoryRPCs.PerformWatcherRiftWarp, callback.getSourceRoom().abstractRoom.name, warpData.ToString(), useNormalWarpLoader);
                            }
                        }
                        if (callback != null)
                        {
                            // throw everyone into the same room
                            for (int j = 0; j < self.game.Players.Count; j++)
                            {
                                if (self.game.Players[j].realizedCreature != null)
                                {
                                    self.game.Players[j].realizedCreature.PlaceInRoom(callback.getSourceRoom());
                                }
                            }
                        }
                    }
                    RainMeadow.Debug("warp-initiate: second phase execute");
                }
                orig(self, callback, warpData, useNormalWarpLoader);
            }
        }

        public void OverWorld_InitiateSpecialWarp_SingleRoom(On.OverWorld.orig_InitiateSpecialWarp_SingleRoom orig, OverWorld self, MoreSlugcats.ISpecialWarp callback, string roomName)
        {
            if (OnlineManager.lobby != null)
            {
                if (isStoryMode(out var _))
                {
                    if (roomName == "MS_COMMS")
                    {
                        if (OnlineManager.lobby.isOwner)
                        {
                            foreach (var player in OnlineManager.players)
                            {
                                if (!player.isMe)
                                {
                                    player.InvokeOnceRPC(StoryRPCs.GoToRivuletEnding);
                                }
                            }
                        }
                        else if (RPCEvent.currentRPCEvent is null)
                        {
                            // tell host to move everyone else
                            OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToRivuletEnding);
                        }
                        StoryRPCs.GoToRivuletEnding(null);
                    }
                    else if (roomName == "SI_A07")
                    {
                        if (OnlineManager.lobby.isOwner)
                        {
                            foreach (var player in OnlineManager.players)
                            {
                                if (!player.isMe)
                                {
                                    player.InvokeOnceRPC(StoryRPCs.GoToSpearmasterEnding);
                                }
                            }
                        }
                        else if (RPCEvent.currentRPCEvent is null)
                        {
                            // tell host to move everyone else
                            OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToSpearmasterEnding);
                        }
                        StoryRPCs.GoToSpearmasterEnding(null);
                    }
                }
                // do nothinf
                RainMeadow.Debug("initiate special warp: RIVULET DOES NOTHINF");
            }
            else
            {
                orig(self, callback, roomName);
            }
        }

        // world transition at gates
        private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed)
        {
            if (OnlineManager.lobby != null)
            {
                WorldSession oldWorldSession = self.activeWorld.GetResource() ?? throw new KeyNotFoundException();
                WorldSession newWorldSession = self.worldLoader.world.GetResource() ?? throw new KeyNotFoundException();
                bool isSameWorld = (self.activeWorld == self.worldLoader.world);
                
                if (self.reportBackToGate != null && RoomSession.map.TryGetValue(self.reportBackToGate.room.abstractRoom, out var roomSession))
                {
                    // Regular gate switch
                    AbstractRoom oldAbsroom = self.reportBackToGate.room.abstractRoom;
                    AbstractRoom newAbsroom = self.worldLoader.world.GetAbstractRoom(oldAbsroom.name);
                    List<AbstractWorldEntity> entitiesFromNewRoom = newAbsroom.entities; // these get ovewritten and need handling
                    List<AbstractCreature> creaturesFromNewRoom = newAbsroom.creatures;

                    // pre: remove remote entities
                    // we go over all APOs in the room
                    Debug("Gate switchery 1");
                    Room room = self.reportBackToGate.room;
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

                    orig(self, warpUsed); // this replace the list of entities in new world with that from old world

                    // post: we add our entities to the new world
                    if (room != null && RoomSession.map.TryGetValue(room.abstractRoom, out var roomSession2))
                    {
                        room.abstractRoom.entities.AddRange(entitiesFromNewRoom); // re-add overwritten entities
                        room.abstractRoom.creatures.AddRange(creaturesFromNewRoom);
                        roomSession2.Activate();
                    }
                }
                else if (warpUsed)
                {
                    if (self.specialWarpCallback is Watcher.WarpPoint warpPoint)
                    {
                        var room = warpPoint.room; //this shall never be null
                        if (RoomSession.map.TryGetValue(room.abstractRoom, out var roomSession3))
                        {
                            Debug("Next level switching");
                            var entities = room.abstractRoom.entities;
                            for (int i = entities.Count - 1; i >= 0; i--)
                            {
                                if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                                {
                                    oe.apo.LoseAllStuckObjects();
                                    if (!oe.isMine)
                                    {
                                        // not-online-aware removal
                                        Debug("removing remote entity from game " + oe);
                                        oe.beingMoved = true;
                                        if (oe.apo.realizedObject is Creature c && c.inShortcut)
                                        {
                                            if (c.RemoveFromShortcuts()) c.inShortcut = false;
                                        }
                                        entities.Remove(oe.apo);
                                        room.abstractRoom.creatures.Remove(oe.apo as AbstractCreature);
                                        room.RemoveObject(oe.apo.realizedObject);
                                        room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                                        oe.beingMoved = false;
                                    }
                                    else // mine leave the old online world elegantly
                                    {
                                        Debug("removing my entity from online " + oe);
                                        oe.ExitResource(roomSession3);
                                        oe.ExitResource(roomSession3.worldSession);
                                    }
                                }
                            }
                        }
                        Debug("Watcher warp switchery 1");
                        // THIS MAY BE A BAD IDEA; but we can't let the world loader delete our players
                        orig(self, warpUsed);

                        self.game.cameras[0].WarpMoveCameraPrecast((warpPoint.overrideData != null) ? warpPoint.overrideData.destRoom : warpPoint.Data.destRoom, (warpPoint.overrideData != null) ? warpPoint.overrideData.destCam : warpPoint.Data.destCam);
                    }
                    else
                    {
                        RainMeadow.Error("watcher warp point doesnt exist at time of loading");
                    }
                }
                else
                {
                    // special warp, don't bother with room items
                    orig(self, warpUsed);
                }

                foreach (var absplayer in self.game.Players)
                {
                    if (absplayer.realizedCreature is Player player && player.objectInStomach is AbstractPhysicalObject apo)
                    {
                        newWorldSession.ApoEnteringWorld(apo);
                    }
                }

                // "warps" to the same world, twice, for some bloody reason
                if (!isSameWorld)
                {
                    oldWorldSession.Deactivate();
                    oldWorldSession.NotNeeded(); // done? let go
                }

                if (OnlineManager.lobby.gameMode is StoryGameMode storyGameMode && OnlineManager.lobby.isOwner)
                {
                    storyGameMode.changedRegions = true;
                    storyGameMode.readyForGate = StoryGameMode.ReadyForGate.Crossed;
                    if (warpUsed)
                    {
                        storyGameMode.changedRegions = false;
                        storyGameMode.readyForGate = StoryGameMode.ReadyForGate.Closed;
                    }
                }
                if (OnlineManager.lobby.gameMode is MeadowGameMode)
                {
                    MeadowMusic.NewWorld(self.activeWorld);
                }

                // force clients to warp AFTER the host
                if (OnlineManager.lobby.isOwner && warpUsed)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeOnceRPC(StoryRPCs.ForceWatcherWarpOnClient);
                        }
                    }
                }
            }
            else
            {
                orig(self, warpUsed);
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
