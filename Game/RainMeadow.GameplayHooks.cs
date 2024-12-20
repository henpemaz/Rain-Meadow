using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
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

            RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
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

            orig(self, hitFac, explosion, hitChunk);
        }

        private void PhysicalObject_HitByWeapon(On.PhysicalObject.orig_HitByWeapon orig, PhysicalObject self, Weapon weapon)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, weapon);
                return;
            }

            RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
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
                            || trueVillain.abstractPhysicalObject.type == MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.SingularityBomb)
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
                            || trueVillain.abstractPhysicalObject.type == MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.SingularityBomb)
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
