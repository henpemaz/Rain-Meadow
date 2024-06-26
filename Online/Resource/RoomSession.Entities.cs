﻿using System;

namespace RainMeadow
{
    public partial class RoomSession
    {
        // Something entered this resource, check if it needs registering
        public void ApoEnteringRoom(AbstractPhysicalObject apo, WorldCoordinate pos)
        {
            if (!isAvailable || !isActive) return; // don't need to queue, easy enough to list on activation
            if (!OnlinePhysicalObject.map.TryGetValue(apo, out var oe)) // New to me
            {
                RainMeadow.Debug($"{this} - registering {apo}");
                oe = OnlinePhysicalObject.RegisterPhysicalObject(apo);
            }
            oe.EnterResource(this);
        }

        public void ApoLeavingRoom(AbstractPhysicalObject apo)
        {
            if (!isAvailable || !isActive) return;
            if (OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
            {
                oe.ExitResource(this);
            }
            else
            {
                RainMeadow.Error($"Unregistered entity leaving {this} : {apo} - {Environment.StackTrace}");
            }
        }
    }
}
