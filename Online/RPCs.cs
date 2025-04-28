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
            if(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
            {
                foreach (var onlineHud in game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>())
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
        }

        [RPCMethod]
        public static void KickToLobby(RPCEvent rpc)
        {
            RainMeadow.Debug($"{rpc.from} is trying to kick {rpc.to}");
            if (OnlineManager.lobby.owner != rpc.from) return; // Only respond if its the host kicking the player
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
            OnlineManager.LeaveLobby();
        }

        [RPCMethod]
        public static void ExitToGameModeMenu()
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            game.manager.RequestMainProcessSwitch(OnlineManager.lobby.gameMode.MenuProcessId());
        }

        [RPCMethod]
        public static void Creature_Die(OnlinePhysicalObject opo, OnlinePhysicalObject saint)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;

            (opo.apo as AbstractCreature)?.realizedCreature?.Die();
            if (saint != null)
            {
                DeathMessage.PvPRPC(saint.apo.realizedObject as Player, opo.apo.realizedObject as Creature, 1);
            }
        }

        [RPCMethod]
        public static void KillFeedEnvironment(OnlinePhysicalObject opo, int index)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.apo is AbstractCreature ac)
                {
                    if (opo1.id == opo.id)
                    {
                        DeathMessage.DeathType type = (DeathMessage.DeathType)index;
                        DeathMessage.EnvironmentalDeathMessage(opo, type);
                        break;
                    }
                }
            }
        }

        [RPCMethod]
        public static void KillFeedPvP(OnlinePhysicalObject killer, OnlinePhysicalObject target, int context)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            OnlinePhysicalObject myKiller = null;
            OnlinePhysicalObject myTarget = null;
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.apo is AbstractCreature ac)
                {
                    if (opo1.id == killer.id)
                    {
                        myKiller = opo1;
                    }
                    if (opo1.id == target.id)
                    {
                        myTarget = opo1;
                    }
                }
            }
            if (myKiller != null)
            {
                if (myTarget == null && target.id.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.apo is AbstractCreature ac)
                {
                    myTarget = opo1;
                }
                if ((target.apo as AbstractCreature).creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                {
                    DeathMessage.PlayerKillPlayer(myKiller, myTarget, context);
                } 
                else
                {
                    DeathMessage.PlayerKillCreature(myKiller, myTarget, context);
                }
            }
        }
        [RPCMethod]
        public static void KillFeedCvP(OnlinePhysicalObject killer, OnlinePhysicalObject target)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            OnlinePhysicalObject myTarget = null;
            OnlinePhysicalObject myKiller = null;
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.apo is AbstractCreature)
                {
                    if (opo1.id == target.id)
                    {
                        myTarget = opo1;
                        break;
                    }
                }
            }
            if (killer.id.FindEntity(true) is OnlinePhysicalObject opo2 && opo2.apo is AbstractCreature)
            {
                myKiller = opo2;
            }
            DeathMessage.CreatureKillPlayer(myKiller, myTarget);
        }
    }
}
