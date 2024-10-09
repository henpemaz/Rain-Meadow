using System;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        // customize creature behavior for online sync
        private void CreatureHooks()
        {
            On.OverseerAI.UpdateTempHoverPosition += OverseerAI_UpdateTempHoverPosition; // no teleporting
            On.OverseerAI.Update += OverseerAI_Update; // please look at what I tell you to

            On.AbstractPhysicalObject.Update += AbstractPhysicalObject_Update; // Don't think
            On.AbstractCreature.Update += AbstractCreature_Update; // Don't think
            On.AbstractCreature.OpportunityToEnterDen += AbstractCreature_OpportunityToEnterDen; // Don't think
            On.AbstractCreature.InDenUpdate += AbstractCreature_InDenUpdate; // Don't think

            On.ScavengerAbstractAI.InitGearUp += ScavengerAbstractAI_InitGearUp;
        }

        private void ScavengerAbstractAI_InitGearUp(On.ScavengerAbstractAI.orig_InitGearUp orig, ScavengerAbstractAI self)
        {
            if (OnlineManager.lobby != null)
            {
                if (self.world.GetResource() is WorldSession ws && !ws.isOwner)
                {
                    return;
                }
            }
            orig(self);
        }

        // Don't think
        private void AbstractCreature_InDenUpdate(On.AbstractCreature.orig_InDenUpdate orig, AbstractCreature self, int time)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    return;
                }
            }
            orig(self, time);
        }

        // Don't think
        private void AbstractCreature_OpportunityToEnterDen(On.AbstractCreature.orig_OpportunityToEnterDen orig, AbstractCreature self, WorldCoordinate den)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }
            orig(self, den);
        }

        // Don't think
        private void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    return;
                }
            }
            orig(self, time);
        }

        // Don't think
        private void AbstractPhysicalObject_Update(On.AbstractPhysicalObject.orig_Update orig, AbstractPhysicalObject self, int time)
        {
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    return;
                }
            }
            orig(self, time);
        }

        // overseers determine what they look at based on:
        // Random.range/value calls, a ton of state that would be a waste to sync,
        // who player 1 is (i think), and the location of stars in the sky.
        // so lets not let them choose for themselves.
        private void OverseerAI_Update(On.OverseerAI.orig_Update orig, OverseerAI self)
        {
            if (OnlineManager.lobby != null)
            {
                if (OnlinePhysicalObject.map.TryGetValue(self.overseer.abstractCreature, out var oe) && !oe.isMine)
                {
                    Vector2 tempLookAt = self.lookAt;
                    orig(self);
                    self.lookAt = tempLookAt;
                    return;
                }
            }
            orig(self);
        }

        // remote overseers have gotten their zipping permissions revoked.
        // we might also need to block ziptoposition, but i havent been able to test if thats an issue.
        private void OverseerAI_UpdateTempHoverPosition(On.OverseerAI.orig_UpdateTempHoverPosition orig, OverseerAI self)
        {
            if (OnlineManager.lobby != null)
            {
                if (OnlinePhysicalObject.map.TryGetValue(self.overseer.abstractCreature, out var oe) && !oe.isMine)
                {
                    return;
                }
            }
            orig(self);
        }
    }
}
