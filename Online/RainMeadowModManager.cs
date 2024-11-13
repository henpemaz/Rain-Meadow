using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace RainMeadow
{
    public class RainMeadowModManager
    {
        public static string[] GetActiveMods()
        {
            var highImpactMods = ModManager.ActiveMods
                .Where(mod => Directory.Exists(Path.Combine(mod.path, "modify", "world"))
                    || mod.id == "rwremix" // Remix needs to be added to the 'game-breaking' mods for game settings sync
                    || mod.id == "moreslugcats")
                .Select(mod => mod.id);

            return highImpactMods.ToArray();
        }

        internal static void CheckMods(string[] lobbyMods, string[] localMods) // both filtered by GetActiveMods
        {
            RainMeadow.Debug($"lobby mods: [ {string.Join(", ", lobbyMods)} ]");
            RainMeadow.Debug($"local mods: [ {string.Join(", ", localMods)} ]");
            if (Enumerable.SequenceEqual(localMods, lobbyMods))
            {
                RainMeadow.Debug("Matching mod set");
            }
            else if (lobbyMods.ToHashSet().SetEquals(localMods.ToHashSet()))
            {
                RainMeadow.Debug("Matching mod set, but mismatched order");
                // TODO: worry about this
            }
            else
            {
                RainMeadow.Debug("Mismatching mod set");

                List<bool> mods = ModManager.InstalledMods.ConvertAll(mod => mod.enabled);
                List<int> loadOrder = ModManager.InstalledMods.ConvertAll(mod => mod.loadOrder);

                List<ModManager.Mod> modsToEnable = new();
                List<ModManager.Mod> modsToDisable = new();
                List<string> unknownMods = new();

                foreach (var id in lobbyMods.Except(localMods))
                {
                    int index = ModManager.InstalledMods.FindIndex(mod => mod.id == id);

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

                foreach (var id in localMods.Except(lobbyMods))
                {
                    int index = ModManager.InstalledMods.FindIndex(mod => mod.id == id);

                    mods[index] = false;
                    modsToDisable.Add(ModManager.InstalledMods[index]);
                }

                ModApplier modApplyer = new(RWCustom.Custom.rainWorld.processManager, mods, loadOrder);

                modApplyer.ShowConfirmation(modsToEnable, modsToDisable, unknownMods);

                modApplyer.OnFinish += (ModApplier modApplyer) =>
                {
                    RainMeadow.Debug("Finished applying");

                    //Utils.Restart($"+connect_lobby {MatchmakingManager.instance.GetLobbyID()}"); 

                };
            }
        }

        internal static void Reset()
        {
            if (ModManager.MMF)
            {
                RainMeadow.Debug("Restoring config settings");

                var mmfOptions = MachineConnector.GetRegisteredOI(MoreSlugcats.MMF.MOD_ID);
                MachineConnector.ReloadConfig(mmfOptions);
            }
        }
    }
}
