using BepInEx;
using System;
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
        public const string MeadowVersionStr = "0.0.42";
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

            Logger.LogInfo("A");
            OnlineState.StateType.CreatureStateState = new OnlineState.StateType("asdf", true);

            try
            {
                OnlineState.RegisterState(OnlineState.StateType.CreatureStateState, typeof(CreatureStateState), OnlineState.DeltaSupport.FollowsContainer);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }

            Logger.LogInfo("B");
            try
            {
                var seri = new Serializer(1000);
                var state = new CreatureStateState();
                state.alive = true;
                state.meatLeft = 6;
                Logger.LogInfo("C");

                seri.BeginWrite(null);
                state.CustomSerialize(seri);
                Logger.LogInfo(seri.stream.ToString());
                Logger.LogInfo(seri.Position.ToString());

                Logger.LogInfo("D");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }

            try
            {
                var state = new CreatureStateState();
                state.alive = true;
                state.meatLeft = 6;
                var other = (CreatureStateState)state.DeepCopy();
                Logger.LogInfo(state.meatLeft);
                Logger.LogInfo(other.meatLeft);

                Logger.LogInfo("E");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
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
                self.processManager.sideProcesses.Add(new OnlineManager(self.processManager));

                MenuHooks();
                GameHooks();
                EntityHooks();
                ShortcutHooks();
                GameplayHooks();
                PlayerHooks();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }
    }
}
