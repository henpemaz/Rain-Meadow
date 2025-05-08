using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public void GameplayHooks()
        {
            On.ShelterDoor.Close += ShelterDoorOnClose;
            On.ShelterDoor.DoorClosed += ShelterDoor_DoorClosed;
            On.Creature.Update += CreatureOnUpdate;
            On.Creature.Violence += CreatureOnViolence;
            On.Lizard.Violence += Lizard_Violence; // todo there might be more like this one that do not call base()
            On.PhysicalObject.HitByWeapon += PhysicalObject_HitByWeapon;
            On.PhysicalObject.HitByExplosion += PhysicalObject_HitByExplosion;
            IL.ScavengerBomb.Explode += PhysicalObject_Explode;
            IL.MoreSlugcats.SingularityBomb.Explode += PhysicalObject_Explode;
            IL.FlareBomb.StartBurn += PhysicalObject_Explode;
            IL.FirecrackerPlant.Ignite += PhysicalObject_Trigger;
            IL.FirecrackerPlant.Explode += PhysicalObject_Explode;
            IL.PuffBall.Explode += PhysicalObject_Explode;
            IL.MoreSlugcats.FireEgg.Explode += PhysicalObject_Explode;
            IL.MoreSlugcats.EnergyCell.Explode += PhysicalObject_Explode;
            IL.JellyFish.Tossed += PhysicalObject_Trigger;
            IL.Snail.Click += PhysicalObject_Trigger;

            On.Spear.Spear_makeNeedle += Spear_makeNeedle;

            On.AbstractPhysicalObject.AbstractObjectStick.ctor += AbstractObjectStick_ctor;
            On.Creature.SwitchGrasps += Creature_SwitchGrasps;

            On.RoomRealizer.Update += RoomRealizer_Update;
            On.Creature.Die += Creature_Die; // do not die!
            IL.Player.TerrainImpact += Player_TerrainImpact;
            On.DeafLoopHolder.Update += DeafLoopHolder_Update;
            On.Weapon.HitThisObject += Weapon_HitThisObject;
            On.Weapon.HitAnotherThrownWeapon += Weapon_HitAnotherThrownWeapon;
            On.SocialEventRecognizer.CreaturePutItemOnGround += SocialEventRecognizer_CreaturePutItemOnGround;
            On.Watcher.WarpPoint.Update += Watcher_WarpPoint_Update;
            On.Watcher.WarpPoint.PerformWarp += WarpPoint_PerformWarp;
            On.Watcher.PrinceBehavior.InitateConversation += Watcher_PrinceBehavior_InitateConversation;
            IL.Watcher.Barnacle.LoseShell += Watcher_Barnacle_LoseShell;
            On.Watcher.SpinningTop.SpawnWarpPoint += SpinningTop_SpawnWarpPoint;
            On.Watcher.SpinningTop.RaiseRippleLevel += SpinningTop_RaiseRippleLevel;
            On.Watcher.SpinningTop.Update += SpinningTop_Update;
            On.RainWorldGame.ForceSaveNewDenLocation += RainWorldGame_ForceSaveNewDenLocation;
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

        private void RainWorldGame_ForceSaveNewDenLocation(On.RainWorldGame.orig_ForceSaveNewDenLocation orig, RainWorldGame game, string roomName, bool saveWorldStates)
        {
            
            if (RainMeadow.isStoryMode(out var story))
            {
                if (!OnlineManager.lobby.isOwner)
                {
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.ForceSaveNewDenLocation, roomName, saveWorldStates); // tell host to save den location for everyone else
                }
                
                
                
            }
           orig(game, roomName, saveWorldState);

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

        private void SocialEventRecognizer_CreaturePutItemOnGround(On.SocialEventRecognizer.orig_CreaturePutItemOnGround orig, 
            SocialEventRecognizer self, PhysicalObject item, Creature creature) {

            orig(self, item, creature);
            if (OnlineManager.lobby != null) return;
            if (!creature.IsLocal()) return;

            if (RoomSession.map.TryGetValue(creature.room.abstractRoom, out var roomSession)) {
                if (creature.abstractCreature.GetOnlineCreature(out OnlineCreature? oc) &&
                    item.abstractPhysicalObject.GetOnlineObject(out OnlinePhysicalObject? opo)) {
                    oc?.BroadcastRPCInRoom(roomSession.CreaturePutItemOnGround, 
                        opo.id, oc.id);
                } 
                
            }
        }

        private void Weapon_HitAnotherThrownWeapon(On.Weapon.orig_HitAnotherThrownWeapon orig, Weapon self, Weapon obj)
        {
            if (OnlineManager.lobby != null && self.IsLocal())
            {
                self.thrownBy.abstractPhysicalObject.GetOnlineObject().didParry = true;
            }
            orig(self, obj);
        }

        private bool Weapon_HitThisObject(On.Weapon.orig_HitThisObject orig, Weapon self, PhysicalObject obj)
        {
            if (!obj.FriendlyFireSafetyCandidate() && obj is Player && self is Spear && self.thrownBy != null && self.thrownBy is Player)
            {
                return true;
            }

            if (ModManager.MSC && (OnlineManager.lobby != null) && obj is Player pl && pl.slugOnBack?.slugcat != null && pl.slugOnBack.slugcat == self.thrownBy)
            {
                return false;
            }
            return orig(self, obj);
        }

        private void DeafLoopHolder_Update(On.DeafLoopHolder.orig_Update orig, DeafLoopHolder self, bool eu)
        {
            orig(self, eu);
            if (OnlineManager.lobby != null)
            {
                if (self.player != null && self.player.IsLocal() && self.player.dead && self.deafLoop != null)
                {
                    self.deafLoop = null;
                }
            }

        }

        private void Centipede_Shock(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(1),
                    i => i.MatchIsinst<Creature>(),
                    i => i.MatchCallvirt<Creature>(nameof(Creature.Die))
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((Centipede self, PhysicalObject shockObj) =>
                {
                    if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is not MeadowGameMode && shockObj is Player player)
                        DeathMessage.CvPRPC(self, player);
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // Keep this for now despite having DeathContextualizer
        private void Player_TerrainImpact(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdstr("Fall damage death")
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Player self) =>
                {
                    if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is not MeadowGameMode)
                    {
                        //DeathMessage.EnvironmentalDeathMessage(self, DeathMessage.DeathType.FallDamage);
                        DeathMessage.EnvironmentalRPC(self, DeathMessage.DeathType.FallDamage);
                    }

                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            if (OnlineManager.lobby != null)
            {
                if (OnlineManager.lobby.gameMode is MeadowGameMode)
                {
                    return;
                }

                if (!self.dead) // Prevent death messages from firing 987343 times.
                {
                    DeathMessage.CreatureDeath(self);
                }
            }
            orig(self);
        }
        private void Spear_makeNeedle(On.Spear.orig_Spear_makeNeedle orig, Spear self, int type, bool active)
        {
            // apo.realize defaults to inactive even if remote is active
            if (!self.IsLocal()) active = self.spearmasterNeedle_hasConnection;
            orig(self, type, active);
        }

        private void PhysicalObject_Trigger(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((PhysicalObject self) =>
                {
                    if (OnlineManager.lobby != null)
                    {
                        if (!self.abstractPhysicalObject.GetOnlineObject(out var opo))
                        {
                            Error($"Entity {self} doesn't exist in online space!");
                            return true;
                        }
                        if (opo.roomSession.isOwner && (opo.isMine || RPCEvent.currentRPCEvent is not null || self is not Player))
                        {
                            opo.BroadcastRPCInRoom(opo.Trigger);
                        }
                        else if (RPCEvent.currentRPCEvent is null)
                        {
                            if (!opo.isMine) return false;  // wait to be RPC'd
                            opo.roomSession.owner.InvokeOnceRPC(opo.Trigger);
                        }
                    }
                    return true;
                });
                c.Emit(OpCodes.Brtrue, skip);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void PhysicalObject_Explode(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((PhysicalObject self) =>
                {
                    if (OnlineManager.lobby != null)
                    {
                        if (!self.abstractPhysicalObject.GetOnlineObject(out var opo))
                        {
                            Error($"Entity {self} doesn't exist in online space!");
                            return true;
                        }
                        if (opo.roomSession.isOwner && (opo.isMine || RPCEvent.currentRPCEvent is not null || self is not Player))
                        {
                            opo.BroadcastRPCInRoom(opo.Explode, self.bodyChunks[0].pos);
                        }
                        else if (RPCEvent.currentRPCEvent is null)
                        {
                            if (!opo.isMine) return false;  // wait to be RPC'd
                            opo.roomSession.owner.InvokeOnceRPC(opo.Explode, self.bodyChunks[0].pos);
                        }
                    }
                    return true;
                });
                c.Emit(OpCodes.Brtrue, skip);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static void Creature_SwitchGrasps(On.Creature.orig_SwitchGrasps orig, Creature self, int fromGrasp, int toGrasp)
        {
            orig(self, fromGrasp, toGrasp);
            if (OnlineManager.lobby != null)
            {
                // unmap so they're re-created and detected as different instances by shallow delta
                var a = self.grasps[fromGrasp];
                var b = self.grasps[toGrasp];
                if (a != null) GraspRef.map.Remove(a);
                if (b != null) GraspRef.map.Remove(b);
                for (int j = 0; j < self.abstractCreature.stuckObjects.Count; j++)
                {
                    if (self.abstractCreature.stuckObjects[j] is AbstractPhysicalObject.CreatureGripStick cgs && cgs.A == self.abstractCreature)
                    {
                        if (a != null && a.graspUsed == cgs.grasp)
                        {
                            AbstractObjStickRepr.map.Remove(cgs);
                        }
                        else if (b != null && b.graspUsed == cgs.grasp)
                        {
                            AbstractObjStickRepr.map.Remove(cgs);
                        }
                    }
                }
            }
        }

        private static void AbstractObjectStick_ctor(On.AbstractPhysicalObject.AbstractObjectStick.orig_ctor orig, AbstractPhysicalObject.AbstractObjectStick self, AbstractPhysicalObject A, AbstractPhysicalObject B)
        {
            if (OnlineManager.lobby != null)
            {
                // issue: abstractsticks are often duplicated
                for (int i = A.stuckObjects.Count - 1; i >= 0; i--)
                {
                    var other = A.stuckObjects[i];
                    if (other.A == A && other.B == B && other.GetType() == self.GetType())
                    {
                        if (AbstractObjStickRepr.map.TryGetValue(other, out var otherRep))
                        {
                            AbstractObjStickRepr.map.Add(self, otherRep);
                        }
                        other.Deactivate();
                    }
                }
                // issue: connecting things that belong to different players is troublesome
                if (OnlinePhysicalObject.map.TryGetValue(A, out var opoA) && OnlinePhysicalObject.map.TryGetValue(B, out var opoB)) // both online
                {
                    if (opoA.isMine)
                    {
                        // try transfer "grabbed" side
                        if (!opoB.isMine)
                        {
                            RainMeadow.Debug("my object connecting to group that isn't mine");
                            var bentities = B.GetAllConnectedObjects().Select(o => OnlinePhysicalObject.map.TryGetValue(o, out var opo) ? opo : null).Where(o => o != null).ToList();
                            bool btransferable = bentities.All(e => e.isTransferable);
                            if (btransferable)
                            {
                                RainMeadow.Debug("requesting all connected objects");
                                foreach (var item in bentities)
                                {
                                    if (!item.isPending) item.Request();
                                    else
                                    {
                                        RainMeadow.Debug($"can't request {item} because pending");
                                    }
                                }
                            }
                            else
                            {
                                RainMeadow.Debug("can't request object because group not transferable");
                            }
                        } // else: both groups mine nothing to do
                    }
                    else if (opoB.isMine) // A not mine, B mine
                    {
                        RainMeadow.Debug("grabbed group is mine");
                        // grabber isn't mine, THEY need to request me tho
                        var bentities = B.GetAllConnectedObjects().Select(o => OnlinePhysicalObject.map.TryGetValue(o, out var opo) ? opo : null).Where(o => o != null).ToList();
                        bool btransferable = bentities.All(e => e.isTransferable);
                        if (btransferable)
                        {
                            RainMeadow.Debug("grabbed group is transferable"); // other will request
                        }
                        else
                        {
                            RainMeadow.Debug("grabbed group is NOT transferable");
                            var aentities = A.GetAllConnectedObjects().Select(o => OnlinePhysicalObject.map.TryGetValue(o, out var opo) ? opo : null).Where(o => o != null).ToList();
                            bool atransferable = aentities.All(e => e.isTransferable);
                            if (atransferable)
                            {
                                RainMeadow.Debug("requesting all connected objects");
                                foreach (var item in aentities)
                                {
                                    if (!item.isPending) item.Request();
                                    else
                                    {
                                        RainMeadow.Debug($"can't request {item} because pending");
                                    }
                                }
                            }
                            else
                            {
                                RainMeadow.Debug("can't request grabber group because group not transferable");
                            }
                        }
                    }
                }
            }
            orig(self, A, B);
        }

        private void PhysicalObject_HitByExplosion(On.PhysicalObject.orig_HitByExplosion orig, PhysicalObject self, float hitFac, Explosion explosion, int hitChunk)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, hitFac, explosion, hitChunk);
                return;
            }

            if (self.room == null && (self.room = explosion?.room) == null)
                return;

            if (RoomSession.map.TryGetValue(self.room.abstractRoom, out var room))
            {
                if (!room.isOwner)
                {
                    OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var objectHit);
                    OnlinePhysicalObject.map.TryGetValue(explosion?.sourceObject.abstractPhysicalObject, out var explosionSource);
                    if (objectHit != null && (objectHit.isMine || (explosionSource != null && explosionSource.isMine)))
                    {
                        room.owner.InvokeOnceRPC(objectHit.HitByExplosion, hitFac);
                        return;
                    }
                }
            }

            orig(self, hitFac, explosion, hitChunk);
        }

        private void PhysicalObject_HitByWeapon(On.PhysicalObject.orig_HitByWeapon orig, PhysicalObject self, Weapon weapon)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, weapon);
                return;
            }

            if (RoomSession.map.TryGetValue(self.room.abstractRoom, out var room))
            {
                if (!room.isOwner)
                {
                    OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var objectHit);
                    OnlinePhysicalObject.map.TryGetValue(weapon.abstractPhysicalObject, out var abstWeapon);
                    if (objectHit != null && abstWeapon != null && (objectHit.isMine || abstWeapon.isMine))
                    {
                        room.owner.InvokeRPC(objectHit.HitByWeapon, abstWeapon);
                        return;
                    }
                }
            }

            orig(self, weapon);
        }

        private void ShelterDoorOnClose(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self);
                return;
            }

            if (RainMeadow.isStoryMode(out var storyGameMode) && !self.Broken)
            {
                storyGameMode.storyClientData.readyForWin = true;
                if (!storyGameMode.readyForWin) return;
            }
            else
            {
                var scug = self.room.game.Players.First(); //needs to be changed if we want to support Jolly
                var realizedScug = (Player)scug.realizedCreature;
                if (realizedScug == null || !self.room.PlayersInRoom.Contains(realizedScug)) return;
                if (!realizedScug.readyForWin) return;
            }

            orig(self);

            if (self.IsClosing)
            {
                if (storyGameMode != null && storyGameMode.storyClientData.readyForWin)
                {
                    storyGameMode.myLastDenPos = self.room.abstractRoom.name;
                    storyGameMode.myLastWarp = null; //do not warp anymore!
                    storyGameMode.hasSheltered = true;
                }
            }
        }

        private void ShelterDoor_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
        {
            if (isStoryMode(out var storyGameMode) && !storyGameMode.hasSheltered) return;
            orig(self);
        }

        private void CreatureOnUpdate(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig(self, eu);
            if (OnlineManager.lobby == null) return;
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineCreature))
            {
                Trace($"Creature {self} {self.abstractPhysicalObject.ID} doesn't exist in online space!");
                return;
            }
            if (OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                if (EmoteDisplayer.map.TryGetValue(self, out var displayer))
                {
                    displayer.OnUpdate(); // so this only updates while the creature is in-room, what about creatures in pipes though
                }

                if (self is AirBreatherCreature breather) breather.lungs = 1f;

                if (self.room != null)
                {
                    // fall out of world handling
                    float num = -self.bodyChunks[0].restrictInRoomRange + 1f;
                    if (self is Player && self.bodyChunks[0].restrictInRoomRange == self.bodyChunks[0].defaultRestrictInRoomRange)
                    {
                        if ((self as Player).bodyMode == Player.BodyModeIndex.WallClimb)
                        {
                            num = Mathf.Max(num, -250f);
                        }
                        else
                        {
                            num = Mathf.Max(num, -500f);
                        }
                    }
                    if (self.bodyChunks[0].pos.y < num && (!self.room.water || self.room.waterInverted || self.room.defaultWaterLevel < -10) && (!self.Template.canFly || self.Stunned || self.dead) && (self is Player || !self.room.game.IsArenaSession || self.room.game.GetArenaGameSession.chMeta == null || !self.room.game.GetArenaGameSession.chMeta.oobProtect))
                    {
                        RainMeadow.Debug("fall out of world prevention: " + self);
                        var room = self.room;
                        self.RemoveFromRoom();
                        room.CleanOutObjectNotInThisRoom(self); // we need it this frame
                        var node = self.coord.abstractNode;
                        if (node > room.abstractRoom.exits) node = UnityEngine.Random.Range(0, room.abstractRoom.exits);
                        self.SpitOutOfShortCut(room.ShortcutLeadingToNode(node).startCoord.Tile, room, true);
                    }
                }
            }

            if (OnlineManager.lobby.gameMode is ArenaOnlineGameMode || OnlineManager.lobby.gameMode is StoryGameMode)
            {
                if (self.room != null)
                {
                    // fall out of world handling
                    float num = -self.bodyChunks[0].restrictInRoomRange + 1f;
                    if (self is Player && self.bodyChunks[0].restrictInRoomRange == self.bodyChunks[0].defaultRestrictInRoomRange)
                    {
                        if ((self as Player).bodyMode == Player.BodyModeIndex.WallClimb)
                        {
                            num = Mathf.Max(num, -250f);
                        }
                        else
                        {
                            num = Mathf.Max(num, -500f);
                        }
                    }
                    if (self.bodyChunks[0].pos.y < num && (!self.room.water || self.room.waterInverted || self.room.defaultWaterLevel < -10) && (!self.Template.canFly || self.Stunned || self.dead) && (self is Player || self.room.game.GetArenaGameSession.chMeta == null || !self.room.game.GetArenaGameSession.chMeta.oobProtect))
                    {

                        //DeathMessage.EnvironmentalDeathMessage(self as Player, DeathMessage.DeathType.Abyss);
                        DeathMessage.EnvironmentalRPC(self as Player, DeathMessage.DeathType.Abyss);
                        RainMeadow.Debug("prevent abstract creature destroy: " + self); // need this so that we don't release the world session on death
                        self.Die();
                        self.State.alive = false;
                    }
                }
            }

            // this is here as a safegard because we don't transfer full detail grasp data
            if (onlineCreature.isMine && self.grasps != null)
            {
                foreach (var grasp in self.grasps)
                {
                    if (grasp == null) continue;
                    if (!OnlinePhysicalObject.map.TryGetValue(grasp.grabbed.abstractPhysicalObject, out var onlineGrabbed))
                    {
                        Trace($"Grabbed object {grasp.grabbed.abstractPhysicalObject} {grasp.grabbed.abstractPhysicalObject.ID} doesn't exist in online space!");
                        continue;
                    }
                    if (!onlineGrabbed.isMine && onlineGrabbed.isTransferable && !onlineGrabbed.isPending) // been leased to someone else
                    {
                        var grabbersOtherThanMe = grasp.grabbed.grabbedBy.Select(x => x.grabber).Where(x => x != self);
                        if (grabbersOtherThanMe.All(x => x.abstractPhysicalObject.GetOnlineObject(out var opo) && opo.isMine))
                            onlineGrabbed.Request();
                    }
                }
            }
        }

        private void CreatureOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                return;
            }
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineApo) || onlineApo is not OnlineCreature onlineCreature)
            {
                Error($"Target {self} doesn't exist in online space!");
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                return;
            }

            var room = self.room;
            if (room != null && room.updateIndex <= room.updateList.Count)
            {
                PhysicalObject trueVillain = null;
                var suspect = room.updateList[room.updateIndex];
                if (suspect is Explosion explosion) trueVillain = explosion.sourceObject;
                else if (suspect is PhysicalObject villainObject) trueVillain = villainObject;
                if (trueVillain != null)
                {
                    if (!OnlinePhysicalObject.map.TryGetValue(trueVillain.abstractPhysicalObject, out var onlineTrueVillain))
                    {
                        if (trueVillain.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb
                            || trueVillain.abstractPhysicalObject.type == DLCSharedEnums.AbstractObjectType.SingularityBomb)
                        {
                            // bombs exit quickly, and that's ok.
                            OnlinePhysicalObject onlineVillain = null;
                            onlineCreature.RPCCreatureViolence(onlineVillain, hitChunk?.index, hitAppendage, directionAndMomentum, type, damage, stunBonus);
                            return;
                        }
                        Error($"True villain {trueVillain} - {trueVillain.abstractPhysicalObject.ID} doesn't exist in online space!");
                        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                        return;
                    }
                    if ((onlineTrueVillain.owner.isMe || onlineTrueVillain.isPending) && !onlineApo.owner.isMe) // I'm violencing a remote entity
                    {
                        OnlinePhysicalObject onlineVillain = null;
                        if (source != null && !OnlinePhysicalObject.map.TryGetValue(source.owner.abstractPhysicalObject, out onlineVillain))
                        {
                            Error($"Source {source.owner} - {source.owner.abstractPhysicalObject.ID} doesn't exist in online space!");
                            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                            return;
                        }
                        // Notify entity owner of violence
                        onlineCreature.RPCCreatureViolence(onlineVillain, hitChunk?.index, hitAppendage, directionAndMomentum, type, damage, stunBonus);
                        return; // Remote is gonna handle this
                    }
                    if (!onlineTrueVillain.owner.isMe) return; // Remote entity will send an event
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        // copypaste of above for now
        private void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                return;
            }
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineApo) || onlineApo is not OnlineCreature onlineCreature)
            {
                Error($"Target {self} doesn't exist in online space!");
                orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                return;
            }

            var room = self.room;
            if (room != null && room.updateIndex <= room.updateList.Count)
            {
                PhysicalObject trueVillain = null;
                var suspect = room.updateList[room.updateIndex];
                if (suspect is Explosion explosion) trueVillain = explosion.sourceObject;
                else if (suspect is PhysicalObject villainObject) trueVillain = villainObject;
                if (trueVillain != null)
                {
                    if (!OnlinePhysicalObject.map.TryGetValue(trueVillain.abstractPhysicalObject, out var onlineTrueVillain))
                    {
                        if (trueVillain.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb
                            || trueVillain.abstractPhysicalObject.type == DLCSharedEnums.AbstractObjectType.SingularityBomb)
                        {
                            // bombs exit quickly, and that's ok.
                            OnlinePhysicalObject onlineVillain = null;
                            onlineCreature.RPCCreatureViolence(onlineVillain, hitChunk?.index, hitAppendage, directionAndMomentum, type, damage, stunBonus);
                            return;
                        }
                        Error($"True villain {trueVillain} - {trueVillain.abstractPhysicalObject.ID} doesn't exist in online space!");
                        orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                        return;
                    }
                    if ((onlineTrueVillain.owner.isMe || onlineTrueVillain.isPending) && !onlineApo.owner.isMe) // I'm violencing a remote entity
                    {
                        OnlinePhysicalObject onlineVillain = null;
                        if (source != null && !OnlinePhysicalObject.map.TryGetValue(source.owner.abstractPhysicalObject, out onlineVillain))
                        {
                            Error($"Source {source.owner} - {source.owner.abstractPhysicalObject.ID} doesn't exist in online space!");
                            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                            return;
                        }
                        // Notify entity owner of violence
                        onlineCreature.RPCCreatureViolence(onlineVillain, hitChunk?.index, hitAppendage, directionAndMomentum, type, damage, stunBonus);
                        return; // Remote is gonna handle this
                    }
                    if (!onlineTrueVillain.owner.isMe) return; // Remote entity will send an event
                }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private void RoomRealizer_Update(On.RoomRealizer.orig_Update orig, RoomRealizer self)
        {
            if (OnlineManager.lobby != null && self.followCreature != null)
            {
                var origFollow = self.world.game.cameras[0].followAbstractCreature;
                self.world.game.cameras[0].followAbstractCreature = self.followCreature;
                orig(self);
                self.world.game.cameras[0].followAbstractCreature = origFollow;
                return;
            }

            orig(self);
        }
    }
}
