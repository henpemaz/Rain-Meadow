using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        // customize creature behavior for online sync
        private void ScavengerHooks()
        {
            On.ScavengerAbstractAI.InitGearUp += ScavengerAbstractAI_InitGearUp;
            On.ScavengerAbstractAI.ReGearInDen += ScavengerAbstractAI_ReGearInDen;
            On.Scavenger.TryThrow_BodyChunk_ViolenceType_Nullable1 += Scavenger_TryThrow_BodyChunk_ViolenceType_Nullable1;
            On.Scavenger.Throw += Scavenger_Throw;
            On.Scavenger.TryToMeleeCreature += Scavenger_TryToMeleeCreature;
            On.Scavenger.ArrangeInventory += Scavenger_ArrangeInventory;

            On.Scavenger.ForcedLookCreature += Scavenger_ForcedLookCreature;
        }

        public Tracker.CreatureRepresentation Scavenger_ForcedLookCreature(On.Scavenger.orig_ForcedLookCreature orig, global::Scavenger self)
        {
            if (!self.IsLocal())
            {
                return null!; // we'll recieve the lookcreature from state
            }

            return orig(self);
        }

        public bool Scavenger_ArrangeInventory(On.Scavenger.orig_ArrangeInventory orig, Scavenger self)
        {
            if (!self.IsLocal()) return false;
            return orig(self);
        }

        public void Scavenger_TryThrow_BodyChunk_ViolenceType_Nullable1(On.Scavenger.orig_TryThrow_BodyChunk_ViolenceType_Nullable1 orig, Scavenger self, BodyChunk aimChunk, ScavengerAI.ViolenceType violenceType, Vector2? aimPosition)
        {
            if (!self.IsLocal()) return;
            orig(self, aimChunk, violenceType, aimPosition);
        }


        public void Scavenger_Throw(On.Scavenger.orig_Throw orig, Scavenger self, Vector2 throwDir)
        {
            if (!self.IsLocal()) return;
            orig(self, throwDir);
        }

        public void Scavenger_TryToMeleeCreature(On.Scavenger.orig_TryToMeleeCreature orig, Scavenger self)
        {
            if (!self.IsLocal()) return;
            orig(self);
        }

        private void ScavengerAbstractAI_InitGearUp(On.ScavengerAbstractAI.orig_InitGearUp orig, ScavengerAbstractAI self)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.creatingRemoteObject) return;
            orig(self);
        }

        private void ScavengerAbstractAI_ReGearInDen(On.ScavengerAbstractAI.orig_ReGearInDen orig, ScavengerAbstractAI self)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.creatingRemoteObject) return;
            orig(self);
        }
    }
}