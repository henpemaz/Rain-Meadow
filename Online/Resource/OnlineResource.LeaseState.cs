using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        

        private void ProcessLease(IdentifiablesDeltaList leaseState)
        {
            RainMeadow.Debug(this);
            if(currentLeaseState != null)
            {
                currentLeaseState.AddDelta(leaseState);
            }
            else
            {
                if (leaseState.isDelta) { throw new InvalidOperationException("delta"); }
                this.currentLeaseState = leaseState;
            }
            if (leaseState is LobbyLeaseState lobbyLease) { UpdateParticipants(lobbyLease.players.list.Select(id => LobbyManager.instance.GetPlayer(id)).ToList()); }
            foreach (var item in currentLeaseState.sublease)
            {
                var resource = SubresourceFromShortId(item.resourceId);
                var itemOwner = LobbyManager.lobby.PlayerFromId(item.owner);
                if (resource.owner != itemOwner) resource.NewOwner(itemOwner);
                resource.UpdateParticipants(item.participants.list.Select(u=>LobbyManager.lobby.PlayerFromId(u)).ToList());
            }
        }

        

        
    }
}
