using Menu;
using System.Runtime.CompilerServices;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class LobbySelectMenu : Menu.Menu
    {
        private MenuLabel debugLabel;

        Vector2 btns = new Vector2(350, 100);
        Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton createbtn;

        //using System.Runtime.CompilerServices;
        void DebugLog(string message, [CallerMemberName] string callerName = "")
        {
            message = callerName + ": " + message;
            if (debugLabel != null) debugLabel.text = message;
            RainMeadow.sLogger.LogInfo(message);
        }

        public LobbySelectMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbySelectMenu)
        {
            DebugLog("LobbySelectMenu created");
            this.pages.Add(new Page(this, null, "main", 0));

            this.scene = new InteractiveMenuScene(this, pages[0], MenuScene.SceneID.Landscape_CC);
            pages[0].subObjects.Add(this.scene);

            pages[0].subObjects.Add(this.backObject = new SimplerButton(this, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += Back;

            pages[0].subObjects.Add(debugLabel = new MenuLabel(this, pages[0], "Start", this.infoLabel.GetPosition() + new Vector2(0, -30), new Vector2(200, 30), false));

            pages[0].subObjects.Add(createbtn = new SimplerButton(this, pages[0], "new lobby", btns, btnsize));
            createbtn.OnClick += (SimplerButton obj) => { RequestLobbyCreate(); };

            OnlineManager.instance.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            OnlineManager.instance.OnLobbyJoined += OnlineManager_OnLobbyJoined;

            OnlineManager.instance.RequestLobbyList();
        }

        private void Back(SimplerButton obj)
        {
            DebugLog("back");
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
        }

        public override string UpdateInfoText()
        {
            if (this.selectedObject is SimplerButton sb)
            {
                return sb.description;
            }
            return base.UpdateInfoText();
        }

        void RequestLobbyCreate()
        {
            OnlineManager.instance.CreateLobby();
        }

        void RequestLobbyJoin(Lobby lobby)
        {
            OnlineManager.instance.JoinLobby(lobby);
        }

        private void OnlineManager_OnLobbyJoined(bool ok, Lobby lobby)
        {
            if (ok)
            {
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbyMenu);
            }
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
                    btn.OnClick += (SimplerButton obj) => { RequestLobbyJoin(lobby); };
                    this.pages[0].subObjects.Add(btn);
                }
            }
            else
            {
                debugLabel.text = "LobbyListReceived failure";
            }
        }

        public override void ShutDownProcess()
        {
            OnlineManager.instance.OnLobbyListReceived -= OnlineManager_OnLobbyListReceived;
            OnlineManager.instance.OnLobbyJoined -= OnlineManager_OnLobbyJoined;
            base.ShutDownProcess();
        }
    }
}
