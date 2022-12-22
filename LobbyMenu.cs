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
        class EnumExt_LobbyMenu
        {
            public static ProcessManager.ProcessID LobbyMenu;
        }

        public static void Apply()
        {
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.RainWorld.Start += RainWorld_Start;
            On.SteamManager.Awake += SteamManager_Awake;
        }

        private static void SteamManager_Awake(On.SteamManager.orig_Awake orig, MonoBehaviour self)
        {
            orig(self);
            if (self is SteamManager sm && sm.m_bInitialized) LobbyMenu.OnSteamConnected?.Invoke();
        }

        static event Action OnSteamConnected;

        private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);

            IL.ProcessManager.SwitchMainProcess += ProcessManager_SwitchMainProcess;
            UnityEngine.Debug.LogError("LobbyMenu registered");
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

        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            float num3 = (self.CurrLang != InGameTranslator.LanguageID.Italian) ? 110f : 150f;
            var btn = new SimplerButton(self, self.pages[0], "Meadow", new Vector2(883f - num3 / 2f, 170f), new Vector2(num3, 30f));
            self.pages[0].subObjects.Add(btn);
            OnSteamConnected += () => { btn.buttonBehav.greyedOut = false; };
            btn.buttonBehav.greyedOut = !SteamManager.Initialized;
            btn.OnClick += (SimplerButton obj) => { self.manager.RequestMainProcessSwitch(EnumExt_LobbyMenu.LobbyMenu); };
        }

        class SimplerButton : SimpleButton
        {
            public SimplerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, "", pos, size){}
            public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
            public event Action<SimplerButton> OnClick;
        }

        private MenuLabel debugLabel;
        private LobbyManager lobbyManager;

        Vector2 btns = new Vector2(350,100);
        Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton joinbtn;
        private SimplerButton startbtn;

        public LobbyMenu(ProcessManager manager) : base(manager, EnumExt_LobbyMenu.LobbyMenu)
        {
            this.pages.Add(new Page(this, null, "main", 0));

            debugLabel = new Menu.MenuLabel(this, this.pages[0], "Start", new Vector2(400, 200), new Vector2(200, 30), false);
            pages[0].subObjects.Add(debugLabel);

            joinbtn = new SimplerButton(this, this.pages[0], "new lobby", btns, btnsize);
            this.pages[0].subObjects.Add(joinbtn);
            joinbtn.OnClick += (SimplerButton obj) => { lobbyManager.CreateLobby(); };

            startbtn = new SimplerButton(this, this.pages[0], "start", btns + new Vector2(200,0), btnsize);
            this.pages[0].subObjects.Add(startbtn);
            startbtn.OnClick += (SimplerButton obj) => { StartGame(); };
            startbtn.buttonBehav.greyedOut = true;

            lobbyManager = new LobbyManager();

            lobbyManager.OnLobbyListReceived += LobbyManager_OnLobbyListReceived;
            lobbyManager.OnLobbyJoined += LobbyManager_OnLobbyJoined;
        }

        private void LobbyManager_OnLobbyJoined(bool ok, Lobby lobby)
        {
            if(ok) startbtn.buttonBehav.greyedOut = false;
        }

        private void LobbyManager_OnLobbyListReceived(bool ok, Lobby[] lobbies)
        {
            if (ok)
            {
                debugLabel.text = "LobbyListReceived success";

                for (int i = 0; i < lobbies.Length; i++)
                {
                    var lobby = lobbies[i];
                    var btn = new SimplerButton(this, this.pages[0], "join " + lobby.name + " - meadow", new UnityEngine.Vector2(0, 40 + 40 * i) + btns, btnsize);
                    btn.OnClick += (SimplerButton obj) => { lobbyManager.JoinLobby(lobby); };
                    this.pages[0].subObjects.Add(btn);
                }
            }
            else 
            { 
                debugLabel.text = "LobbyListReceived failure";

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
            if (nextProcess is RainWorldGame game && game.isOnlineSession()) game.getOnlineSession().lobby = lobbyManager.currentLobby;
        }
    }
}
