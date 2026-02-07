using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;

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
            //IL.OverWorld.InitiateSpecialWarp_WarpPoint += OverWorld_InitiateSpecialWarp_WarpPoint2;
            On.OverWorld.InitiateSpecialWarp_SingleRoom += OverWorld_InitiateSpecialWarp_SingleRoom;
            IL.OverWorld.Update += OverWorld_Update;
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
            IL.MirosBirdAbstractAI.Raid += MirosBirdAbstractAI_Raid; //miros birds dont need to do this
            On.Watcher.SandGrubGraphics.DrawSprites += SandGrubGraphics_DrawSprites;
            On.Watcher.BigSandGrubGraphics.DrawSprites += BigSandGrubGraphics_DrawSprites;
            On.Watcher.BigSandGrubNeck.Update += BigSandGrubNeck_Update;
            On.Watcher.BigSandGrubGraphics.UpdateSegments += BigSandGrubGraphics_UpdateSegments;
            On.Watcher.SandGrub.Collide += SandGrub_Collide;
            On.Watcher.SandGrub.UpdateTentacle += SandGrub_UpdateTentacle;

            new Hook(typeof(AbstractCreature).GetProperty("Quantify").GetGetMethod(), this.AbstractCreature_Quantify);
        }
        private void SandGrub_UpdateTentacle(On.Watcher.SandGrub.orig_UpdateTentacle orig, Watcher.SandGrub self)
        {
            try
            {
                orig(self);
            }
            catch (System.NullReferenceException e)
            {
                // TODO: Non-fatal
                //throw;
            }
        }

        private void BigSandGrubGraphics_UpdateSegments(On.Watcher.BigSandGrubGraphics.orig_UpdateSegments orig, Watcher.BigSandGrubGraphics self)
        {
            try
            {
                orig(self);
            }
            catch (System.NullReferenceException e)
            {
                // TODO: Non-fatal, occurs most likely due to unsynched plates
                //throw;
            }
        }

        private void BigSandGrubNeck_Update(On.Watcher.BigSandGrubNeck.orig_Update orig, Watcher.BigSandGrubNeck self)
        {
            try
            {
                orig(self);
            }
            catch (System.NullReferenceException e)
            {
                // TODO: Non-fatal, occurs most likely due to unsynched plates
                //throw;
            }
        }

        // This occurs because sandgrub is attempting to collide to an object not present on the client's end yet - and or
        // is attempting to grab a chunk that the entity does not posses or the sandgrub doesnt posses due to ID desyncs
        private void SandGrub_Collide(On.Watcher.SandGrub.orig_Collide orig, Watcher.SandGrub self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            try
            {
                orig(self, otherObject, myChunk, otherChunk);
            }
            catch (System.NullReferenceException e)
            {
                // TODO: Non-fatal, occurs most likely due to unsynched plates
                //throw;
            }
        }
        private void BigSandGrubGraphics_DrawSprites(On.Watcher.BigSandGrubGraphics.orig_DrawSprites orig, Watcher.BigSandGrubGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            try
            {
                orig(self, sLeaser, rCam, timeStacker, camPos);
            }
            catch (System.NullReferenceException e)
            {
                // TODO: Non-fatal, occurs most likely due to unsynched plates
                //throw;
            }
        }
        private void SandGrubGraphics_DrawSprites(On.Watcher.SandGrubGraphics.orig_DrawSprites orig, Watcher.SandGrubGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            try
            {
                orig(self, sLeaser, rCam, timeStacker, camPos);
            }
            catch (System.NullReferenceException e)
            {
                // TODO: Non-fatal, occurs most likely due to unsynched plates
                //throw;
            }
        }
        private void MirosBirdAbstractAI_Raid(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                ILLabel skip = null;
                c.GotoNext(moveType: MoveType.Before,
                    i => i.MatchLdloc(0),
                    i => i.MatchLdcI4(3),
                    i => i.MatchBge(out skip)
                );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((MirosBirdAbstractAI self) => OnlineManager.lobby == null || self.parent.IsLocal());
                c.Emit(OpCodes.Brfalse, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
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
                UnityEngine.Random.InitState(self.ID.RandomSeed);
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
            if (OnlineManager.lobby != null)
            {
                UnityEngine.Random.InitState(self.ID.RandomSeed);
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
            if (OnlineManager.lobby == null)
            {
                orig(self, ent);
                return;
            }

            var apo = ent as AbstractPhysicalObject;
            if (OnlineManager.lobby != null && apo is not null && !apo.CanMove()) return;
            
            ent.IsEnteringDen(ent.pos);
            self.entities.Remove(ent);
            if (ent is AbstractCreature)
            {
                self.creatures.Remove((AbstractCreature)ent);
            }

            bool isOwner = OnlineManager.lobby.isOwner;
            if (RoomSession.map.TryGetValue(self, out var rs) && rs.owner != null)
            {
                isOwner = rs.isOwner;
            }
            if (self.entitiesInDens.IndexOf(ent) == -1 && isOwner) // Stop duplicates
            {
                self.entitiesInDens.Add(ent);
            }

            if (OnlineManager.lobby != null && apo is not null)
            {
                self.world.GetResource().ApoEnteringWorld(apo);
                self.GetResource()?.ApoLeavingRoom(apo); // rs might not be registered yet
            }
        }



        // echo warps from the waher
        public void OverWorld_InitiateSpecialWarp_WarpPoint(On.OverWorld.orig_InitiateSpecialWarp_WarpPoint orig, OverWorld self, MoreSlugcats.ISpecialWarp callback, Watcher.WarpPoint.WarpPointData warpData, bool useNormalWarpLoader)
        {
            if (OnlineManager.lobby != null && isStoryMode(out var storyGameMode) && callback is Watcher.WarpPoint warpPoint)
            {
                var forcedWarp = storyGameMode.myLastWarp;
                if (forcedWarp != null)
                {
                    Debug("Forced warp found");
                    if (warpData != forcedWarp)
                    {
                        Debug("Replacing called Warp data with forced warp data");
                        warpData = forcedWarp;
                    }
                }
                /*if (callback.getSourceRoom() == null)
                {
                    self.warpData = storyGameMode.myLastWarp;
                    storyGameMode.lastWarpIsEcho = true;
                }*/
                orig(self, callback, warpData, useNormalWarpLoader);
                string sourceRoomName = warpPoint.getSourceRoom() == null ? "" : warpPoint.getSourceRoom().abstractRoom.name;
                RainMeadow.Debug($"doing warp point from {sourceRoomName}, data={warpData.ToString()}");
            }
            else
            {
                orig(self, callback, warpData, useNormalWarpLoader);
            }
            Debug($"active world's name? {self.activeWorld?.name ?? "NULL"}, warpPoint's region: {warpData.RegionString}");
        }

        public void OverWorld_InitiateSpecialWarp_WarpPoint2(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(moveType: MoveType.Before, //code that can be run by client safely
                    i => i.MatchLdarg(0),
                    i => i.MatchLdcI4(1),
                    i => i.MatchStfld<OverWorld>("worldLoadedFromWarp")
                );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate((OverWorld self, Watcher.WarpPoint.WarpPointData warpData) =>
                {
                    if (OnlineManager.lobby != null && isStoryMode(out var storyGameMode) && storyGameMode.lastWarpIsEcho)
                    {
                        RainMeadow.Debug($"LAST WARP ECHO OK ACK");
                        self.warpData = storyGameMode.myLastWarp; //OVERRIDE WARP DATA VERY IMPORTANT!
                        warpData = storyGameMode.myLastWarp;
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void OverWorld_InitiateSpecialWarp_SingleRoom(On.OverWorld.orig_InitiateSpecialWarp_SingleRoom orig, OverWorld self, MoreSlugcats.ISpecialWarp callback, string roomName)
        {
            if (OnlineManager.lobby != null)
            {
                if (isStoryMode(out var story))
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
                    else if (roomName == "SI_SAINTINTRO")
                    {
                        RainMeadow.Debug("Running Saint ending...");
                        if (OnlineManager.lobby.isOwner)
                        {
                            foreach (var player in OnlineManager.players)
                            {
                                if (!player.isMe)
                                {
                                    player.InvokeOnceRPC(StoryRPCs.GoToSaintEnding);
                                }
                            }
                        }
                        else if (RPCEvent.currentRPCEvent is null)
                        {
                            // tell host to move everyone else
                            OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToSaintEnding);
                        }
                        StoryRPCs.GoToSaintEnding(null);
                    }
                    else
                    {
                        orig(self, callback, roomName);
                    }
                }
            }
            else
            {
                orig(self, callback, roomName);
            }
        }

        private void WarpPoint_NewWorldLoaded_Room(On.Watcher.WarpPoint.orig_NewWorldLoaded_Room orig, Watcher.WarpPoint self, Room newRoom)
        {
            if (isStoryMode(out var storyGameMode))
            {
                var warpData = (self.overrideData ?? self.Data).ToString();
                string? sourceRoomName = self.getSourceRoom()?.abstractRoom.name;
                orig(self, newRoom);
                // remove uneeded item transportation between warps (makes dupes for no reason)
                // we should rather manually do fit ourselves, and remember to always identify APOs that traverse regions
                newRoom.game.GetStorySession.pendingWarpPointTransferObjects.Clear();
                newRoom.game.GetStorySession.importantWarpPointTransferedEntities.Clear();
                newRoom.game.GetStorySession.saveState.importantTransferEntitiesAfterWarpPointSave.Clear();
                WorldSession newWorldSession = newRoom.world.GetResource() ?? throw new KeyNotFoundException();
                RainMeadow.Debug($"new room loaded w={warpData}! forcing camera to {newRoom.abstractRoom.name}");

                for (int l = 0; l < newRoom.game.cameras.Length; l++)
                { // once again, force camera
                    newRoom.game.cameras[l].WarpMoveCameraActual(newRoom, -1);
                }
                var isEchoWarp = sourceRoomName == null ||newRoom.game.GetStorySession.spinningTopWarpsLeadingToRippleScreen.Contains(self.MyIdentifyingString()); //null room = loaded from first world as a host
                if (OnlineManager.lobby.isOwner && !isEchoWarp)
                {
                    foreach (var player in OnlineManager.players)
                    { // do nat throw everyone into the same room?
                        if (!player.isMe)
                        {
                            player.InvokeOnceRPC(StoryRPCs.NormalExecuteWatcherRiftWarp, sourceRoomName, warpData, false);
                        }
                    }
                }
            }
            else
            {
                orig(self, newRoom);
            }
        }
        private void OverWorld_Update(ILContext il)
        {
            try
            {
                ILCursor c = new(il);
                c.GotoNext(x => x.MatchCall<OverWorld>("WarpUpdate"));
                c.GotoPrev(MoveType.After, x => x.MatchLdfld<OverWorld>(nameof(OverWorld.readyForWarp)));
                c.GotoNext(x => x.MatchBrfalse(out _));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(delegate (bool origBool, OverWorld overWorld)
                {
                    if (OnlineManager.lobby != null)
                        //prevent readyForWarp to call WorldLoaded when warpWorldLoader is queued and Regions has not fully loaded, this is quick fix for now
                        return origBool && !overWorld.warpingPreload; 
                    return origBool;
                });
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }

        /// <summary>
        /// Used to control Host/Client resource racing in ArenaSitting_NextLevel
        /// </summary>
         private System.Collections.IEnumerator Overworld_Loaded_WaitLoop(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed, WorldSession oldWorldSession, WorldSession newWorldSession, World world)
        {
            float startTime = UnityEngine.Time.time;
            float timeoutSeconds = 5f;
            oldWorldSession.transitionInProgress = true;
            if (OnlineManager.lobby.isOwner) {
                while (oldWorldSession.participants.Count > 0 && (UnityEngine.Time.time - startTime < timeoutSeconds))
                {
                    yield return null; 
                }
        } else
            {
                 while (oldWorldSession.participants.Count > 0 && !newWorldSession.isAvailable && (UnityEngine.Time.time - startTime < timeoutSeconds))
                {
                    yield return null; 
                }  
            }
            // Check if we hit the timeout for logging purposes
            if (UnityEngine.Time.time - startTime >= timeoutSeconds)
            {
                Debug("WaitLoop timed out after 5 seconds. Proceeding anyway to prevent deadlock.");
            }
            else
            {
                Debug("Entities removed. Proceeding...");
            }
            oldWorldSession.transitionInProgress = false;
            self.WorldLoaded(warpUsed);
        }

        private void DeactivateAndWait(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed, bool isSameWorld, WorldSession oldWorldSession, WorldSession newWorldSession, World newWorld)
        {
            if (OnlineManager.lobby.gameMode is not MeadowGameMode) {
                if (!isSameWorld && oldWorldSession.isActive)
                { // there exists "warps" to the same world, twice, for some bloody reason
                    //this in fact probably is required for now because rain world devs DESPISE US
                    RainMeadow.Debug("Unsubscribing from old world");
                    oldWorldSession.Deactivate();
                    oldWorldSession.NotNeeded(); // done? let go
                }

                if (oldWorldSession.participants.Count > 0)
                {
                    if (OnlineManager.lobby.isOwner) {
                    Debug($"Waiting for {oldWorldSession.participants.Count} players to leave...");
                    } else
                    {
                        Debug($"Waiting for host  players to join new world...");
                    }
                    self.game.manager.rainWorld.StartCoroutine(Overworld_Loaded_WaitLoop(orig, self, warpUsed, oldWorldSession, newWorldSession, newWorld));
                    oldWorldSession.transitionInProgress = true;
                    return; 
                }
            }
        }
        // world transition at gatesactiveEntities
        private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed)
        {
            if (OnlineManager.lobby != null)
            {
                Debug($"warpWorldLoader is null: {self.warpWorldLoader == null}, worldLoader is null: {self.worldLoader == null}");
                World newWorld = (self.worldLoader == null) ? self.activeWorld : self.worldLoader.ReturnWorld();
                WorldSession oldWorldSession = self.activeWorld.GetResource() ?? throw new KeyNotFoundException();
                WorldSession newWorldSession = newWorld.GetResource() ?? throw new KeyNotFoundException();
                bool isSameWorld = (self.activeWorld.name == newWorld.name);
                bool isEchoWarp = (self.game.GetStorySession.saveState.warpPointTargetAfterWarpPointSave != null);
                bool isFirstWarpWorld = false;

                if (oldWorldSession.transitionInProgress)
                {
                    return;
                }

                if (self.reportBackToGate != null && RoomSession.map.TryGetValue(self.reportBackToGate.room.abstractRoom, out var roomSession))
                {
                    // Regular gate switch
                    AbstractRoom oldAbsroom = self.reportBackToGate.room.abstractRoom;
                    AbstractRoom newAbsroom = newWorld.GetAbstractRoom(oldAbsroom.name);
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
                                    opo.RemoveEntityFromGame(false);
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

                        DeactivateAndWait(orig, self, warpUsed, isSameWorld, oldWorldSession, newWorldSession, newWorld);

                }
                else if (warpUsed)
                {
                    RainMeadow.Debug("getting warp point!");
                    //somehow WorldLoaded() gets called more than once in a row and leads to this exception when spawning from echo warp. No big issue tho..
                    Watcher.WarpPoint warpPoint = self.specialWarpCallback as Watcher.WarpPoint ?? throw new InvalidProgrammerException("watcher warp point doesnt exist at time of loading");

                    Watcher.WarpPoint.WarpPointData warpData = warpPoint.overrideData ?? warpPoint.Data;

                    Room room = warpPoint.room; //may be null in the case a client activates an echo warp
                    isFirstWarpWorld = room == null; //do not update gate status afterwards :)

                    if (isEchoWarp || isFirstWarpWorld || warpData.rippleWarp)
                    { //echo activation is special edge case
                        if (room == null) RainMeadow.Error("warp point with a null room");
                        RainMeadow.Debug("this an echo warp");
                        /*if (isStoryMode(out var storyGameMode))
                        {
                            self.warpData = storyGameMode.myLastWarp; //OVERRIDE WARP DATA VERY IMPORTANT!
                        }*/
                    }
                    // We delete every single entitity in the old world, every single one, even our
                    // slugcats are deleted, nothing is spared, this is because if we dont do this
                    // someone will keep requesting for the creatures on the old world
                    else if (RoomSession.map.TryGetValue(room.abstractRoom, out var roomSession3))
                    {
                        RainMeadow.Debug($"warp continous region switching -- clear session");
                        var entities = room.abstractRoom.entities;
                        for (int i = entities.Count - 1; i >= 0; i--)
                        {
                            if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                            {
                                oe.apo.LoseAllStuckObjects();
                                if (!oe.isMine)
                                {
                                    // not-online-aware removal
                                    oe.RemoveEntityFromGame(false);
                                }
                                else // mine leave the old online world elegantly
                                {
                                    RainMeadow.Debug("removing my entity from online " + oe);
                                    oe.ExitResource(roomSession3);
                                    oe.ExitResource(roomSession3.worldSession);
                                }
                            }
                        }
                    }
                    Debug($"destination region: {warpData.destRegion}, worldLoader is null? {self.worldLoader == null}, worldLoader's World is null? {self.worldLoader == null || self.worldLoader.world == null}, activeWorld is null? {self.activeWorld == null}");
                    Debug($"Watcher warp switchery APOs preparations from {self.activeWorld.name} to {(self.worldLoader == null ? newWorld.name : ($"{self.worldLoader.worldName}/{newWorld.name}"))}");
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                    { //it will move places
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.apo is AbstractCreature ac)
                        {
                            opo1.beingMoved = true;
                        }
                    }
                    orig(self, warpUsed);
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                    { //no longer moves places
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.apo is AbstractCreature ac)
                        {
                            opo1.beingMoved = false;
                        }
                    }
                    RainMeadow.Debug($"Watcher warp switchery post");

                        DeactivateAndWait(orig, self, warpUsed, isSameWorld, oldWorldSession, newWorldSession, newWorld);
                }
                else
                {
                    // special warp, don't bother with room items
                    orig(self, warpUsed);

                    DeactivateAndWait(orig, self, warpUsed, isSameWorld, oldWorldSession, newWorldSession, newWorld);

                }

                if (warpUsed)
                { // and for warps we require a more manual approach; to properly make aware of old APOs entering a new region
                    foreach (var absplayer in self.game.Players)
                    {
                        if (absplayer.realizedCreature is Player player)
                        {
                            RainMeadow.Debug($"fixing player");
                            // do not get stuck on bottom left
                            if (self.warpData?.destPos != null)
                            {
                                player.abstractCreature.pos.Tile = new RWCustom.IntVector2((int)(self.warpData.destPos.Value.x / 20f), (int)(self.warpData.destPos.Value.y / 20f));
                            }
                            player.slugOnBack?.DropSlug();
                            if (player.objectInStomach is AbstractPhysicalObject apo)
                            { // apo's in stomach (isn't realized but has to be "carried" over)
                                newWorldSession.ApoEnteringWorld(apo);
                            }
                            for (int k = 0; k < player.grasps.Length; k++)
                            { // grasped objects (i.e toys from WAUA_TOYS)
                                if (player.grasps[k] != null && player.grasps[k].grabbed != null)
                                {
                                    newWorldSession.ApoEnteringWorld(player.grasps[k].grabbed.abstractPhysicalObject);
                                }
                            }
                        }
                    }
                }
                else
                { //normal code for gates
                    foreach (var absplayer in self.game.Players)
                    {
                        if (absplayer.realizedCreature is Player player && player.objectInStomach is AbstractPhysicalObject apo)
                        {
                            newWorldSession.ApoEnteringWorld(apo);
                        }
                    }
                }



                if (OnlineManager.lobby.gameMode is StoryGameMode storyGameMode)
                {
                    if (OnlineManager.lobby.isOwner && !isFirstWarpWorld)
                    {
                        storyGameMode.changedRegions = true;
                        storyGameMode.readyForTransition = StoryGameMode.ReadyForTransition.Crossed;
                    }
                    if (storyGameMode.myLastWarp != null)
                    {
                        if (self.specialWarpPointGoal != null && self.specialWarpPointGoal != storyGameMode.myLastWarp)
                            Debug("We loaded from a warp different of our forced warp!");
                        storyGameMode.myLastWarp = null;
                    }
                }

                

                if (OnlineManager.lobby.gameMode is MeadowGameMode)
                {
                    MeadowMusic.NewWorld(self.activeWorld);
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
