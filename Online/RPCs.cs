using System.Linq;

namespace RainMeadow
{
    public static class RPCs
    {
        [RPCMethod]
        public static void DeltaReset(RPCEvent rpcEvent, OnlineResource onlineResource, OnlineEntity.EntityId entity)
        {
            RainMeadow.Debug($"from {rpcEvent.from} resource {onlineResource} entity {entity}");
            if (entity != null)
            {
                foreach (var feed in OnlineManager.feeds)
                {
                    if (feed.player == rpcEvent.from && feed.entity.id == entity && feed.resource == onlineResource)
                    {
                        feed.ResetDeltas();
                        return;
                    }
                }
            }
            else
            {
                foreach (var subscription in OnlineManager.subscriptions)
                {
                    if (subscription.player == rpcEvent.from && subscription.resource == onlineResource)
                    {
                        subscription.ResetDeltas();
                        return;
                    }
                }
            }
        }

        [RPCMethod]
        public static void AddFood(short add)
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).AddFood(add);
        }

        [RPCMethod]
        public static void AddQuarterFood()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).AddQuarterFood();
        }

        [RPCMethod]
        public static void AddReadyToWinPlayer(RPCEvent rpcEvent) {
            if (RainMeadow.isStoryMode(out var gameMode)) 
            { 
                gameMode.readyForWinPlayers.Add(rpcEvent.from.inLobbyId);
            }
        }

        [RPCMethod]
        public static void RemoveReadyToWinPlayer(RPCEvent rpcEvent)
        {
            if (RainMeadow.isStoryMode(out var gameMode))
            {
                gameMode.readyForWinPlayers.Remove(rpcEvent.from.inLobbyId);
            }
        }

        [RPCMethod]
        public static void RequestPlayerAvatar(RPCEvent rpcEvent, ushort inLobbyId) {
            if (!(rpcEvent.from.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.AddPlayerAvatar, inLobbyId, OnlineManager.lobby.playerAvatars[inLobbyId]))))
            {
                rpcEvent.from.InvokeRPC(RPCs.AddPlayerAvatar, inLobbyId, OnlineManager.lobby.playerAvatars[inLobbyId]);
            }
        }

        [RPCMethod]
        public static void AddPlayerAvatar(RPCEvent rpcEvent, ushort inLobbyId, OnlineCreature avatar)
        {
            if (avatar == null) {
                rpcEvent.from.InvokeRPC(RPCs.RequestPlayerAvatar, inLobbyId);
                return;
            }
            OnlineManager.lobby.playerAvatars[inLobbyId] = avatar;
        }

        [RPCMethod]
        public static void SetReadyForNextRegion(RPCEvent rpcEvent)
        {
            OnlineManager.lobby.isReadyForNextRegion = true;
        }
    }
}
