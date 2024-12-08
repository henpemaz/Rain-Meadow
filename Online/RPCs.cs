using System;
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
        public static void UpdateUsernameTemporarily(RPCEvent rpc, string lastSentMessage)
        {
            string incomingUsername = rpc.from.id.name;
            RainMeadow.Debug("Incoming: " + incomingUsername + ": " + lastSentMessage);

            if (OnlineManager.lobby.gameMode.mutedPlayers.Contains(incomingUsername)) return;

            var game = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

            if (game == null) return;

            var onlineHuds = game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

            foreach (var onlineHud in onlineHuds)
            {
                foreach (var part in onlineHud.parts.OfType<OnlinePlayerDisplay>())
                {
                    if (part.player == rpc.from)
                    {
                        part.messageQueue.Enqueue(new OnlinePlayerDisplay.Message(lastSentMessage));

                        return;
                    }
                }
            }
        }

        [RPCMethod]
        public static void KickToLobby()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            try
            {
                game.ExitToMenu();
            }
            catch
            {
                RWCustom.Custom.rainWorld.processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
            BanHammer.ShowBan(RWCustom.Custom.rainWorld.processManager);
        }

        [RPCMethod]
        public static void ExitToGameModeMenu()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            game.manager.RequestMainProcessSwitch(OnlineManager.lobby.gameMode.MenuProcessId());
        }
    }
}
