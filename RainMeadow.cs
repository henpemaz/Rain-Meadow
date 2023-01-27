using BepInEx;
using MonoMod.Cil;
using System.Linq;
using System;
using Mono.Cecil.Cil;
using UnityEngine;
using System.Security.Permissions;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using System.Reflection;

[assembly: AssemblyVersion(RainMeadow.RainMeadow.MeadowVersionStr)]
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

            //pm already initialized lol
            self.processManager.sideProcesses.Add(new OnlineManager(self.processManager));

            On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues;
            On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues_LoadingContext += WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues_LoadingContext;

            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

            On.Room.ctor += Room_ctor;
            IL.RainWorldGame.ctor += RainWorldGame_ctor;
            IL.Room.LoadFromDataString += Room_LoadFromDataString;
            IL.Room.Loaded += Room_Loaded;
        }
    }
}
