using BepInEx;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Permissions;

[assembly: AssemblyVersion(RainMeadow.RainMeadow.MeadowVersionStr)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace RainMeadow
{
    [BepInPlugin("henpemaz.rainmeadow", "RainMeadow", MeadowVersionStr)]
    public partial class RainMeadow : BaseUnityPlugin
    {
        public const string MeadowVersionStr = "0.0.44";
        public static RainMeadow instance;
        private bool init;

        public void OnEnable()
        {
            instance = this;
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
                TestExpressions();
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
                RainMeadow.Debug($"MeadowProgression.InitializeBuiltinTypes: {sw.Elapsed}");


                self.processManager.sideProcesses.Add(new OnlineManager(self.processManager));

                MenuHooks();
                GameHooks();
                EntityHooks();
                ShortcutHooks();
                GameplayHooks();
                PlayerHooks();
                CustomizationHooks();
                
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        private void TestExpressions()
        {
           
            Stopwatch sw = Stopwatch.StartNew();
            var objParam = Expression.Parameter(typeof(TestObject), "objParam");
            var goodMethod = Expression.Lambda<Action<TestObject>>(Expression.Call(objParam, typeof(TestObject).GetMethod("GoodMethod")), objParam).Compile();
            var badMethod = Expression.Lambda<Action<TestObject>>(Expression.Call(objParam, typeof(TestObject).GetMethod("BadMethod")), objParam).Compile();
            sw.Stop();
            RainMeadow.Debug($"compiling: {sw.Elapsed}");

            sw = Stopwatch.StartNew();
            new TestObject().GoodMethod();
            sw.Stop();
            RainMeadow.Debug($"good method direct: {sw.Elapsed}");
            sw = Stopwatch.StartNew();
            new TestObject().GoodMethod();
            sw.Stop();
            RainMeadow.Debug($"good method direct 2: {sw.Elapsed}");
            sw = Stopwatch.StartNew();
            new TestObject().GoodMethod();
            sw.Stop();
            RainMeadow.Debug($"good method direct 3: {sw.Elapsed}");

            var lb = (TestObject obj) => obj.GoodMethod();
            sw = Stopwatch.StartNew();
            lb(new TestObject());
            sw.Stop();
            RainMeadow.Debug($"good method lambda: {sw.Elapsed}");
            sw = Stopwatch.StartNew();
            lb(new TestObject());
            sw.Stop();
            RainMeadow.Debug($"good method lambda 2: {sw.Elapsed}");
            sw = Stopwatch.StartNew();
            lb(new TestObject());
            sw.Stop();
            RainMeadow.Debug($"good method lambda 3: {sw.Elapsed}");

            sw = Stopwatch.StartNew();
            goodMethod(new TestObject());
            sw.Stop();
            RainMeadow.Debug($"good method et lambda: {sw.Elapsed}");
            sw = Stopwatch.StartNew();
            goodMethod(new TestObject());
            sw.Stop();
            RainMeadow.Debug($"good method et lambda 2: {sw.Elapsed}");
            sw = Stopwatch.StartNew();
            goodMethod(new TestObject());
            sw.Stop();
            RainMeadow.Debug($"good method et lambda 3: {sw.Elapsed}");

            //badMethod(new TestObject());
        }

        private class TestObject
        {
            public void GoodMethod()
            {
                //throw new Exception("gotcha");
            }

            public void BadMethod()
            {
                throw new Exception("gotcha");
            }
        }
    }
}
