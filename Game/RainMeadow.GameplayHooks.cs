using RWCustom;
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
            On.Creature.Update += CreatureOnUpdate;
            On.Creature.Violence += CreatureOnViolence;
            On.PhysicalObject.HitByWeapon += PhysicalObject_HitByWeapon;
            On.PhysicalObject.HitByExplosion += PhysicalObject_HitByExplosion;

            On.AbstractPhysicalObject.AbstractObjectStick.ctor += AbstractObjectStick_ctor;
            On.Creature.SwitchGrasps += Creature_SwitchGrasps;
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

            RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
            if (!room.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var objectHit);
                if (objectHit != null)
                {
                    if (!room.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(OnlinePhysicalObject.HitByExplosion, objectHit, hitFac)))
                    {
                        room.owner.InvokeRPC(OnlinePhysicalObject.HitByExplosion, objectHit, hitFac);
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

            RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
            if (!room.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var objectHit);
                OnlinePhysicalObject.map.TryGetValue(weapon.abstractPhysicalObject, out var abstWeapon);
                room.owner.InvokeRPC(OnlinePhysicalObject.HitByWeapon, objectHit, abstWeapon);
            }
            else
            {
                orig(self, weapon);
            }
        }

        private void ShelterDoorOnClose(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self);
                return;
            }

            if (OnlineManager.lobby.gameMode is StoryGameMode storyGameMode)
            {
                //for now force all players to be in the shelter to close the door.
                var playerIDs = OnlineManager.lobby.participants.Select(p => p.inLobbyId).ToList();
                var readyWinPlayers = storyGameMode.readyForWinPlayers.ToList();

                foreach (var playerID in playerIDs)
                {
                    if (!readyWinPlayers.Contains(playerID)) return;
                }
                var storyClientSettings = storyGameMode.clientSettings as StoryClientSettings;
                storyClientSettings.myLastDenPos = self.room.abstractRoom.name;
                if (OnlineManager.lobby.isOwner)
                {
                    (OnlineManager.lobby.gameMode as StoryGameMode).defaultDenPos = self.room.abstractRoom.name;
                }
                storyGameMode.changedRegions = false;
            }
            else
            {
                var scug = self.room.game.Players.First(); //needs to be changed if we want to support Jolly
                var realizedScug = (Player)scug.realizedCreature;
                if (realizedScug == null || !self.room.PlayersInRoom.Contains(realizedScug)) return;
                if (!realizedScug.readyForWin) return;
            }
            orig(self);
        }

        private void CreatureOnUpdate(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig(self, eu);
            if (OnlineManager.lobby == null) return;
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineCreature))
            {
                Error($"Creature {self} {self.abstractPhysicalObject.ID} doesn't exist in online space!");
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

            if (OnlineManager.lobby.gameMode is ArenaCompetitiveGameMode) // Need to test this with creatures on
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
                        Error($"Grabbed object {grasp.grabbed.abstractPhysicalObject} {grasp.grabbed.abstractPhysicalObject.ID} doesn't exist in online space!");
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
                                Error($"Other grabber {grabbers.abstractPhysicalObject} {grabbers.abstractPhysicalObject.ID} doesn't exist in online space!");
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

        private void CreatureOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionandmomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                return;
            }
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineVictim) || onlineVictim is not OnlineCreature)
            {
                Error($"Chunk owner {self} - {self.abstractPhysicalObject.ID} doesn't exist in online space!");
                orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
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
                        Error($"True villain {trueVillain} - {trueVillain.abstractPhysicalObject.ID} doesn't exist in online space!");
                        orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                        return;
                    }
                    if ((onlineTrueVillain.owner.isMe || onlineTrueVillain.isPending) && !onlineVictim.owner.isMe) // I'm violencing a remote entity
                    {
                        OnlinePhysicalObject onlineVillain = null;
                        if (source != null && !OnlinePhysicalObject.map.TryGetValue(source.owner.abstractPhysicalObject, out onlineVillain))
                        {
                            Error($"Source {source.owner} - {source.owner.abstractPhysicalObject.ID} doesn't exist in online space!");
                            orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                            return;
                        }
                        // Notify entity owner of violence
                        (onlineVictim as OnlineCreature).RPCCreatureViolence(onlineVillain, hitchunk?.index, hitappendage, directionandmomentum, type, damage, stunbonus);
                        return; // Remote is gonna handle this
                    }
                    if (!onlineTrueVillain.owner.isMe) return; // Remote entity will send an event
                }
            }
            orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
        }
    }
}
