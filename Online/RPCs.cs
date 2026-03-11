using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public static class RPCs
    {
        [RPCMethod]
        public static void DeathRain(RPCEvent rpc, GlobalRain.DeathRain.DeathRainMode deathRainMode, 
            float timeInThisMode, float calmBeforeStornSunlight)
        {
            if (rpc.from != OnlineManager.lobby.owner) return; // Only allow DeathRain from the host.
            if ((RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is not null))
            {
                if (game.globalRain.deathRain == null)
                {
                    game.globalRain.deathRain = new(game.globalRain);
                }
                game.globalRain.deathRain.deathRainMode = deathRainMode;
                if (deathRainMode == GlobalRain.DeathRain.DeathRainMode.Mayhem) return;

                if (deathRainMode == GlobalRain.DeathRain.DeathRainMode.GradeABuildUp)
                {
                    game.globalRain.ShaderLight = -1f;
                }

                game.globalRain.deathRain.progression = 0f;
                game.globalRain.deathRain.timeInThisMode = timeInThisMode;
                game.globalRain.deathRain.calmBeforeStormSunlight = calmBeforeStornSunlight;
            }
        }

        [RPCMethod]
        public static void Weapon_HitAnotherThrownWeapon(RPCEvent rpc, OnlinePhysicalObject weapon1, OnlinePhysicalObject weapon2)
        {
            if ((RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is not null))
            {
                if (weapon1.apo.realizedObject != null && weapon2.apo.realizedObject != null)
                {
                    (weapon1.apo.realizedObject as Weapon).HitAnotherThrownWeapon(weapon2.apo.realizedObject as Weapon);
                }
            }
        }


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
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
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

            if (saint != null && (opo.apo as AbstractCreature)?.realizedObject != null && (saint.apo as AbstractCreature)?.realizedCreature != null)
            {
                // Don't kill our friends!
                if ((saint.apo as AbstractCreature).realizedCreature.FriendlyFireSafetyCandidate((opo.apo as AbstractCreature).realizedCreature)) return;
            }

            (opo.apo as AbstractCreature)?.realizedCreature?.Die();
            if (saint != null)
            {
                DeathMessage.PvPRPC(saint.apo.realizedObject as Player, opo.apo.realizedObject as Creature, 1);
            }
        }

        [RPCMethod]
        public static void KillFeedEnvironment(OnlinePhysicalObject opo, int index, OnlinePhysicalObject? blame)
        {
            if (!(RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.manager.upcomingProcess is null)) return;
            OnlinePhysicalObject myKiller = null;
            OnlinePhysicalObject myTarget = null;
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo1 && opo1.apo is AbstractCreature ac)
                {
                    if (blame != null && opo1.id == blame.id)
                    {
                        myKiller = opo1;
                    }
                    if (opo1.id == opo.id)
                    {
                        myTarget = opo1;
                        if (blame == null) break;
                    }
                }
            }
            if (myTarget != null)
            {
                DeathMessage.DeathType type = (DeathMessage.DeathType)index;
                DeathMessage.EnvironmentalDeathMessage(opo, type, blame);
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
                DeathMessage.PvPContext pvpContext = Enum.IsDefined(typeof(DeathMessage.PvPContext), context) ? (DeathMessage.PvPContext)context : DeathMessage.PvPContext.Default;
                if ((target.apo as AbstractCreature).creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                {
                    DeathMessage.PlayerKillPlayer(myKiller, myTarget, pvpContext);
                }
                else
                {
                    DeathMessage.PlayerKillCreature(myKiller, myTarget, pvpContext);
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

        [RPCMethod]
        public static void TournamentValidation(RPCEvent rpc, string data)
        {
            if (!OnlineManager.lobby.owner.isMe) return;
            if (data.Length > 32767)
            {
                RainMeadow.Debug($"[Tournament Validation - {rpc.from}] Data was larger than 32kb, probably playing with disallowed mods.");
                return;
            }

            RainMeadow.Debug($"[Tournament Validation - {rpc.from}] {data}");
        }
    }
}
