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
            IL.OverWorld.InitiateSpecialWarp_WarpPoint += OverWorld_InitiateSpecialWarp_WarpPoint2;
            On.OverWorld.InitiateSpecialWarp_SingleRoom += OverWorld_InitiateSpecialWarp_SingleRoom;

            On.Watcher.WarpPoint.NewWorldLoaded_Room += WarpPoint_NewWorldLoaded_Room; // creature moving between WORLDS
            On.Watcher.WarpPoint.Update += Watcher_WarpPoint_Update;
            On.Watcher.WarpPoint.PerformWarp += WarpPoint_PerformWarp;
            On.Watcher.PrinceBehavior.InitateConversation += Watcher_PrinceBehavior_InitateConversation;
            IL.Watcher.Barnacle.LoseShell += Watcher_Barnacle_LoseShell;
            On.Watcher.SpinningTop.SpawnWarpPoint += SpinningTop_SpawnWarpPoint;
            On.Watcher.SpinningTop.RaiseRippleLevel += SpinningTop_RaiseRippleLevel;
            On.Watcher.SpinningTop.Update += SpinningTop_Update;
            On.Watcher.SpinningTop.VanillaRegionSpinningTopEncounter += SpinningTop_VanillaRegionSpinningTopEncounter;

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

            On.HUD.FoodMeter.GameUpdate += (On.HUD.FoodMeter.orig_GameUpdate orig, HUD.FoodMeter self) =>
            {
                try
                {
                    orig(self);
                }
                catch (System.NullReferenceException e)
                {
                    // TODO: this happens because watcher is evil
                    // and we are evil too
                    //RainMeadow.Debug($"bad thing with hud {e}");
                }
            };
            On.Watcher.CamoMeter.Update += (On.Watcher.CamoMeter.orig_Update orig, Watcher.CamoMeter self) =>
            {
                try
                {
                    orig(self);
                }
                catch (System.NullReferenceException e)
                {
                    // TODO: this happens because watcher is evil
                    // and we are evil too
                    //RainMeadow.Debug($"bad thing with hud {e}");
                }
            };

            IL.AbstractCreature.IsExitingDen += AbstractCreature_IsExitingDen;
            IL.MirosBirdAbstractAI.Raid += MirosBirdAbstractAI_Raid; //miros birds dont need to do this

            new Hook(typeof(AbstractCreature).GetProperty("Quantify").GetGetMethod(), this.AbstractCreature_Quantify);
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
            if (isStoryMode(out var storyGameMode))
            {
                bool readyForWarp = storyGameMode.readyForTransition != StoryGameMode.ReadyForTransition.Closed;
                if (OnlineManager.lobby.isOwner && OnlineManager.lobby.clientSettings.Values.Where(cs => cs.inGame) is var inGameClients && inGameClients.Any())
                {
                    var inGameClientsData = inGameClients.Select(cs => cs.GetData<StoryClientSettingsData>());
                    var inGameAvatarOPOs = inGameClients.SelectMany(cs => cs.avatars.Select(id => id.FindEntity(true))).OfType<OnlinePhysicalObject>();
                    var rooms = inGameAvatarOPOs.Select(opo => opo.apo.pos.room);
                    var wasOneWay = (self.overrideData != null) ? self.overrideData.wasOneWay : self.Data.wasOneWay;
                    // Can't warp to warp points with null rooms (echo warps)
                    // remember that echo warps are one way only, so we will NOT gate thru them
                    // so please do not pretend it's a gate, and no requirements can be met, thanks :)
                    // and ensure theyre in the same room as the warp point itself :)
                    if (rooms.Distinct().Count() == 1 && !wasOneWay && self.room != null && inGameAvatarOPOs.First().apo.Room == self.room.abstractRoom)
                    { // make sure they're at the same room
                        RainWorld.roomIndexToName.TryGetValue(rooms.First(), out var gateRoom);
                        RainMeadow.Debug($"ready for warp {gateRoom}!");
                        storyGameMode.readyForTransition = StoryGameMode.ReadyForTransition.MeetRequirement;
                        readyForWarp = true;
                    }
                    else
                    {
                        storyGameMode.readyForTransition = StoryGameMode.ReadyForTransition.Closed;
                        readyForWarp = false;
                    }
                }
                if (!OnlineManager.lobby.isOwner || !readyForWarp)
                { // clients cant activate or update warp points unless it is an echo
                    self.triggerTime = 0;
                    self.lastTriggerTime = 0;
                }
            }
            orig(self, eu); // either host or singleplayer
        }

        public void Watcher_Barnacle_LoseShell(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                ILLabel spawnRocks = null;
                c.GotoNext(moveType: MoveType.Before, //code that can be run by client safely
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchBrtrue(out spawnRocks),
                    i => i.MatchRet()
                );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Watcher.Barnacle self) => OnlineManager.lobby == null || (self.abstractCreature is AbstractPhysicalObject apo && apo.GetOnlineObject(out var opo) && opo.isMine));
                c.Emit(OpCodes.Brtrue, spawnRocks); //only may the object owner (or singleplayer) add rocks for a barnacle
                c.Emit(OpCodes.Ret);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void SpinningTop_SpawnWarpPoint(On.Watcher.SpinningTop.orig_SpawnWarpPoint orig, Watcher.SpinningTop self)
        {
            if (OnlineManager.lobby != null)
            {
                RainMeadow.Debug("spawning warp point from echo");
                PlacedObject placedObject = new(PlacedObject.Type.WarpPoint, null);
                SpinningTopData specialData = self.SpecialData;
                // setup data
                if (specialData != null && placedObject.data is Watcher.WarpPoint.WarpPointData warpData)
                {
                    warpData.destPos = specialData.destPos;
                    warpData.RegionString = specialData.RegionString;
                    warpData.destRoom = specialData.destRoom;
                    warpData.destTimeline = specialData.destTimeline;
                    warpData.panelPos = specialData.panelPos;
                    warpData.deathPersistentWarpPoint = true;
                    warpData.rippleWarp = specialData.rippleWarp;
                    // TODO: All warps by echos are one-way for now this is because we can't reliably obtain the
                    // "source room" of those (see Overworld_WorldLoaded); this leads to warps that warp to the
                    // same region and room which WILL cause issues - so to remediate that we simply pretend all
                    // warps made by echoes are one way only.
                    warpData.oneWay = true; //(specialData.rippleWarp || Region.IsWatcherVanillaRegion(self.room.world.name) || Region.IsVanillaSentientRotRegion(self.room.world.name));
                    if (self.room.game.IsStorySession)
                    {
                        warpData.cycleSpawnedOn = self.room.game.GetStorySession.saveState.cycleNumber;
                    }
                    warpData.destCam = Watcher.WarpPoint.GetDestCam(warpData);
                    placedObject.data = warpData;
                    placedObject.pos = self.pos;
                    Watcher.WarpPoint warpPoint = self.room.TrySpawnWarpPoint(placedObject, false, true, true);
                    if (warpPoint != null)
                    {
                        if (self.CanRaiseRippleLevel() || self.vanillaToRippleEncounter)
                        {
                            self.room.game.GetStorySession.spinningTopWarpsLeadingToRippleScreen.Add(warpPoint.MyIdentifyingString());
                        }
                        warpPoint.WarpPrecast(); // force cast NOW
                        if (OnlineManager.lobby.isOwner)
                        {
                            StoryRPCs.EchoExecuteWatcherRiftWarp(null, self.room.abstractRoom.name, warpData.ToString()); // maybe just call orig here instead
                        }
                        else
                        {
                            OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.EchoExecuteWatcherRiftWarp, self.room.abstractRoom.name, warpData.ToString()); //tell owner to perform rift for everyone, only IF its an echo
                        }
                        if (!specialData.rippleWarp)
                        {
                            warpPoint.triggerTime = (int)(warpPoint.triggerActivationTime - 1f);
                            warpPoint.strongPull = true;
                        }
                    }
                    else
                    {
                        RainMeadow.Error("could not spawn a warp point for echo");
                    }
                }
                else
                {
                    RainMeadow.Error("echo does not have special data");
                }
            }
            else
            {
                orig(self);
            }
        }

        public void SpinningTop_Update(On.Watcher.SpinningTop.orig_Update orig, Watcher.SpinningTop self, bool eu)
        {
            orig(self, eu);
            if (OnlineManager.lobby != null)
            {
                var maximumRippleLevel = self.room.game.GetStorySession.saveState.deathPersistentSaveData.maximumRippleLevel;
                int num = 3;
                if (maximumRippleLevel != 0f)
                {
                    if (maximumRippleLevel != 0.25f)
                    {
                        if (maximumRippleLevel != 0.5f)
                        { num = -1; }
                        else
                        { num = 3; }
                    }
                    else
                    { num = 2; }
                }
                else
                {
                    num = 1;
                }
                self.vanillaEncounterNumber = num;
            }
        }

        public void SpinningTop_VanillaRegionSpinningTopEncounter(On.Watcher.SpinningTop.orig_VanillaRegionSpinningTopEncounter orig, Watcher.SpinningTop self)
        {
            orig(self);
            // HACK: Yep, hydrophonics doesnt get updated? WELL FORCE IT TO UPDATE
            // WE WILL GO TO HYDROPHONICS NO MATTER THE COST
            self.hasRequestedShutDown = true;
            self.room.game.GetStorySession.saveState.sessionEndingFromSpinningTopEncounter = true;
            self.room.game.Win(false, false);
            RainWorldGame.ForceSaveNewDenLocation(self.room.game, "HI_W05", true);
        }

        // Static method, fortunely, means we dont have to worry about keeping track of a spinning top (echo)
        public void SpinningTop_RaiseRippleLevel(On.Watcher.SpinningTop.orig_RaiseRippleLevel orig, Room room)
        {
            orig(room);
            if (RainMeadow.isStoryMode(out var story))
            {
                var vector = new UnityEngine.Vector2(
                    room.game.GetStorySession.saveState.deathPersistentSaveData.minimumRippleLevel,
                    room.game.GetStorySession.saveState.deathPersistentSaveData.maximumRippleLevel
                );
                if (OnlineManager.lobby.isOwner)
                {
                    story.rippleLevel = room.game.GetStorySession.saveState.deathPersistentSaveData.rippleLevel;
                }
                if (!OnlineManager.lobby.isOwner && story.rippleLevel < vector.y)
                {
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.RaiseRippleLevel, vector); // host needs notification that we get new rippleLevel
                }
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeOnceRPC(StoryRPCs.PlayRaiseRippleLevelAnimation, vector);
                    }
                }
            }

        }

        public void WarpPoint_PerformWarp(On.Watcher.WarpPoint.orig_PerformWarp orig, Watcher.WarpPoint self)
        {
            if (isStoryMode(out var storyGameMode))
            {
                orig(self);
                World world = self.room.game.overWorld.worldLoader.ReturnWorld();
                var ws = world.GetResource() ?? throw new KeyNotFoundException();
                ws.Deactivate();
                ws.NotNeeded();
            }
            else
            {
                orig(self);
            }
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
                { // tell host to move everyone else
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.PrinceSetHighestConversation, newValue);
                }
                StoryRPCs.PrinceSetHighestConversation(null, newValue);
            }
        }

        // echo warps from the waher
        public void OverWorld_InitiateSpecialWarp_WarpPoint(On.OverWorld.orig_InitiateSpecialWarp_WarpPoint orig, OverWorld self, MoreSlugcats.ISpecialWarp callback, Watcher.WarpPoint.WarpPointData warpData, bool useNormalWarpLoader)
        {
            if (OnlineManager.lobby != null && isStoryMode(out var storyGameMode) && callback is Watcher.WarpPoint warpPoint)
            {
                if (callback.getSourceRoom() == null)
                {
                    self.warpData = storyGameMode.myLastWarp;
                    storyGameMode.lastWarpIsEcho = true;
                }
                orig(self, callback, warpData, useNormalWarpLoader);
                string sourceRoomName = warpPoint.getSourceRoom() == null ? "" : warpPoint.getSourceRoom().abstractRoom.name;
                RainMeadow.Debug($"doing warp point from {sourceRoomName}, data={warpData.ToString()}");
            }
            else
            {
                orig(self, callback, warpData, useNormalWarpLoader);
            }
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
                var isEchoWarp = newRoom.game.GetStorySession.spinningTopWarpsLeadingToRippleScreen.Contains(self.MyIdentifyingString());
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

        // world transition at gates
        private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self, bool warpUsed)
        {
            if (OnlineManager.lobby != null)
            {
                WorldSession oldWorldSession = self.activeWorld.GetResource() ?? throw new KeyNotFoundException();
                WorldSession newWorldSession = self.worldLoader.world.GetResource() ?? throw new KeyNotFoundException();
                bool isSameWorld = (self.activeWorld.name == self.worldLoader.world.name);
                bool isEchoWarp = (self.game.GetStorySession.saveState.warpPointTargetAfterWarpPointSave != null);
                bool isFirstWarpWorld = false;

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
                    Watcher.WarpPoint warpPoint = self.specialWarpCallback as Watcher.WarpPoint ?? throw new InvalidProgrammerException("watcher warp point doesnt exist at time of loading");
                    Room room = warpPoint.room; //may be null in the case a client activates an echo warp
                    isFirstWarpWorld = (room == null) || (warpPoint.overrideData != null ? warpPoint.overrideData.rippleWarp : warpPoint.Data.rippleWarp); //do not update gate status afterwards :)
                    if (isEchoWarp || isFirstWarpWorld)
                    { //echo activation is special edge case
                        if (room == null) RainMeadow.Error("warp point with a null room");
                        RainMeadow.Debug("this an echo warp");
                        if (isStoryMode(out var storyGameMode))
                        {
                            self.warpData = storyGameMode.myLastWarp; //OVERRIDE WARP DATA VERY IMPORTANT!
                        }
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
                                    RainMeadow.Debug("removing remote entity from game " + oe);
                                    oe.beingMoved = true;
                                    if (oe.apo.realizedObject is Creature c && c.inShortcut)
                                    {
                                        if (c.RemoveFromShortcuts()) c.inShortcut = false;
                                    }
                                    entities.Remove(oe.apo);
                                    room.abstractRoom.creatures.Remove(oe.apo as AbstractCreature);
                                    if (oe.apo.realizedObject != null)
                                    {
                                        room.RemoveObject(oe.apo.realizedObject);
                                        room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                                    }
                                    oe.beingMoved = false;
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
                    RainMeadow.Debug($"Watcher warp switchery APOs preparations from {self.activeWorld.name} to {self.worldLoader.worldName}/{self.worldLoader.world?.name}");
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
                }
                else
                {
                    // special warp, don't bother with room items
                    orig(self, warpUsed);
                }

                if (warpUsed)
                { // and for warps we require a more manual approach; to properly make aware of old APOs entering a new region
                    foreach (var absplayer in self.game.Players)
                    {
                        if (absplayer.realizedCreature is Player player)
                        {
                            RainMeadow.Debug($"fixing player");
                            // do not get stuck on bottom left
                            player.abstractCreature.pos.Tile = new RWCustom.IntVector2((int)(self.warpData.destPos.Value.x / 20f), (int)(self.warpData.destPos.Value.y / 20f));
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

                if (!isSameWorld)
                { // there exists "warps" to the same world, twice, for some bloody reason
                    RainMeadow.Debug("Unsubscribing from old world");
                    oldWorldSession.Deactivate();
                    oldWorldSession.NotNeeded(); // done? let go
                }

                if (OnlineManager.lobby.isOwner)
                {
                    if (OnlineManager.lobby.gameMode is StoryGameMode storyGameMode && !isFirstWarpWorld)
                    {
                        storyGameMode.changedRegions = true;
                        storyGameMode.readyForTransition = StoryGameMode.ReadyForTransition.Crossed;
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
