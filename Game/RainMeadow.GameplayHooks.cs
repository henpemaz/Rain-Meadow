using RWCustom;
using Sony.NP;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public void GameplayHooks()
        {
            On.ShelterDoor.Close += ShelterDoorOnClose;
            On.Creature.Update += CreatureOnUpdate;
            On.Creature.Violence += CreatureOnViolence;
            On.Creature.Grasp.ctor += GraspOnctor;
            On.PhysicalObject.Grabbed += PhysicalObjectOnGrabbed;
            On.PhysicalObject.HitByWeapon += PhysicalObject_HitByWeapon;
            On.PhysicalObject.HitByExplosion += PhysicalObject_HitByExplosion;

        }

        private void ScavengerBomb_TerrainImpact(On.ScavengerBomb.orig_TerrainImpact orig, ScavengerBomb self, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, chunk, direction, speed, firstContact);
                return;
            }

            if (!RoomSession.map.TryGetValue(self.room.abstractRoom, out var room))
            {
                Error("Error getting room for scav explosion!");

            }
            if (!room.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {


                if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var scavBombAbstract))

                {
                    Error("Error getting target of explosion object hit");

                }

                RainMeadow.Debug("TERRAUB CHUNKS INDEX X ABST " + chunk);

                RainMeadow.Debug("DIR POS X ABST " + direction);
                RainMeadow.Debug("SPD RAD X ABST " + speed);
                RainMeadow.Debug("FC MASS X ABST " + firstContact);

                if (scavBombAbstract != null)
                {
                    if (!room.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(OnlinePhysicalObject.ScavengerBombTerrainImpact, scavBombAbstract, chunk, direction, speed, firstContact)))
                    {
                        room.owner.InvokeRPC(OnlinePhysicalObject.ScavengerBombTerrainImpact, scavBombAbstract, chunk, direction, speed, firstContact);
                    }
                }

            }
            orig(self, chunk, direction, speed, firstContact);
        }

        private void ScavengerBomb_Explode(On.ScavengerBomb.orig_Explode orig, ScavengerBomb self, BodyChunk hitChunk)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, hitChunk);
                return;
            }

            if (!RoomSession.map.TryGetValue(self.room.abstractRoom, out var room))
            {
                Error("Error getting room for scav explosion!");

            }
            if (!room.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {


                if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var scavBombAbstract))

                {
                    RainMeadow.Debug("Error getting scav bomb abstract");

                }

                // HIT CHUNK INDEX NULL REFS ON THROW TO GROUND
                //RainMeadow.Debug("EXPLODE CHUNKS INDEX X ABST " + hitChunk.index);

                RainMeadow.Debug("CHUNKS POS X ABST " + hitChunk.pos);
                RainMeadow.Debug("CHUNKS RAD X ABST " + hitChunk.rad);
                RainMeadow.Debug("CHUNKS MASS X ABST " + hitChunk.mass);


                if (scavBombAbstract != null)
                {
                    if (!room.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(OnlinePhysicalObject.ScavengerBombExplode, scavBombAbstract, hitChunk.index, hitChunk.pos, hitChunk.rad, hitChunk.mass)))
                    {
                        // Currently null refs, kills client game
                        // Then the other player can pick up where client threw bomb anad throw it again.
                        room.owner.InvokeRPC(OnlinePhysicalObject.ScavengerBombExplode, scavBombAbstract, hitChunk.index, hitChunk.pos, hitChunk.rad, hitChunk.mass);

                    }
                }
            }
            orig(self, hitChunk);
        }

        private void PhysicalObject_HitByExplosion(On.PhysicalObject.orig_HitByExplosion orig, PhysicalObject self, float hitFac, Explosion explosion, int hitChunk)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, hitFac, explosion, hitChunk);
                return;
            }

            // explosion.room is the most stable source of truth for room data. Other options sometimes null ref
            if (!RoomSession.map.TryGetValue(explosion.room.abstractRoom, out var room))
            {
                Error("Error getting room for explosion!");

            }
            if (!room.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var objectHit))
                {
                    Error("Error getting target of explosion object hit");

                }
                if (!OnlinePhysicalObject.map.TryGetValue(explosion.sourceObject.abstractPhysicalObject, out var sourceObject))
                {
                    // Not getting source object can kill friendly slugcats and will null ref in the RPC but continue the game safely.
                    // May be due to current null Creature violence against other slugcats.
                    // Can only replicate if I throw explosive spear at friendly slugcat who is right next to a wall and the spear hits their position but denies a kill
                    Error("Error getting source object for explosion");
                }

                if (explosion.killTagHolder == null)
                {
                    orig(self, hitFac, explosion, hitChunk); // Safely kills target when it's stuck in them.
                    return;
                }


                if (!OnlinePhysicalObject.map.TryGetValue(explosion.killTagHolder.abstractPhysicalObject, out var onlineCreature)) // to pass OnlinePhysicalObject data to convert to OnlineCreature over the wire
                {

                    Error("Error getting kill tag holder");
                }

                if (objectHit != null)
                {
                    if (!room.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(OnlinePhysicalObject.HitByExplosion, objectHit, sourceObject, explosion.pos, explosion.lifeTime, explosion.rad, explosion.force, explosion.damage, explosion.stun, explosion.deafen, onlineCreature, explosion.killTagHolderDmgFactor, explosion.minStun, explosion.backgroundNoise, hitFac, hitChunk)))
                    {
                        room.owner.InvokeRPC(OnlinePhysicalObject.HitByExplosion, objectHit, sourceObject, explosion.pos, explosion.lifeTime, explosion.rad, explosion.force, explosion.damage, explosion.stun, explosion.deafen, onlineCreature, explosion.killTagHolderDmgFactor, explosion.minStun, explosion.backgroundNoise, hitFac, hitChunk);
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
                var playerIDs = OnlineManager.lobby.participants.Keys.Select(p => p.inLobbyId).ToList();
                var readyWinPlayers = storyGameMode.readyForWinPlayers.ToList();

                foreach (var playerID in playerIDs)
                {
                    if (!readyWinPlayers.Contains(playerID)) return;
                }
                var storyClientSettings = storyGameMode.clientSettings as StoryClientSettings;
                storyClientSettings.myLastDenPos = self.room.abstractRoom.name;

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
                    if (!onlineGrabbed.isMine && onlineGrabbed.isTransferable && !onlineGrabbed.isPending)
                    {
                        if (grasp.grabbed is not Creature) // Non-Creetchers cannot be grabbed by multiple creatures
                        {
                            grasp.Release();
                            return;
                        }

                        var grabbersOtherThanMe = grasp.grabbed.grabbedBy.Select(x => x.grabber).Where(x => x != self);
                        foreach (var grabbers in grabbersOtherThanMe)
                        {
                            if (!OnlinePhysicalObject.map.TryGetValue(grabbers.abstractPhysicalObject, out var tempEntity))
                            {
                                Error($"Other grabber {grabbers.abstractPhysicalObject} {grabbers.abstractPhysicalObject.ID} doesn't exist in online space!");
                                continue;
                            }
                            if (!tempEntity.isMine) return;
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
                //if (suspect is Explosion explosion) trueVillain = explosion.sourceObject;
                if (suspect is PhysicalObject villainObject) trueVillain = villainObject;
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

        private void GraspOnctor(On.Creature.Grasp.orig_ctor orig, Creature.Grasp self, Creature grabber, PhysicalObject grabbed, int graspused, int chunkgrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            orig(self, grabber, grabbed, graspused, chunkgrabbed, shareability, dominance, pacifying);
            if (OnlineManager.lobby == null) return;
            if (!OnlinePhysicalObject.map.TryGetValue(grabber.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");
            if (!OnlinePhysicalObject.map.TryGetValue(grabbed.abstractPhysicalObject, out var onlineGrabbed)) throw new InvalidOperationException("Grabbed thing doesn't exist in online space!");

            if (onlineGrabber.isMine && !onlineGrabbed.isMine && onlineGrabbed.isTransferable && !onlineGrabbed.isPending)
            {
                onlineGrabbed.Request();
            }
        }

        private void PhysicalObjectOnGrabbed(On.PhysicalObject.orig_Grabbed orig, PhysicalObject self, Creature.Grasp grasp)
        {
            orig(self, grasp);
            if (OnlineManager.lobby == null) return;
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidOperationException("Entity doesn't exist in online space!");
            if (!OnlinePhysicalObject.map.TryGetValue(grasp.grabber.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");

            if (!onlineEntity.isTransferable && onlineEntity.isMine)
            {
                if (!onlineGrabber.isMine && onlineGrabber.isTransferable && !onlineGrabber.isPending)
                {
                    onlineGrabber.Request(); // If I've been grabbed and I'm not transferrable, but my grabber is, request him
                }
            }
        }
    }
}
