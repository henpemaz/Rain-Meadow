﻿using RWCustom;
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
            On.Creature.Grasp.ctor += GraspOnctor;
            On.PhysicalObject.Grabbed += PhysicalObjectOnGrabbed;
            On.Creature.SuckedIntoShortCut += CreatureSuckedIntoShortCut;
        }

        private void ShelterDoorOnClose(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self);
                return;
            }

            var scug = self.room.game.Players.First(); //needs to be changed if we want to support Jolly
            var realizedScug = (Player)scug.realizedCreature;
            if (realizedScug == null || !self.room.PlayersInRoom.Contains(realizedScug)) return;
            if (!realizedScug.readyForWin) return;
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
            if (!onlineCreature.isMine) return;

            if (self.grasps == null) return;
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

        private void CreatureOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionandmomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                return;
            }
            if (!OnlinePhysicalObject.map.TryGetValue(hitchunk.owner.abstractPhysicalObject, out var onlineVictim) || onlineVictim is not OnlineCreature)
            {
                Error($"Chunk owner {hitchunk.owner} - {hitchunk.owner.abstractPhysicalObject.ID} doesn't exist in online space!");
                orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
                return;
            }
            var room = hitchunk.owner.room;
            if (room != null && room.updateIndex <= room.updateList.Count)
            {
                PhysicalObject trueVillain = null;
                var suspect = room.updateList[room.updateIndex];
                if (suspect is Explosion explosion) trueVillain = explosion.sourceObject;
                else if (suspect is PhysicalObject villainObject) trueVillain = villainObject;
                if(trueVillain != null)
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
                        (onlineVictim as OnlineCreature).CreatureViolence(onlineVillain, hitchunk.index, hitappendage, directionandmomentum, type, damage, stunbonus);
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
            if (!OnlinePhysicalObject.map.TryGetValue(grabbed.abstractPhysicalObject, out var onlineGrabbed)) throw new InvalidOperationException("Grabbed tjing doesn't exist in online space!");

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

        private void CreatureSuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
        {
            if (OnlineManager.lobby == null)
            {
                orig(self, entrancePos, carriedByOther);
                return;
            }

            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractCreature, out var onlineEntity))
            {
                Error($"Entity {self} - {self.abstractCreature.ID} doesn't exist in online space!");
                orig(self, entrancePos, carriedByOther);
                return;
            }

            var onlineCreature = (OnlineCreature)onlineEntity;

            if (onlineCreature.enteringShortCut) // If this call was from a processing event
            {
                orig(self, entrancePos, carriedByOther);
                onlineCreature.enteringShortCut = false;
            }
            else if (onlineCreature.isMine)
            {
                // tell everyone that I am about to enter a shortcut!
                onlineCreature.SuckedIntoShortCut(entrancePos, carriedByOther);
            }
            else
            {
                // Clear shortcut that it was meant to enter
                self.enteringShortCut = null;
            }
        }
    }
}
