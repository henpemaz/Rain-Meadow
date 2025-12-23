using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public partial class OverworldSession : OnlineResource
    {
        public class IDReservation
        {
            public int id;
            public readonly int end;

            public IDReservation(int id, int end)
            {
                this.id = id;
                this.end = end;
            }   
        }

        public List<IDReservation> entityIDReservations = new();
        public const int entityIDBatchSize = 100;
        public const int minEntityIDBatch = 50;
        public int latestReservation { get; private set; }


        public void ReserveEntityIDsIfNecessary()
        {
            if (entityIDReservations.FirstOrDefault() is not IDReservation reservation || (reservation.end - reservation.id) < minEntityIDBatch)
            {
                ReserveEntityIDs();
            }
            return;
        }

        public void ReserveEntityIDs()
        {
            RainMeadow.DebugMe();
            if (!isActive) throw new InvalidOperationException("not active");
            if (!isAvailable) throw new InvalidOperationException("not available");

            if (isOwner)
            {
                latestReservation += entityIDBatchSize;
                ResolveRequestedReserveIDs(null!, latestReservation - entityIDBatchSize, latestReservation);
            }
            else
            {
                if (owner is not null)
                {
                    owner.InvokeOnceRPC(RequestReserveIDs);
                }
            }
        }

        [RPCMethod(runDeferred = true)]
        public void RequestReserveIDs(RPCEvent rpcEvent)
        {
            if (!isOwner) throw new InvalidOperationException("not owner");
            if (isPending) throw new InvalidOperationException("pending");
            latestReservation += entityIDBatchSize;
            rpcEvent.from.InvokeRPC(ResolveRequestedReserveIDs, latestReservation - entityIDBatchSize, latestReservation);
        }


        [RPCMethod(runDeferred = true)]
        public void ResolveRequestedReserveIDs(RPCEvent rpcEvent, int id, int end)
        {
            RainMeadow.Debug($"got new reservation {id} -> {end}");
            if (rpcEvent != null)
            {
                if (isOwner) throw new InvalidOperationException("owner");
                if (isPending) throw new InvalidOperationException("pending");
                if (rpcEvent.from != owner) throw new InvalidOperationException("not owner");
            }

            foreach (IDReservation reservation in entityIDReservations)
            {
                if (EventMath.IsNewerOrEqual((uint)end, (uint)reservation.id) && 
                    EventMath.IsNewerOrEqual((uint)reservation.end, (uint) id))
                {
                    RainMeadow.Error($"reservation overlaps with {reservation.id} -> {reservation.end}");
                    return;
                }
            }

            entityIDReservations.Add(new IDReservation(id, end));
        }


        public void UpdateGameNextID()
        {
            OnlineManager.lobby.gameMode.clientSettings.nextID = overWorld.game.nextIssuedId;
            if (isOwner)
            {
                int biggestID = overWorld.game.nextIssuedId;
                foreach (int id in participants.Select(x => OnlineManager.lobby.clientSettings[x].nextID))
                {
                    if (!EventMath.IsNewer((uint)id, (uint)biggestID)) continue;
                    if (!EventMath.IsNewer((uint)id, (uint)latestReservation)) continue; //LIAR
                    biggestID = id;
                }

                overWorld.game.nextIssuedId = biggestID;
            }
        }
    }
}