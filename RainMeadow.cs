using BepInEx;
using MonoMod.Cil;
using System.Linq;
using System;
using Mono.Cecil.Cil;
using UnityEngine;
using System.Security.Permissions;
using BepInEx.Logging;
using System.Runtime.CompilerServices;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RainMeadow
{
    [BepInPlugin("henpemaz.rainmeadow", "RainMeadow", "0.0.1")]
    partial class RainMeadow : BaseUnityPlugin
    {
        static RainMeadow instance;
        public static ManualLogSource sLogger => instance.Logger;
        public static void Debug(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
        {
            callerFile = callerFile.Substring(callerFile.LastIndexOf('\\') + 1);
            callerFile = callerFile.Substring(0, callerFile.LastIndexOf('.'));
            instance.Logger.LogInfo($"{callerFile}.{callerName}:{data}");
        }
        public static void DebugMethodName([CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
        {
            callerFile = callerFile.Substring(callerFile.LastIndexOf('\\') + 1);
            callerFile = callerFile.Substring(0, callerFile.LastIndexOf('.'));
            instance.Logger.LogInfo($"{callerFile}.{callerName}");
        }

        public void OnEnable()
        {
            instance = this;
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.Update += RainWorld_Update;
        }

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
            //pm already initialized lol
            self.processManager.sideProcesses.Add(new OnlineManager(self.processManager));

            On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues;
            On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues_LoadingContext += WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues_LoadingContext;

            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

            IL.RainWorldGame.ctor += RainWorldGame_ctor;
            orig(self);
        }
    }
}
