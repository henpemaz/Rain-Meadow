using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{

    class LobbyMenu : Menu.Menu
    {
        public static void Apply()
        {
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.RainWorld.Start += RainWorld_Start;
        }

        private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);
            IL.ProcessManager.SwitchMainProcess += ProcessManager_SwitchMainProcess;
            IL.RainWorldGame.ctor += RainWorldGame_ctor;

            On.RainWorld.Start -= RainWorld_Start;
        }

        private static void RainWorldGame_ctor(ILContext il)
        {
            try
            {
                var c = new MonoMod.Cil.ILCursor(il);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchNewobj<StoryGameSession>(),
                    i => i.MatchStfld<RainWorldGame>("session")
                    );
                var skip = c.IncomingLabels.Last();

                c.GotoPrev(i=>i.MatchBr(skip));
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
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
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
                c.Emit(OpCodes.Newobj, typeof(LobbyMenu).GetConstructor(new Type[]{ typeof(ProcessManager) }));
                c.Emit<ProcessManager>(OpCodes.Stfld, "currentMainLoop");
                c.Emit(OpCodes.Br, l);

                c.GotoPrev(i => i.MatchSwitch(out _));
                ILLabel to = null;
                c.GotoNext(MoveType.Before,o=>o.MatchBr(out to));
                c.MoveBeforeLabels();
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((ProcessManager.ProcessID id) => { return id == EnumExt_LobbyMenu.LobbyMenu; });
                c.Emit(OpCodes.Brtrue, l2);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        class EnumExt_LobbyMenu
        {
            public static ProcessManager.ProcessID LobbyMenu;
        }

        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            float num3 = (self.CurrLang != InGameTranslator.LanguageID.Italian) ? 110f : 150f;
            var btn = new SimplerButton(self, self.pages[0], "Meadow", new Vector2(883f - num3 / 2f, 170f), new Vector2(num3, 30f));
            self.pages[0].subObjects.Add(btn);

            btn.OnClick += (SimplerButton obj) => { self.manager.RequestMainProcessSwitch(EnumExt_LobbyMenu.LobbyMenu); };
        }

        class SimplerButton : Menu.SimpleButton
        {
            public SimplerButton(Menu.Menu menu, MenuObject owner, string displayText, UnityEngine.Vector2 pos, UnityEngine.Vector2 size) : base(menu, owner, displayText, "", pos, size){}
            public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
            public event Action<SimplerButton> OnClick;
        }

        private MenuLabel debugLabel;

        private CallResult<LobbyMatchList_t> m_RequestLobbyListCall;
        private CallResult<LobbyCreated_t> m_CreateLobbyCall;
        private CallResult<LobbyEnter_t> m_JoinLobbyCall;

        private CSteamID me;
        private Lobby currentLobby;

        Vector2 btns = new Vector2(350,100);
        Vector2 btnsize = new Vector2(100, 20);


        public LobbyMenu(ProcessManager manager) : base(manager, EnumExt_LobbyMenu.LobbyMenu)
        {
            this.pages.Add(new Page(this, null, "main", 0));

            debugLabel = new Menu.MenuLabel(this, this.pages[0], "Start", new UnityEngine.Vector2(400, 200), new UnityEngine.Vector2(200, 30), false);
            pages[0].subObjects.Add(debugLabel);

            m_RequestLobbyListCall = CallResult<LobbyMatchList_t>.Create(LobbyListReceived);
            m_CreateLobbyCall = CallResult<LobbyCreated_t>.Create(LobbyCreated);
            m_JoinLobbyCall = CallResult<LobbyEnter_t>.Create(LobbyJoined);

            var joinbtn = new SimplerButton(this, this.pages[0], "new lobby", btns, btnsize);
            this.pages[0].subObjects.Add(joinbtn);
            joinbtn.OnClick += (SimplerButton obj) => { CreateLobby(); };

            var startbtn = new SimplerButton(this, this.pages[0], "new lobby", btns + new Vector2(200,0), btnsize);
            this.pages[0].subObjects.Add(startbtn);
            startbtn.OnClick += (SimplerButton obj) => { StartGame(); };

            debugLabel.text = "GetSteamID";
            me = SteamUser.GetSteamID();

            debugLabel.text = "AddRequestLobbyListStringFilter";
            SteamMatchmaking.AddRequestLobbyListStringFilter(Lobby.CLIENT_KEY, Lobby.CLIENT_VAL, ELobbyComparison.k_ELobbyComparisonEqual);

            debugLabel.text = "RequestLobbyList";
            m_RequestLobbyListCall.Set(SteamMatchmaking.RequestLobbyList());
        }

        void LobbyListReceived(LobbyMatchList_t pCallback, bool bIOFailure)
        {
            debugLabel.text = "LobbyListReceived";
            if (bIOFailure) return;

            var l = new List<CSteamID>();
            var ln = new List<FNode>();
            for (int i = 0; i < pCallback.m_nLobbiesMatching; i++)
            {
                var lobby = SteamMatchmaking.GetLobbyByIndex(i);
                var name = SteamMatchmaking.GetLobbyData(lobby, Lobby.NAME_KEY);
                var btn = new SimplerButton(this, this.pages[0], "join " + name + " - meadow", new UnityEngine.Vector2(0, 40 + 40 * i) + btns, btnsize);
                this.pages[0].subObjects.Add(btn);

                btn.OnClick += (SimplerButton obj) => { JoinLobby(lobby); };
            }
        }

        private void CreateLobby()
        {
            debugLabel.text = "CreateLobby";
            m_CreateLobbyCall.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 10));
        }

        private void LobbyCreated(LobbyCreated_t param, bool bIOFailure)
        {
            debugLabel.text = "LobbyCreated";
            if (bIOFailure || param.m_eResult != EResult.k_EResultOK) return;

            currentLobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));

            debugLabel.text = "SetupNewLobby";
            currentLobby.SetupNew();
        }

        private void JoinLobby(CSteamID lobby)
        {
            debugLabel.text = "JoinLobby";
            m_JoinLobbyCall.Set(SteamMatchmaking.JoinLobby(lobby));
        }

        private void LobbyJoined(LobbyEnter_t param, bool bIOFailure)
        {
            debugLabel.text = "LobbyJoined";
            if (bIOFailure) return;

            currentLobby = new Lobby(new CSteamID(param.m_ulSteamIDLobby));

            if(currentLobby.owner == me)
            {
                debugLabel.text = "SetupNewLobby";
                currentLobby.SetupNew();
            }
        }

        private void StartGame()
        {
            manager.menuSetup.startGameCondition = OnlineSession.EnumExt_OnlineSession.Online;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void CommunicateWithUpcomingProcess(MainLoopProcess nextProcess)
        {
            base.CommunicateWithUpcomingProcess(nextProcess);
            if (nextProcess is RainWorldGame game && game.isOnlineSession()) game.getOnlineSession().lobby = currentLobby;
        }
    }
}
