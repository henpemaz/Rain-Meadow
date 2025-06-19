using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
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

            // If the weapon super calls Weapon.HitSomething we don't have to hook
            // Weapon_HitSomething hooks
            HookWeaponHitSomething<Spear>();
            HookWeaponHitSomething<ScavengerBomb>();
            HookWeaponHitSomething<FirecrackerPlant>();
            // HookWeapon<FlareBomb>();
            // HookWeapon<PuffBall>();
            HookWeaponHitSomething<Rock>();

            // moreslugcats Weapon_HitSomething hooks
            HookWeaponHitSomething<MoreSlugcats.LillyPuck>();
            HookWeaponHitSomething<MoreSlugcats.Bullet>();
            HookWeaponHitSomething<MoreSlugcats.SingularityBomb>();

            // Watcher Weapon_HitSomething
            HookWeaponHitSomething<Boomerang>();

            // for super calls
            HookWeaponHitSomething<Weapon>();
            


            On.PhysicalObject.HitByExplosion += PhysicalObject_HitByExplosion;
            IL.ScavengerBomb.Explode += PhysicalObject_Explode;
            IL.ExplosiveSpear.Explode += PhysicalObject_Explode;

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

        bool WeaponIsDangerous(Weapon weapon)
        {
            if (ModManager.DLCShared && weapon is MoreSlugcats.LillyPuck) return true;
            if (weapon is Spear) return true;

            return false;
        }

        private bool Weapon_HitThisObject(On.Weapon.orig_HitThisObject orig, Weapon self, PhysicalObject obj)
        {
            if (!self.IsLocal())
            {
                return false;
            }

            if (obj is Creature c && c.FriendlyFireSafetyCandidate(self.thrownBy) && WeaponIsDangerous(self))
            {
                return false;
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

        // Keep this for now despite having DeathContextualizerf
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


        void HookWeaponHitSomething<T>() where T : Weapon => new Hook(typeof(T).GetMethod("HitSomething"), Weapon_HitSomething<T>);
        delegate bool Weapon_orig_HitSomething<T>(T self, SharedPhysics.CollisionResult result, bool eu);
        private bool Weapon_HitSomething<WeaponT>(Weapon_orig_HitSomething<WeaponT> orig, WeaponT self, SharedPhysics.CollisionResult result, bool eu) 
            where WeaponT : Weapon 
        {

            if (OnlineManager.lobby == null)
            {    
                return orig(self, result, eu);
            }

            if (result.obj == null) 
            {
                return orig(self, result, eu);
            }

            OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var WeaponOnline);
            OnlinePhysicalObject.map.TryGetValue(result.obj.abstractPhysicalObject, out var onlineHit);
            if (onlineHit == null) {
                RainMeadow.Debug($"Object hit by weapon not found in online space. object: {onlineHit}, weapon: {WeaponOnline}");
                return orig(self, result, eu);
            }

            if (WeaponOnline == null) {
                RainMeadow.Debug($"weapon that hit object not found in online space. object: {onlineHit}, weapon: {WeaponOnline}");
                return orig(self, result, eu);
            }

            if (WeaponOnline.HittingRemotely) {
                bool wasthrown = self.mode == Weapon.Mode.Thrown;
				if (self.thrownBy != null && result.obj != null && result.obj is Creature critter)
                {
                    self.thrownClosestToCreature = null;
                    self.closestCritDist = float.MaxValue;
                    critter.SetKillTag(self.thrownBy.abstractCreature);
                }

                bool ret = orig(self, result, eu);

                if (self is ExplosiveSpear explosiveSpear) {
                    if (wasthrown && explosiveSpear.mode != Weapon.Mode.Thrown && explosiveSpear.igniteCounter < 1) {
                        explosiveSpear.Ignite();
                    }
                }

                return ret;
            }
            else if (self.IsLocal()) {
                RealizedPhysicalObjectState realizedstate = null!;
                if (self is Spear) realizedstate = new RealizedSpearState(WeaponOnline);    
                else realizedstate = new RealizedWeaponState(WeaponOnline);


                BodyChunkRef? chunk = result.chunk is null? null : new BodyChunkRef(onlineHit, result.chunk.index);
                AppendageRef? appendageRef = result.onAppendagePos is null ? null : new AppendageRef(result.onAppendagePos);

                if (!onlineHit.owner.isMe)
                {
                    onlineHit.owner.InvokeRPC(WeaponOnline.WeaponHitSomething, realizedstate, new OnlinePhysicalObject.OnlineCollisionResult(
                        onlineHit.id, chunk, appendageRef, result.hitSomething, result.collisionPoint
                    ));
                }

                return orig(self, result, eu);
            } 
            return true;
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
                    if (!(trueVillain is Weapon)) // handled Weapon_HitSomething 
                    {
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
