using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public partial class WorldSession
    {
        private List<AbstractPhysicalObject> earlyApos = new(); // stuff that gets added during world loading

        // Something entered this resource, check if it needs registering
        public void ApoEnteringWorld(AbstractPhysicalObject apo)
        {
            if (!isAvailable) throw new InvalidOperationException("not available");
            if (!OnlinePhysicalObject.map.TryGetValue(apo, out var oe)) // New to me
            {
                if (isActive)
                {
                    RainMeadow.Debug($"{this} - registering {apo}");
                    oe = OnlinePhysicalObject.RegisterPhysicalObject(apo);
                }
                else // world population generates before this can be activated // can't we simply mark it as active earlier?
                {
                    RainMeadow.Debug($"{this} - queuing up for later {apo}");
                    this.earlyApos.Add(apo);
                    return;
                }
            }
            if (oe.owner.isMe && !oe.locallyEnteredResources.Contains(this)) // Under my control
            {
                oe.EnterResourceLocally(this);
            }
            // no error if already contained, our hooks are triggered multiple times
            // no action if remote
        }

        public void ApoLeavingWorld(AbstractPhysicalObject apo)
        {
            RainMeadow.Debug(this);
            if (OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
            {
                if (oe.owner.isMe)
                {
                    oe.LeaveResource(this);
                }
            }
            else
            {
                RainMeadow.Error("Unregistered entity leaving");
            }
        }






        public override void old_EntityEnteredResource(OnlineEntity oe)
        {
            base.old_EntityEnteredResource(oe);
            oe.worldSession = this;

            // not sure how "correct" this is because on the host side it might be different?
            if (!oe.owner.isMe) // kinda wanted a .isRemote helper at this point
            {
                oe.beingMoved = true;
                this.world.GetAbstractRoom(oe.enterPos).AddEntity(oe.entity);
                oe.beingMoved = false;
            }
        }

        public override void old_EntityLeftResource(OnlineEntity oe)
        {
            base.old_EntityLeftResource(oe);
            if (oe.worldSession == this) oe.worldSession = null;
        }
    }
}
