using BepInEx;
using System;
using System.Security.Permissions;
using System.Reflection;

[assembly: AssemblyVersion(RainMeadow.RainMeadow.MeadowVersionStr)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RainMeadow
{
    [BepInPlugin("henpemaz.rainmeadow", "RainMeadow", MeadowVersionStr)]
    partial class RainMeadow : BaseUnityPlugin
    {

        public const string MeadowVersionStr = "0.0.1";
        static RainMeadow instance;
        private bool init;

        public void OnEnable()
        {
            instance = this;
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.Update += RainWorld_Update;
        }

        // Debug
        private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
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

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {
                MenuHooks();
                if (SteamManager.Initialized)
                {
                    PlayersManager.InitPlayersManager();
                    LobbyManager.InitLobbyManager();
                    self.processManager.sideProcesses.Add(new OnlineManager(self.processManager));
                    
                    GameHooks();
                    EntityHooks();
                    GameplayHooks();
                }
                else
                {
                    Error("Steam is required to play this mod");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }
    }
}
