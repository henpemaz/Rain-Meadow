using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    partial class RainMeadow
    {
        public void GameplayHooks()
        {
            On.ShelterDoor.Close += ShelterDoorOnClose;
            On.Creature.Update += CreatureOnUpdate;
            On.Creature.Violence += CreatureOnViolence;
            On.Creature.Grasp.ctor += GraspOnctor;
            On.PhysicalObject.Grabbed += PhysicalObjectOnGrabbed;
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
            if (!OnlineEntity.map.TryGetValue(self.abstractPhysicalObject, out var onlineCreature)) throw new InvalidOperationException("Creature doesn't exist in online space!");
            if (!onlineCreature.owner.isMe) return;
            
            foreach (var grasp in self.grasps)
            {
                if (grasp == null) continue;
                if (!OnlineEntity.map.TryGetValue(grasp.grabbed.abstractPhysicalObject, out var onlineGrabbed)) throw new InvalidOperationException("Creature doesn't exist in online space!");
                if (!onlineGrabbed.owner.isMe && onlineGrabbed.isTransferable && !onlineGrabbed.isPending)
                {
                    if (grasp.grabbed is not Creature) // Non-Creetchers cannot be grabbed by multiple creatures
                    {
                        grasp.Release();
                        return;
                    }
                    
                    var grabbersOtherThanMe = grasp.grabbed.grabbedBy.Select(x => x.grabber).Where(x => x != self);
                    foreach (var grabbers in grabbersOtherThanMe)
                    {
                        if (!OnlineEntity.map.TryGetValue(grabbers.abstractPhysicalObject, out var tempEntity)) throw new InvalidOperationException("Creature doesn't exist in online space!");
                        if (!tempEntity.owner.isMe) return;
                    }
                    // If no remotes holding the entity, request it
                    onlineGrabbed.Request();
                }
            }
        }
        
        private void CreatureOnViolence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionandmomentum, BodyChunk hitchunk, PhysicalObject.Appendage.Pos hitappendage, Creature.DamageType type, float damage, float stunbonus)
        {
            if (OnlineManager.lobby == null) return;
            if (!OnlineEntity.map.TryGetValue(hitchunk.owner.abstractPhysicalObject, out var onlineVictim)) throw new InvalidOperationException("Victim doesn't exist in online space!");
            if (!onlineVictim.owner.isMe)
            {
                OnlineEntity onlineVillain = null;
                if (source != null && !OnlineEntity.map.TryGetValue(source.owner.abstractPhysicalObject, out onlineVillain)) throw new InvalidOperationException("Villain doesn't exist in online space!");

                onlineVictim.CreatureViolence(onlineVillain, hitchunk.index, hitappendage, directionandmomentum, type, damage, stunbonus);
                return;
            }
            
            orig(self, source, directionandmomentum, hitchunk, hitappendage, type, damage, stunbonus);
        }
        
        private void GraspOnctor(On.Creature.Grasp.orig_ctor orig, Creature.Grasp self, Creature grabber, PhysicalObject grabbed, int graspused, int chunkgrabbed, Creature.Grasp.Shareability shareability, float dominance, bool pacifying)
        {
            orig(self, grabber, grabbed, graspused, chunkgrabbed, shareability, dominance, pacifying);
            if (OnlineManager.lobby == null) return;
            if (!OnlineEntity.map.TryGetValue(grabber.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");
            if (!OnlineEntity.map.TryGetValue(grabbed.abstractPhysicalObject, out var onlineGrabbed)) throw new InvalidOperationException("Grabbed tjing doesn't exist in online space!");
            
            if (onlineGrabber.owner.isMe && !onlineGrabbed.owner.isMe && onlineGrabbed.isTransferable && !onlineGrabbed.isPending)
            {
                onlineGrabbed.Request();
            }
        }

        private void PhysicalObjectOnGrabbed(On.PhysicalObject.orig_Grabbed orig, PhysicalObject self, Creature.Grasp grasp)
        {
            orig(self, grasp);
            if (OnlineManager.lobby == null) return;
            if (!OnlineEntity.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidOperationException("Entity doesn't exist in online space!");
            if (!OnlineEntity.map.TryGetValue(grasp.grabber.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");
            
            if (!onlineEntity.isTransferable && onlineEntity.owner.isMe)
            {
                if (!onlineGrabber.owner.isMe && onlineGrabber.isTransferable && !onlineGrabber.isPending)
                {
                    onlineGrabber.Request(); // If I've been grabbed and I'm not transferrable, but my grabber is, request him
                }
            }
        }
    }
}
