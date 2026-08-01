using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {

        [RPCMethod]
        void CreatureGrabRPC(OnlineEntity.EntityId creatureID, GraspRef graspRef)
        {
            if (creatureID.FindEntity() is not OnlineCreature oc) return;
            if (oc.realizedCreature is null) return;
            if (graspRef.onlineGrabbed.FindEntity() is not OnlinePhysicalObject obj) return;
            if (obj.apo.realizedObject is null) return;

            graspRef.MakeGrasp(oc.realizedCreature, obj.apo.realizedObject);
        }

        // Stowaway

        [RPCMethod]
        void StowawayHeadAttackRPC(OnlinePhysicalObject crit, byte headIndex)
        {
            if (crit.apo.realizedObject is not MoreSlugcats.StowawayBug stowaway) return;

            stowaway.headFired[headIndex] = true;
            stowaway.heads[headIndex].retractFac = 0f;
            stowaway.room.PlaySound(SoundID.Big_Spider_Spit, stowaway.firstChunk);
            stowaway.room.PlaySound(SoundID.Red_Lizard_Spit_Hit_NPC, stowaway.firstChunk);
        }

        [RPCMethod]
        void StowawayBiteRPC(OnlinePhysicalObject crit, byte killerBiteAndDead)
        {
            bool killerBite = killerBiteAndDead != 0;
            bool isCreatureDead = killerBiteAndDead == 2;

            if (crit.apo.realizedObject is not MoreSlugcats.StowawayBug stowaway) return;

            stowaway.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, stowaway.firstChunk);

            for (int n = UnityEngine.Random.Range(1, 5); n > 0; n--)
            {
                stowaway.room.AddObject(new WaterDrip(stowaway.bodyChunks[1].pos, RWCustom.Custom.DirVec(stowaway.firstChunk.pos, stowaway.bodyChunks[1].pos) * 10f + RWCustom.Custom.RNV(), true));
            }

            var stowawayGraphics = (MoreSlugcats.StowawayBugGraphics)stowaway.graphicsModule;

            if (killerBite)
            {
                stowawayGraphics.KillerBite();
                if (isCreatureDead)
                {
                    stowawayGraphics.digestPrey += .01f;
                    for (int i = UnityEngine.Random.Range(4, 8); i > 0; i--)
                    {
                        stowaway.room.AddObject(new WaterDrip(stowaway.bodyChunks[1].pos, default(UnityEngine.Vector2) + RWCustom.Custom.RNV(), true));
                    }
                    stowaway.LoseAllGrasps();
                    stowaway.room.PlaySound(SoundID.Bro_Digestion_Init, stowaway.firstChunk);
                }
                stowaway.room.PlaySound(SoundID.Lizard_Jaws_Grab_Player, stowaway.firstChunk);
            }
            else
            {
                stowawayGraphics.Bite();
            }
        }

        // BoxWorm

        [RPCMethod]
        void AskForOnlineLarva(RPCEvent rpc, OnlinePhysicalObject opo, byte index)
        {
            if (!opo.isMine) return;
            if (opo.apo.realizedObject is not Watcher.BoxWorm boxWorm) return;

            var larvaHolder = boxWorm.larvaHolders[index];

            if (!larvaHolder.hasLarva) return;
            
            if(boxWorm.larvaHolders[index].larva.abstractPhysicalObject.GetOnlineObject() is OnlinePhysicalObject onlineLarva)
                rpc.from.InvokeRPC(SendOnlineLarva, opo, onlineLarva, index);
        }

        [RPCMethod]
        void SendOnlineLarva(OnlinePhysicalObject onlineBoxWorm, OnlinePhysicalObject onlineLarva, byte index)
        {
            if (onlineBoxWorm.apo.realizedObject is not Watcher.BoxWorm boxWorm) return;
            if (onlineLarva.apo.realizedObject is not Watcher.BoxWorm.Larva larva) return;

            var larvaHolder = boxWorm.larvaHolders[index];

            larvaHolder.hasLarva = true;
            larvaHolder.abstractLarva = new Watcher.BoxWorm.Larva.AbstractLarva(larvaHolder.room.world, null, larvaHolder.room.GetWorldCoordinate(larvaHolder.position), larvaHolder.room.game.GetNewID());
            larvaHolder.abstractLarva.realizedObject = larva;
        }
    }
}