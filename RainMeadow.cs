using BepInEx;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;

[assembly: AssemblyVersion(RainMeadow.RainMeadow.MeadowVersionStr)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RainMeadow
{
    [BepInPlugin("henpemaz.rainmeadow", "RainMeadow", MeadowVersionStr)]
    public partial class RainMeadow : BaseUnityPlugin
    {
        public const string MeadowVersionStr = "0.0.58";
        public static RainMeadow instance;
        private bool init;
        public static RainMeadowOptions rainMeadowOptions;


        public void OnEnable()
        {
            instance = this;
            rainMeadowOptions = new RainMeadowOptions(this);

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.Update += RainWorld_Update;
            On.WorldLoader.UpdateThread += WorldLoader_UpdateThread;
            On.RoomPreparer.UpdateThread += RoomPreparer_UpdateThread;
            On.WorldLoader.FindingCreaturesThread += WorldLoader_FindingCreaturesThread;
            On.WorldLoader.CreatingAbstractRoomsThread += WorldLoader_CreatingAbstractRoomsThread;
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
                //#if TRACING
                tracing |= Input.GetKeyDown("l");
                //#endif
                orig(self);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {


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
                StorySaveManager.InitializeStorySaves();
                sw.Stop();
                RainMeadow.Debug($"StorySaveManager.InitializeSaveFiles: {sw.Elapsed}");

                EmoteHandler.InitializeBuiltinTypes();

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

                MenuHooks();
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

                MeadowMusic.EnableMusic();

                self.processManager.sideProcesses.Add(new OnlineManager(self.processManager));

#if LOCAL_P2P
                if (!self.setup.startScreen)
                {
                    OnlineManager.lobby = new Lobby(new OnlineGameMode.OnlineGameModeType(LocalMatchmakingManager.localGameMode), OnlineManager.mePlayer, null);
                }
#endif
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }
    }
}
