using BepInEx;
using Menu;
using RainMeadow.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using UnityEngine;

[assembly: AssemblyVersion(RainMeadow.RainMeadow.MeadowVersionStr)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RainMeadow
{
    [BepInPlugin("henpemaz.rainmeadow", "RainMeadow", MeadowVersionStr)]
    public partial class RainMeadow : BaseUnityPlugin
    {
        public const string MeadowVersionStr = "0.1.7.1";
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


        private static HashSet<string> devSteamIdHashes = new HashSet<string>()
        {
            "AOOTy8PrB9DWbAxExg9BbhiLBbRqAgmsRLoAHnIGXOU=",
            "dlWUAGjYBtAdypcmLwbDnZ73akq624OiSNIQ//ecsms=",
            "ApNKog4MYwp7nfkyC6lIPtD+/sBJfBArnSPiy6yo7VU=",
            "YIczH+KncjxdHf3MrnhumDUJ1QVAyBsy9ME6k0bZyPc=",
            "P5S1c63jYWl3Ce73H0k99BeIMSAmxa/BbvkEiyTs9mM=",
            "iJFBCXhwwaHxbJ5uXfmZsK7Ad9a7vZgT1ZwiofO0aMg=",
            "ATe23LFNxITCICTkw+2Bs67cNZ5N/nRBMfziGhIn11s=",
            "oz6hibRdEiJow7IWhn+T7Ij+agHeNqmxyHO34YMOla4=",
            "E5mtN6Hh2vyAuOgBZ5iiTH36j2pAJ8urOgEZKZsciSo=",
            "TA9uZQ7Z7MkVUm7D32EB0gpuQBrhE9cAZWB2UXBuqtg=",
            "tXLLHFXRXKzi285CSDIko+gmRrLChLb3k3K1pV0GUq4=",
            "AkQKwH5S6zj//MRsnrjaTp2HGe7Ln9ZB057MP5xLk2M=",
            "tMoAaCdZejjuWCF0MsXcOUr+D4eok0b2c46B8PTM0kg=",
            "095dLJgw4Nc1zbdUIdxL7d7nmyKxcj7hekNx8EQlXGY=",
            "GpdPaLhUEEkwjCbkSLjXN7lZy0iXa5YlFErMi9V+hXI=",
            "PwcZS6t8kETyBdrPiR2ple35lpLMfEw6TP/VyHVD4z4=",
            "wZ2+Phw6EOBLv9bZKdSGV+3lWhNxiT2KHwCluqhLdzo=",
            "Hr8BfOHHTBRGgSmQoj4qQdlHqaY6d4DHFbF7wCNFI1U=",
            "cOL0sHXOvRyn7y5S+3VXWmuyZE1KvQXdfBgcHrph2kE=",
            "3aA5+Ga/lMY848/EcCZLBnO93TS1RhPfSMgAGtf7MQY=
            "5eD7MQy+i6B6862JCgkjFXRevE7UFU+kvvBGPXJ4hGQ="
        };

        public static bool IsDev(MeadowPlayerId player)
        {
            if (player is SteamMatchmakingManager.SteamPlayerId steamid)
            {
                ulong steamID = steamid.oid.GetSteamID64();
                SHA256 Sha = SHA256.Create();
                var steamIDHash = System.Convert.ToBase64String(Sha.ComputeHash(Encoding.ASCII.GetBytes(steamID.ToString())));

                if (devSteamIdHashes.Contains(steamIDHash))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
