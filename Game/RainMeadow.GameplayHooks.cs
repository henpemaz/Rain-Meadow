﻿using System.Linq;
using UnityEngine;
namespace RainMeadow
{
    public partial class RainMeadow
    {
        public void GameplayHooks()
        {
            On.ShelterDoor.Close += ShelterDoorOnClose;
            On.Creature.Update += CreatureOnUpdate;
            On.Creature.Violence += CreatureOnViolence;
            On.Lizard.Violence += LizardOnViolence;
            On.PhysicalObject.HitByWeapon += PhysicalObject_HitByWeapon;
            On.PhysicalObject.HitByExplosion += PhysicalObject_HitByExplosion;
            On.ScavengerBomb.Explode += ScavengerBomb_Explode;
            On.MoreSlugcats.SingularityBomb.Explode += SingularityBomb_Explode;

            On.AbstractPhysicalObject.AbstractObjectStick.ctor += AbstractObjectStick_ctor;
            On.Creature.SwitchGrasps += Creature_SwitchGrasps;

            On.RoomRealizer.Update += RoomRealizer_Update;
        }

        private void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            if (OnlineManager.lobby != null)
            {
                RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
                if (!room.isOwner)
                {
                    if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var opo))
                    {
                        Error($"Entity {self} doesn't exist in online space!");
                        return;
                    }
                    room.owner.InvokeOnceRPC(OnlinePhysicalObject.ScavengerBombExplode, opo, self.bodyChunks[0].pos);
                }
            }
            orig(self, hitChunk);
        }

        private void SingularityBomb_Explode(On.MoreSlugcats.SingularityBomb.orig_Explode orig, MoreSlugcats.SingularityBomb self)
        {
            if (OnlineManager.lobby != null)
            {
                if (self.activateLightning != null)
                {
                    self.activateLightning.Destroy();
                    self.activateLightning = null;
                }
                RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
                if (!room.isOwner)
                {
                    if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var opo))
                    {
                        Error($"Entity {self} doesn't exist in online space!");
                        return;
                    }
                    room.owner.InvokeOnceRPC(OnlinePhysicalObject.SingularityBombExplode, opo, self.bodyChunks[0].pos);
                }
            }
            orig(self);
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
                    room.owner.InvokeOnceRPC(OnlinePhysicalObject.HitByExplosion, objectHit, hitFac);
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
                    room.owner.InvokeRPC(OnlinePhysicalObject.HitByWeapon, objectHit, abstWeapon);
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

            var storyGameMode = OnlineManager.lobby.gameMode as StoryGameMode;
            var storyClientSettings = storyGameMode?.storyClientData;
            if (storyGameMode != null)
            {
                storyClientSettings.readyForWin = true;

                var anyNotReady = false;
                foreach (var cs in OnlineManager.lobby.clientSettings.Values)
                {
                    var scs = cs.GetData<StoryClientSettingsData>();
                    RainMeadow.Debug($"player {cs.owner} inGame:{cs.inGame} isDead:{scs.isDead} readyForWin:{scs.readyForWin}");
                    anyNotReady |= cs.inGame && !scs.isDead && !scs.readyForWin;
                }

                if (anyNotReady)
                {
                    return;
                }
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
                if (storyGameMode != null)
                {
                    storyGameMode.myLastDenPos = self.room.abstractRoom.name;
                    storyGameMode.hasSheltered = true;
                }
            }
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

            if (OnlineManager.lobby.gameMode is ArenaCompetitiveGameMode || OnlineManager.lobby.gameMode is StoryGameMode)
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
                        if (grasp.grabbed is not Creature) // Non-Creetchers cannot be grabbed by multiple creatures
                        {
                            self.ReleaseGrasp(grasp.graspUsed);
                            continue;
                        }

                        var grabbersOtherThanMe = grasp.grabbed.grabbedBy.Select(x => x.grabber).Where(x => x != self);
                        foreach (var grabbers in grabbersOtherThanMe)
                        {
                            if (!OnlinePhysicalObject.map.TryGetValue(grabbers.abstractPhysicalObject, out var tempEntity))
                            {
                                Trace($"Other grabber {grabbers.abstractPhysicalObject} {grabbers.abstractPhysicalObject.ID} doesn't exist in online space!");
                                continue;
                            }
                            if (!tempEntity.isMine) continue;
                        }
                        // If no remotes holding the entity, request it
                        onlineGrabbed.Request();
                    }
                }
            }
        }

        private bool DoViolence(Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
        {
            if (!self.abstractCreature.GetOnlineCreature(out var onlineVictim))
            {
                Error($"Chunk owner {self} - {self.abstractPhysicalObject.ID} doesn't exist in online space!");
                return true;
            }
            var room = self.room;
            if (room != null && room.updateIndex <= room.updateList.Count)
            {
                PhysicalObject? trueVillain = room.updateList[room.updateIndex] switch {
                    Explosion explosion => explosion.sourceObject,
                    PhysicalObject villainObject => villainObject,
                    _ => null,
                };
                if (trueVillain is null) return true;
                if (!trueVillain.abstractPhysicalObject.GetOnlineObject(out var onlineTrueVillain))
                {
                    // bombs exit quickly, and that's ok.
                    if (trueVillain.abstractPhysicalObject.type != AbstractPhysicalObject.AbstractObjectType.ScavengerBomb
                        && trueVillain.abstractPhysicalObject.type != MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.SingularityBomb)
                    {
                        Error($"True villain {trueVillain} - {trueVillain.abstractPhysicalObject.ID} doesn't exist in online space!");
                        return true;
                    }
                }
                if ((onlineTrueVillain is null || onlineTrueVillain.owner.isMe || onlineTrueVillain.isPending) && !onlineVictim.owner.isMe) // I'm violencing a remote entity
                {
                    OnlinePhysicalObject? onlineVillain = null;
                    if (source is not null && !OnlinePhysicalObject.map.TryGetValue(source.owner.abstractPhysicalObject, out onlineVillain))
                    {
                        Error($"Source {source.owner} - {source.owner.abstractPhysicalObject.ID} doesn't exist in online space!");
                        return true;
                    }
                    // Notify entity owner of violence
                    onlineVictim.RPCCreatureViolence(onlineVillain, hitchunk?.index, hitappendage, directionAndMomentum, type, damage, stunbonus);
                    return false; // Remote is gonna handle this
                }
                if (onlineTrueVillain is not null && !onlineTrueVillain.owner.isMe) return false; // Remote entity will send an event
            }
            return true;
        }

        private void CreatureOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
        {
            if (OnlineManager.lobby is not null)
            {
                if (!DoViolence(self, source, directionAndMomentum, hitchunk, hitappendage, type, damage, stunbonus))
                    return;
            }
            orig(self, source, directionAndMomentum, hitchunk, hitappendage, type, damage, stunbonus);
        }

        private void LizardOnViolence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
        {
            if (OnlineManager.lobby is not null)
            {
                DoViolence(self, source, directionAndMomentum, hitchunk, hitappendage, type, damage, stunbonus);
            }
            orig(self, source, directionAndMomentum, hitchunk, hitappendage, type, damage, stunbonus);
        }

        private void RoomRealizer_Update(On.RoomRealizer.orig_Update orig, RoomRealizer self)
        {
            if (OnlineManager.lobby != null)
            {
                var origFollow = self.world.game.cameras[0].followAbstractCreature;
                self.world.game.cameras[0].followAbstractCreature = self.world.game.Players[0];
                orig(self);
                self.world.game.cameras[0].followAbstractCreature = origFollow;
                return;
            }

            orig(self);
        }
    }
}
