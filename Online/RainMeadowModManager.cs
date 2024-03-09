using IL.MoreSlugcats;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreSlugcats;
using System.Reflection;
using Mono.Cecil;
using static RainMeadow.Lobby;
using static RainMeadow.OnlineResource;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace RainMeadow
{
    public class RainMeadowModManager
    {
        public static string[] GetActiveMods()
        {
            return ModManager.ActiveMods.Where(mod => Directory.Exists(Path.Combine(mod.path, "modify", "world"))).ToList().Select(mod => mod.id.ToString()).ToArray();
        }

        internal static Dictionary<string, bool> GetSettings()
        {
            Dictionary<string, bool> configurables = new Dictionary<string, bool>();

            if (ModManager.MMF)
            {
                configurables.Add(MoreSlugcats.MMF.cfgAlphaRedLizards.key, MoreSlugcats.MMF.cfgBreathTimeVisualIndicator.Value);
                configurables.Add(MoreSlugcats.MMF.cfgBreathTimeVisualIndicator.key, MoreSlugcats.MMF.cfgBreathTimeVisualIndicator.Value);
                /* configurables.Add(MoreSlugcats.MMF.cfgClearerDeathGradients);
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
                 configurables.Add(MoreSlugcats.MMF.cfgWallpounce);*/


                foreach (var setting in configurables)
                {
                    RainMeadow.Debug($"NAME: {setting.Key}, Value: {setting.Value}");
                }
                return configurables;

            }
            return configurables;
        }

        internal static void CheckSettings(Dictionary<string, bool> lobbySettings, Dictionary<string, bool> localSettings)
        {


            foreach (var setting in localSettings)
            {
                RainMeadow.Debug($"LOCAL: {setting.Key}, Value: {setting.Value}");
            }


            foreach (var setting in lobbySettings)
            {
                RainMeadow.Debug($"LOBBY: {setting.Key}, Value: {setting.Value}");
            }


        }


        internal static void CheckMods(string[] lobbyMods, string[] localMods)
        {
            if (Enumerable.SequenceEqual(localMods, lobbyMods))
            {
                RainMeadow.Debug("Same mod set !");
            }
            else
            {
                RainMeadow.Debug("Mismatching mod set");

                var (MissingMods, ExcessiveMods) = CompareModSets(lobbyMods, localMods);

                bool[] mods = ModManager.InstalledMods.ConvertAll(mod => mod.enabled).ToArray();

                List<int> loadOrder = ModManager.InstalledMods.ConvertAll(mod => mod.loadOrder);

                List<string> unknownMods = new();
                List<ModManager.Mod> modsToEnable = new();
                List<ModManager.Mod> modsToDisable = new();

                foreach (var id in MissingMods)
                {
                    int index = ModManager.InstalledMods.FindIndex(_mod => _mod.id == id);

                    if (index >= 0)
                    {
                        mods[index] = true;
                        modsToEnable.Add(ModManager.InstalledMods[index]);
                    }
                    else
                    {
                        RainMeadow.Debug("Unknown mod: " + id);
                        unknownMods.Add(id);
                    }
                }

                foreach (var id in ExcessiveMods)
                {
                    int index = ModManager.InstalledMods.FindIndex(_mod => _mod.id == id);

                    mods[index] = false;

                    modsToDisable.Add(ModManager.InstalledMods[index]);
                }

                ModApplier modApplyer = new(RWCustom.Custom.rainWorld.processManager, mods.ToList(), loadOrder);

                modApplyer.ShowConfirmation(modsToEnable, modsToDisable, unknownMods);

                modApplyer.OnFinish += (ModApplier modApplyer) =>
                {
                    Utils.Restart($"+connect_lobby {MatchmakingManager.instance.GetLobbyID()}");
                };
            }
        }

        private static (List<string> MissingMods, List<string> ExcessiveMods) CompareModSets(string[] arr1, string[] arr2)
        {
            // Find missing strings in arr2
            var missingStrings = arr1.Except(arr2).ToList();

            // Find excessive strings in arr2
            var excessiveStrings = arr2
                .GroupBy(item => item)
                .Where(group => group.Count() > arr1.Count(item => item == group.Key))
                .Select(group => group.Key)
                .ToList();

            return (missingStrings, excessiveStrings);
        }
    }
}
