using MonoMod.Cil;
using System.Linq;
using System;
using Mono.Cecil.Cil;
using UnityEngine;
using Menu;

namespace RainMeadow
{
    public static class OnlineHooks
    {
        private static event Action OnSteamConnected;
        
        public static void Apply()
        {
            On.RainWorld.Start += RainWorld_Start;
            On.SteamManager.Awake += SteamManager_Awake;
        }

        private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.ProcessManager.SwitchMainProcess += ProcessManager_SwitchMainProcess1;
            On.RainWorldSteamManager.ctor += RainWorldSteamManager_ctor;

            On.WorldLoader.ctor += WorldLoader_ctor;
            On.WorldLoader.Update += WorldLoader_Update;
            On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;
            On.World.LoadWorld += World_LoadWorld;

            IL.ProcessManager.SwitchMainProcess += ProcessManager_SwitchMainProcess;
            
            IL.RainWorldGame.ctor += RainWorldGame_ctor;
            IL.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
            IL.World.ctor += World_ctor;
            IL.RainWorldGame.Update += RainWorldGame_Update;
            IL.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;

            UnityEngine.Debug.LogError("OnlineHooks registered");
            orig(self);
        }

        private static void RainWorldSteamManager_ctor(On.RainWorldSteamManager.orig_ctor orig, RainWorldSteamManager self, ProcessManager manager)
        {
            orig(self, manager);
            manager.sideProcesses.Add(new OnlineManager(manager));
        }

        private static void World_LoadWorld(On.World.orig_LoadWorld orig, World self, int slugcatNumber, System.Collections.Generic.List<AbstractRoom> abstractRoomsList, int[] swarmRooms, int[] shelters, int[] gates)
        {
            orig(self, slugcatNumber, abstractRoomsList, swarmRooms, shelters, gates);
            if(self.game?.session is OnlineSession os)
            {
                os.saveState.regionStates[self.region.regionNumber] = new RegionState(os.saveState, self);
            }
        }

        private static void WorldLoader_Update(On.WorldLoader.orig_Update orig, object self)
        {
            if (self is WorldLoader wl && wl.game.session is OnlineSession os && os.worldSessions[wl.world.region].pendingOwnership)
            {
                os.Waiting();
                // abort somehow?
                return;
            }
            orig(self);
        }

        private static void WorldLoader_ctor(On.WorldLoader.orig_ctor orig, object self, RainWorldGame game, int playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            WorldLoader wl = self as WorldLoader;
            if (game != null && game.session is OnlineSession os0)
            {
                playerCharacter = os0.playerCharacter;
            }
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
            if (wl.game != null && wl.game.session is OnlineSession os)
            {
                lock(os.worldSessions)
                {
                    if (!os.worldSessions.ContainsKey(wl.world.region))
                    {
                        var ws = new WorldSession(os, os.me, region);
                        os.worldSessions[wl.world.region] = ws;
                        ws.RequestOwnership();
                    }
                }
            }
        }

        private static void WorldLoader_CreatingWorld(On.WorldLoader.orig_CreatingWorld orig, object self)
        {
            orig(self);
            WorldLoader wl = self as WorldLoader;
            if(wl.game != null && wl.game.session is OnlineSession os)
            {
                if (os.worldSessions[wl.world.region].owner == os.me)
                {
                    wl.GeneratePopulation(true);
                }
            }
        }

        private static void WorldLoader_GeneratePopulation(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);

                while(c.TryGotoNext(MoveType.Before,
                    i=>i.MatchLdfld<WorldLoader>("game"),
                    i=>i.MatchLdfld<RainWorldGame>("session"),
                    i=>i.MatchIsinst<StoryGameSession>(),
                    i=>i.MatchLdfld<StoryGameSession>("saveState")
                    ))
                {
                    var notstory = il.DefineLabel();
                    var skip = il.DefineLabel();
                    c.Index += 2;
                    c.Emit(OpCodes.Dup);
                    c.Emit(OpCodes.Isinst, typeof(StoryGameSession));
                    c.Emit(OpCodes.Brfalse, notstory);
                    c.Index += 2;
                    c.Emit(OpCodes.Br, skip);
                    c.MarkLabel(notstory);
                    c.Emit(OpCodes.Isinst, typeof(OnlineSession));
                    c.Emit<OnlineSession>(OpCodes.Ldfld, "saveState");
                    c.MarkLabel(skip);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void RainWorldGame_Update(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.Goto(il.Instrs[il.Instrs.Count - 1]);
                ILLabel notstory = null;
                c.GotoPrev(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_IsStorySession"),
                    i => i.MatchBrfalse(out notstory),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(0) || i.MatchDup(),
                    i => i.MatchLdfld<RainWorldGame>("updateAbstractRoom")
                    );

                c.Goto(notstory.Target);
                ILLabel skip = null;
                c.Prev.MatchBr(out skip);

                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame game) => { return game.isOnlineSession(); });
                ILLabel theelse = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, theelse);

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame game) => { game.getOnlineSession().Update(); });

                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(theelse);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void World_ctor(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.Goto(il.Instrs[il.Instrs.Count - 1]);
                c.GotoPrev(MoveType.After,
                    i => i.MatchStfld<World>("rainCycle")
                    );
                ILLabel skip = c.IncomingLabels.First();
                c.GotoPrev(MoveType.After, i => i.MatchBr(out var lab) && lab.Target == skip.Target);
                c.MoveAfterLabels();


                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((RainWorldGame game) => { return game.isOnlineSession(); });
                ILLabel theelse = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, theelse);


                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldc_R4, (float)(15f));
                c.Emit(OpCodes.Newobj, typeof(RainCycle).GetConstructor(new Type[] { typeof(World), typeof(float) }));
                c.Emit<World>(OpCodes.Stfld, "rainCycle");

                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(theelse);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void OverWorld_LoadFirstWorld(ILContext il)
        {
            try
            {
                var isVanilla = il.Method.Parameters[0].ParameterType.Name == "OverWorld";

                var c = new MonoMod.Cil.ILCursor(il);
                c.Goto(il.Instrs[il.Instrs.Count - 1]);
                c.GotoPrev(
                    i => i.MatchLdfld<StoryGameSession>("saveState"),
                    i => i.MatchLdfld<SaveState>("denPosition")
                    );
                ILLabel skip = null;
                c.GotoPrev(i => i.MatchBr(out skip));
                c.Index++;
                c.MoveAfterLabels();

                c.Emit(isVanilla ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                c.EmitDelegate((OverWorld self) => { return self.game.manager.menuSetup.startGameCondition == OnlineSession.EnumExt_OnlineSession.Online; });
                ILLabel story = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, story);
                c.Emit(OpCodes.Ldstr, "HI_C04");
                c.Emit(isVanilla ? OpCodes.Stloc_0 : OpCodes.Stloc_1); // CRS moment
                c.Emit(OpCodes.Ldc_I4_0);
                c.Emit(isVanilla ? OpCodes.Stloc_1 : OpCodes.Stloc_0);


                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(story);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void RainWorldGame_ctor(ILContext il)
        {
            try
            {
                // part 1 - create right session type
                var c = new MonoMod.Cil.ILCursor(il);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchNewobj<StoryGameSession>(),
                    i => i.MatchStfld<RainWorldGame>("session")
                    );
                var skip = c.IncomingLabels.Last();

                c.GotoPrev(i => i.MatchBr(skip));
                c.Index++;
                // we're right before story block here hopefully
                c.MoveAfterLabels();

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame self) => { return self.manager.menuSetup.startGameCondition == OnlineSession.EnumExt_OnlineSession.Online; });
                ILLabel story = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, story);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Newobj, typeof(OnlineSession).GetConstructor(new Type[] { typeof(RainWorldGame) }));
                c.Emit<RainWorldGame>(OpCodes.Stfld, "session");
                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(story);

                // part 2 - player create
                //ILLabel notstory = null;
                //ILLabel create = il.DefineLabel();
                //c.GotoNext(moveType: MoveType.Before,
                //    i => i.MatchLdarg(0),
                //    i => i.MatchCallOrCallvirt<RainWorldGame>("get_IsStorySession"),
                //    i => i.MatchBrfalse(out notstory),
                //    i => i.MatchLdarg(0),
                //    i => i.MatchCallOrCallvirt(out _),
                //    i => i.MatchLdstr("Slugcat")
                //    );
                // not sure what do
                // if story, or session host, create player
                // session join does not ?
                


                // part 3 - no breakie if no player/creatures
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdfld<AbstractRoom>("creatures"),
                    i => i.MatchLdcI4(0),
                    i => i.MatchCallOrCallvirt(out _),
                    i => i.MatchStfld<RoomCamera>("followAbstractCreature")
                    );

                skip = c.IncomingLabels.Last();
                c.GotoPrev(MoveType.After, i => i.MatchBrtrue(skip));
                c.Emit(OpCodes.Br, skip); // don't run desperate bit of code that follows creatures[0]

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void ProcessManager_SwitchMainProcess1(On.ProcessManager.orig_SwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if((self.currentMainLoop?.ID ?? ProcessManager.ProcessID.MainMenu) == ProcessManager.ProcessID.MainMenu)
            {
                OnSteamConnected = null;
            }
            orig(self, ID);
        }

        private static void SteamManager_Awake(On.SteamManager.orig_Awake orig, MonoBehaviour self)
        {
            orig(self);
            if (self is SteamManager sm && sm.m_bInitialized) OnlineHooks.OnSteamConnected?.Invoke();
        }

        private static void ProcessManager_SwitchMainProcess(MonoMod.Cil.ILContext il)
        {
            try
            {
                var c = new MonoMod.Cil.ILCursor(il);
                c.GotoNext(moveType: MoveType.Before,
                    i => i.MatchLdloc(0),
                    i => i.MatchBrfalse(out _),
                    i => i.MatchLdloc(0),
                    i => i.MatchLdarg(0)
                    );

                var l = c.MarkLabel();
                c.MoveBeforeLabels();
                var l2 = c.MarkLabel();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Newobj, typeof(LobbyMenu).GetConstructor(new Type[] { typeof(ProcessManager) }));
                c.Emit<ProcessManager>(OpCodes.Stfld, "currentMainLoop");
                c.Emit(OpCodes.Br, l);

                c.GotoPrev(i => i.MatchSwitch(out _));
                ILLabel to = null;
                c.GotoNext(MoveType.Before, o => o.MatchBr(out to));
                c.MoveBeforeLabels();
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((ProcessManager.ProcessID id) => { return id == LobbyMenu.EnumExt_LobbyMenu.LobbyMenu; });
                c.Emit(OpCodes.Brtrue, l2);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            float num3 = (self.CurrLang != InGameTranslator.LanguageID.Italian) ? 110f : 150f;
            var btn = new SimplerButton(self, self.pages[0], "Meadow", new Vector2(883f - num3 / 2f, 170f), new Vector2(num3, 30f));
            self.pages[0].subObjects.Add(btn);
            OnSteamConnected += () => { btn.buttonBehav.greyedOut = false; };
            btn.buttonBehav.greyedOut = !SteamManager.Initialized;
            btn.OnClick += (SimplerButton obj) => { self.manager.RequestMainProcessSwitch(LobbyMenu.EnumExt_LobbyMenu.LobbyMenu); };
        }
    }
}