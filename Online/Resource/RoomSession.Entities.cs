using System;
using System.Linq;

namespace RainMeadow
{
    public partial class RoomSession
    {
        // Something entered this resource, check if it needs registering
        public void ApoEnteringRoom(AbstractPhysicalObject apo, WorldCoordinate pos)
        {
            if (!isAvailable || !isActive) return;
            if (!OnlinePhysicalObject.map.TryGetValue(apo, out var oe)) // New to me
            {
                RainMeadow.Debug($"{this} - registering {apo}");
                oe = OnlinePhysicalObject.RegisterPhysicalObject(apo, pos);
            }
            if (oe.owner.isMe) // Under my control
            {
                oe.EnterResource(this);
            }
        }

        public void ApoLeavingRoom(AbstractPhysicalObject apo)
        {
            if (!isAvailable || !isActive) return;
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
    }
}
