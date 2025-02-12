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
            if ((RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is not null))
            {
                if (RWCustom.Custom.rainWorld.processManager.musicPlayer != null)
                {
                    RWCustom.Custom.rainWorld.processManager.musicPlayer.DeathEvent();
                }

                game.ExitGame(asDeath: true, asQuit: true);
            }
            RWCustom.Custom.rainWorld.processManager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
            BanHammer.ShowBan(RWCustom.Custom.rainWorld.processManager);
        }

        [RPCMethod]
        public static void ExitToGameModeMenu()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            game.manager.RequestMainProcessSwitch(OnlineManager.lobby.gameMode.MenuProcessId());
        }

        [RPCMethod]
        public static void Creature_Die(OnlinePhysicalObject opo)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            (opo.apo as AbstractCreature)?.realizedCreature?.Die();
        }

        [RPCMethod]
        public static void KillFeedEnvironment(OnlinePhysicalObject opo, int index)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            if ((opo.apo as AbstractCreature)?.realizedCreature == null) return;
            DeathMessage.DeathType type = (DeathMessage.DeathType)index;
            DeathMessage.EnvironmentalDeathMessage((opo.apo as AbstractCreature)?.realizedCreature as Player, type);
        }

        [RPCMethod]
        public static void KillFeedPvP(OnlinePhysicalObject killer, OnlinePhysicalObject target)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            if ((killer.apo as AbstractCreature)?.realizedCreature == null || (target.apo as AbstractCreature)?.realizedCreature == null) return;
            if ((target.apo as AbstractCreature)?.realizedCreature is Player)
            {
                DeathMessage.PlayerKillPlayer((killer.apo as AbstractCreature)?.realizedCreature as Player, (target.apo as AbstractCreature)?.realizedCreature as Player);
            } 
            else
            {
                DeathMessage.PlayerKillCreature((killer.apo as AbstractCreature)?.realizedCreature as Player, (target.apo as AbstractCreature)?.realizedCreature);
            }
            
        }
        [RPCMethod]
        public static void KillFeedCvP(OnlinePhysicalObject killer, OnlinePhysicalObject target)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            if ((killer.apo as AbstractCreature)?.realizedCreature == null || (target.apo as AbstractCreature)?.realizedCreature == null) return;
            DeathMessage.CreatureKillPlayer((killer.apo as AbstractCreature)?.realizedCreature, (target.apo as AbstractCreature)?.realizedCreature as Player);
        }
    }
}
