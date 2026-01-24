using BepInEx;
using Newtonsoft.Json.Linq;
using RainMeadow.Game;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.Networking;

[assembly: AssemblyVersion(RainMeadow.RainMeadow.MeadowVersionStr)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RainMeadow
{
    [BepInPlugin("henpemaz.rainmeadow", "RainMeadow", MeadowVersionStr)]
    public partial class RainMeadow : BaseUnityPlugin
    {
        public const string MeadowVersionStr = "0.1.10.0";
        public const string ReleaseUrl = "https://api.github.com/repos/henpemaz/Rain-Meadow/releases/latest";
        public static string NewVersionAvailable = "";
        public static RainMeadow instance;
        private bool init;
        public bool fullyInit;
        public static RainMeadowOptions rainMeadowOptions;
        private PlopMachine PlopMachine;

        public void OnEnable()
        {
            instance = this;
            rainMeadowOptions = new RainMeadowOptions(this);

            if (AdvancedProfilingEnabled())
            {
                MeadowProfiler.FullPatch();
            }

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.ModManager.RefreshModsLists += ModManagerOnRefreshModsLists;
            On.RainWorld.Update += RainWorld_Update;
            On.WorldLoader.UpdateThread += WorldLoader_UpdateThread;
            On.RoomPreparer.UpdateThread += RoomPreparer_UpdateThread;
            On.WorldLoader.FindingCreaturesThread += WorldLoader_FindingCreaturesThread;
            On.WorldLoader.CreatingAbstractRoomsThread += WorldLoader_CreatingAbstractRoomsThread;

            On.RWCustom.Custom.Log += Custom_Log;
            On.RWCustom.Custom.LogImportant += Custom_LogImportant;
            On.RWCustom.Custom.LogWarning += Custom_LogWarning;

            DeathContextualizer.CreateBindings();

            StartCoroutine(CheckForUpdates());
        }

        private bool AdvancedProfilingEnabled()
        {
            foreach(var arg in Environment.GetCommandLineArgs())
            {
                if (arg == "-meadowprofiler") return true;
            }
            return false;
        }

        private void Custom_LogWarning(On.RWCustom.Custom.orig_LogWarning orig, string[] values)
        {
            Logger.LogWarning(string.Join(" ", values));
            orig(values);
        }

        private void Custom_LogImportant(On.RWCustom.Custom.orig_LogImportant orig, string[] values)
        {
            Logger.LogInfo(string.Join(" ", values));
            orig(values);
        }

        private void Custom_Log(On.RWCustom.Custom.orig_Log orig, string[] values)
        {
            Logger.LogInfo(string.Join(" ", values));
            orig(values);
        }

        private void WorldLoader_CreatingAbstractRoomsThread(On.WorldLoader.orig_CreatingAbstractRoomsThread orig, WorldLoader self)
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void WorldLoader_FindingCreaturesThread(On.WorldLoader.orig_FindingCreaturesThread orig, WorldLoader self)
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void RoomPreparer_UpdateThread(On.RoomPreparer.orig_UpdateThread orig, RoomPreparer self)
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void WorldLoader_UpdateThread(On.WorldLoader.orig_UpdateThread orig, WorldLoader self)
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
        {
            try
            {
                tracing = Input.GetKey("l");
                orig(self);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void ModManagerOnRefreshModsLists(On.ModManager.orig_RefreshModsLists orig, RainWorld rainworld)
        {
            orig(rainworld);

            try
            {
                RainMeadowModInfoManager.RefreshRainMeadowModInfos();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error loading Meadow mod info files:\n{e}\n{e.StackTrace}");
            }
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {
                MenuHooks(); //  sets the error message fallback

                MachineConnector.SetRegisteredOI("henpemaz_rainmeadow", rainMeadowOptions);

                UsernameGenerator.Timestamp = DateTime.Now.Ticks;

                var sw = Stopwatch.StartNew();
                OnlineState.InitializeBuiltinTypes();
                sw.Stop();
                RainMeadow.Debug($"OnlineState.InitializeBuiltinTypes: {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                OnlineGameMode.InitializeBuiltinTypes();
                sw.Stop();
                RainMeadow.Debug($"OnlineGameMode.InitializeBuiltinTypes: {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                MeadowProgression.InitializeBuiltinTypes();
                sw.Stop();
                RainMeadow.Debug($"MeadowProgression.InitializeBuiltinTypes: {sw.Elapsed}");

                sw = Stopwatch.StartNew();
                RPCManager.SetupRPCs();
                sw.Stop();
                RainMeadow.Debug($"RPCManager.SetupRPCs: {sw.Elapsed}");

                AssetBundle bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assetbundles/rainmeadow"));
                Shader[] newShaders = bundle.LoadAllAssets<Shader>();
                foreach (Shader shader in newShaders)
                {
                    RainMeadow.Debug("found shader " + shader.name);
                    var found = false;
                    foreach (FShader oldshader in self.Shaders.Values)
                    {
                        if (oldshader.shader.name == shader.name)
                        {
                            RainMeadow.Debug("replaced existing shader");
                            oldshader.shader = shader;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        RainMeadow.Debug("registered as new shader");
                        self.Shaders[shader.name] = FShader.CreateShader(shader.name, shader);
                    }
                }

                GameHooks();
                CreatureHooks();
                EntityHooks();
                ShortcutHooks();
                GameplayHooks();
                PlayerHooks();
                CustomizationHooks();
                MeadowHooks();
                LoadingHooks();
                StoryHooks();
                ArenaHooks();
                ItemHooks();
                ObjectHooks();
                JollyHooks();

                CapeManager.FetchCapes();

                MeadowMusic.EnableMusic();
                this.PlopMachine = new PlopMachine();
                this.PlopMachine.OnEnable();

                MeadowProgression.LoadProgression();

                self.processManager.sideProcesses.Add(new OnlineManager(self.processManager));
                fullyInit = true;

                // // Useful for finding where a Translate() method is missing
                // On.InGameTranslator.Translate += (origTranslate, translator, s) =>
                // {
                //     return "T";
                // };
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                fullyInit = false;
                //throw;
            }
        }

        IEnumerator CheckForUpdates()
        {
            JObject json = null;
            using (UnityWebRequest request = UnityWebRequest.Get(ReleaseUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    json = JObject.Parse(request.downloadHandler.text);
                } 
                else
                {
                    Logger.LogError($"A web request error occured whilst checking for updates: {request.result}");
                    yield break;
                }
            }
            if (json is null)
            {
                Logger.LogError($"A web request error occured whilst checking for updates: JSON returned no body.");
                yield break;
            }
            if (json.TryGetValue("tag_name", out var token))
            {
                string latestVersion = token.ToString();
                if (latestVersion.Count(f => f == '.') < 3)
                {
                    latestVersion = "0." + latestVersion;
                }
                RainMeadow.Debug($"Current Version - {MeadowVersionStr}, Latest Version - {latestVersion}");
                if (IsNewerVersion(latestVersion, MeadowVersionStr))
                {
                    RainMeadow.Debug($"NEW RAIN MEADOW VERSION FOUND.");
                    // One day grace window before users are prompted to update.
                    if (json.TryGetValue("published_at", out var published)
                        && DateTime.TryParse(published.ToString(), out var publishedDate)
                        && publishedDate.AddDays(1) > DateTime.Now)
                    {
                        RainMeadow.Debug($"Update popup grace period active until: {publishedDate.AddDays(1).ToLongDateString()}");
                        yield break;
                    }
                    NewVersionAvailable = latestVersion;
                    
                }
            }
        }

        // This logic could be improved a bit but it seems to work fine for now so I'll leave it be.
        public static bool IsNewerVersion(string newVersion, string currentVersion)
        {
            if (newVersion == currentVersion || string.IsNullOrWhiteSpace(newVersion) || string.IsNullOrWhiteSpace(currentVersion)) return false;

            string[] nParts = newVersion.Split('.');
            string[] cParts = currentVersion.Split('.');

            int length = Math.Max(nParts.Length, cParts.Length);

            for (int i = 0; i < length; i++)
            {
                int nPart = i < nParts.Length ? int.Parse(nParts[i]) : 0;
                int cPart = i < cParts.Length ? int.Parse(cParts[i]) : 0;

                if (nPart > cPart) return true; // newVersion is greater than currentVersion.
            }
            return false;
        }
    }
}
