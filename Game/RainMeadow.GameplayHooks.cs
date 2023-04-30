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
            On.Creature.Violence += CreatureOnViolence;
            On.Creature.Grasp.ctor += GraspOnctor;
            On.Creature.Grasp.Release += GraspOnRelease;
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
            if (!onlineGrabber.owner.isMe) return;
            
            if (!OnlineEntity.map.TryGetValue(grabbed.abstractPhysicalObject, out var onlineGrabbed)) throw new InvalidOperationException("Grabbed tjing doesn't exist in online space!");
            onlineGrabber.GraspRequest(onlineGrabbed, graspused, chunkgrabbed, shareability, dominance, pacifying);
        }
        
        private void GraspOnRelease(On.Creature.Grasp.orig_Release orig, Creature.Grasp self)
        {
            orig(self);
            if (OnlineManager.lobby == null) return;
            if (!OnlineEntity.map.TryGetValue(self.grabber.abstractPhysicalObject, out var onlineGrabber)) throw new InvalidOperationException("Grabber doesn't exist in online space!");

            onlineGrabber.GraspRelease(self.graspUsed);
        }
    }
}
