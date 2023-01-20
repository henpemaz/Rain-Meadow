using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class LobbyMenu : Menu.Menu
    {
        public class EnumExt_LobbyMenu
        {
            public static ProcessManager.ProcessID LobbyMenu;
        }

        private MenuLabel debugLabel;
        private OnlineManager onlineManager => OnlineManager.instance;

        Vector2 btns = new Vector2(350,100);
        Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton createbtn;
        private SimplerButton startbtn;

        public LobbyMenu(ProcessManager manager) : base(manager, EnumExt_LobbyMenu.LobbyMenu)
        {
            this.pages.Add(new Page(this, null, "main", 0));

            debugLabel = new Menu.MenuLabel(this, this.pages[0], "Start", new Vector2(400, 200), new Vector2(200, 30), false);
            pages[0].subObjects.Add(debugLabel);

            createbtn = new SimplerButton(this, this.pages[0], "new lobby", btns, btnsize);
            this.pages[0].subObjects.Add(createbtn);
            createbtn.OnClick += (SimplerButton obj) => { onlineManager.CreateLobby(); };

            startbtn = new SimplerButton(this, this.pages[0], "start", btns + new Vector2(200,0), btnsize);
            this.pages[0].subObjects.Add(startbtn);
            startbtn.OnClick += (SimplerButton obj) => { StartGame(); };
            startbtn.buttonBehav.greyedOut = true;

            onlineManager.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            onlineManager.OnLobbyJoined += OnlineManager_OnLobbyJoined;

            onlineManager.RequestLobbyList();
        }

        private void OnlineManager_OnLobbyJoined(bool ok, Lobby lobby)
        {
            if(ok) startbtn.buttonBehav.greyedOut = false;
        }

        private void OnlineManager_OnLobbyListReceived(bool ok, Lobby[] lobbies)
        {
            if (ok)
            {
                debugLabel.text = "LobbyListReceived success";

                for (int i = 0; i < lobbies.Length; i++)
                {
                    var lobby = lobbies[i];
                    var btn = new SimplerButton(this, this.pages[0], "join " + lobby.name + " - meadow", new UnityEngine.Vector2(0, 40 + 40 * i) + btns, btnsize);
                    btn.OnClick += (SimplerButton obj) => { onlineManager.JoinLobby(lobby); };
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
            if (onlineManager.lobby == null) return;

            manager.menuSetup.startGameCondition = OnlineSession.EnumExt_OnlineSession.Online;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
            if(onlineManager.lobby.owner == onlineManager.me)
            {
                onlineManager.BroadcastEvent(new LobbyEvent(LobbyEvent.LobbyEventType.SessionStarted));
            }
        }

        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
            onlineManager.OnLobbyListReceived -= OnlineManager_OnLobbyListReceived;
            onlineManager.OnLobbyJoined -= OnlineManager_OnLobbyJoined;
        }
    }
}
