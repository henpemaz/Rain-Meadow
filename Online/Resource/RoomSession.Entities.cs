using System;

namespace RainMeadow
{
    public partial class RoomSession
    {
        // Something entered this resource, check if it needs registering
        public void ApoEnteringRoom(AbstractPhysicalObject apo, WorldCoordinate pos)
        {
            if (!isAvailable || !isActive) return; // don't need to queue, easy enough to list on activation
            if (!OnlineManager.lobby.gameMode.ShouldSyncAPOInRoom(this, apo)) return;
            if (OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
            {
                oe.EnterResource(this);
            }
            else if (OnlineManager.lobby.gameMode.ShouldRegisterAPO(this, apo))
            {
                RainMeadow.Debug($"{this} - registering {apo}");
                oe = OnlinePhysicalObject.RegisterPhysicalObject(apo);
                oe.EnterResource(this);
            }
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
                if (OnlineManager.lobby.gameMode.ShouldSyncAPOInRoom(this, apo))
                {
                    RainMeadow.Error($"Unregistered entity leaving {this} : {apo} - {Environment.StackTrace}");
                    OnlinePhysicalObject.RegisterAndCleanOutRemoteEntity(apo, this);
                }
            }
        }
    }
}
