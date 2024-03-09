
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
        public static void AddMushroomCounter()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).mushroomCounter += 320;
        }
        [RPCMethod]
        public static void ReinforceKarma()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma = true;
        }
        [RPCMethod]
        public static void PlayReinforceKarmaAnimation() 
        {
            (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame).cameras[0].hud.karmaMeter.reinforceAnimation = 0;
        }
        [RPCMethod]
        public static void InitGameOver()
        {
            var player = ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0]);
            (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.cameras[0].hud.InitGameOverMode(null, 0, player.pos.room, new UnityEngine.Vector2(0f, 0f));
        }
        [RPCMethod]
        public static void MovePlayersToDeathScreen() {
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                player.InvokeRPC(RPCs.GoToDeathScreen);
            }
        }
        [RPCMethod]
        public static void GoToDeathScreen()
        {
            var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
            if (game.manager.upcomingProcess != null)
            {
                return;
            }
            if (game.IsStorySession && game.GetStorySession.RedIsOutOfCycles && !game.rainWorld.ExpeditionMode)
            {
                game.GoToRedsGameOver();
                return;
            }
            game.GetStorySession.saveState.SessionEnded(game, false, false);
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.DeathScreen);
        }

        [RPCMethod]
        internal static List<Configurable<bool>> GetStoryRemixSettings()
        {
            List<Configurable<bool> > configurables = new List<Configurable<bool>>();

            if (ModManager.MMF)
            {

                configurables.Add(MoreSlugcats.MMF.cfgAlphaRedLizards);
                configurables.Add(MoreSlugcats.MMF.cfgBreathTimeVisualIndicator);
                configurables.Add(MoreSlugcats.MMF.cfgClearerDeathGradients);
                configurables.Add(MoreSlugcats.MMF.cfgClimbingGrip);
                configurables.Add(MoreSlugcats.MMF.cfgCreatureSense);
                configurables.Add(MoreSlugcats.MMF.cfgDeerBehavior);
                configurables.Add(MoreSlugcats.MMF.cfgDisableGateKarma);
                configurables.Add(MoreSlugcats.MMF.cfgDisableScreenShake);
                configurables.Add(MoreSlugcats.MMF.cfgExtraLizardSounds);
                configurables.Add(MoreSlugcats.MMF.cfgExtraTutorials);
                configurables.Add(MoreSlugcats.MMF.cfgFasterShelterOpen);
                configurables.Add(MoreSlugcats.MMF.cfgFastMapReveal);
                configurables.Add(MoreSlugcats.MMF.cfgFreeSwimBoosts);
                configurables.Add(MoreSlugcats.MMF.cfgGlobalMonkGates);
                configurables.Add(MoreSlugcats.MMF.cfgGraspWiggling);
                configurables.Add(MoreSlugcats.MMF.cfgHideRainMeterNoThreat);
                configurables.Add(MoreSlugcats.MMF.cfgHunterBackspearProtect);
                configurables.Add(MoreSlugcats.MMF.cfgHunterBatflyAutograb);
                //configurables.Add(MoreSlugcats.MMF.cfgHunterBonusCycles);
                //configurables.Add(MoreSlugcats.MMF.cfgHunterCycles);
                configurables.Add(MoreSlugcats.MMF.cfgIncreaseStuns);
                configurables.Add(MoreSlugcats.MMF.cfgJetfishItemProtection);
                configurables.Add(MoreSlugcats.MMF.cfgKeyItemPassaging);
                configurables.Add(MoreSlugcats.MMF.cfgKeyItemTracking);
                configurables.Add(MoreSlugcats.MMF.cfgLargeHologramLight);
                configurables.Add(MoreSlugcats.MMF.cfgLoadingScreenTips);
                configurables.Add(MoreSlugcats.MMF.cfgMonkBreathTime);
                configurables.Add(MoreSlugcats.MMF.cfgNewDynamicDifficulty);
                configurables.Add(MoreSlugcats.MMF.cfgNoArenaFleeing);
                configurables.Add(MoreSlugcats.MMF.cfgNoRandomCycles);
                configurables.Add(MoreSlugcats.MMF.cfgOldTongue);
                configurables.Add(MoreSlugcats.MMF.cfgQuieterGates);
                // configurables.Add(MoreSlugcats.MMF.cfgRainTimeMultiplier);
                configurables.Add(MoreSlugcats.MMF.cfgSafeCentipedes);
                configurables.Add(MoreSlugcats.MMF.cfgSandboxItemStems);
                configurables.Add(MoreSlugcats.MMF.cfgScavengerKillSquadDelay);
                configurables.Add(MoreSlugcats.MMF.cfgShowUnderwaterShortcuts);
                //configurables.Add(MoreSlugcats.MMF.cfgSlowTimeFactor);
                configurables.Add(MoreSlugcats.MMF.cfgSpeedrunTimer);
                configurables.Add(MoreSlugcats.MMF.cfgSurvivorPassageNotRequired);
                configurables.Add(MoreSlugcats.MMF.cfgSwimBreathLeniency);
                configurables.Add(MoreSlugcats.MMF.cfgThreatMusicPulse);
                configurables.Add(MoreSlugcats.MMF.cfgTickTock);
                configurables.Add(MoreSlugcats.MMF.cfgUpwardsSpearThrow);
                configurables.Add(MoreSlugcats.MMF.cfgVanillaExploits);
                configurables.Add(MoreSlugcats.MMF.cfgVulnerableJellyfish);
                configurables.Add(MoreSlugcats.MMF.cfgWallpounce);


                foreach (var setting in configurables)
                {
                    RainMeadow.Debug($"SETTING NAME: {setting.key}, Value: {setting.Value}");
                }
                return configurables;

            }
            return configurables;
        }
    }
}
