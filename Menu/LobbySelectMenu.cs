using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class LobbySelectMenu : Menu.Menu
    {
        Vector2 btns = new Vector2(350, 100);
        Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton createbtn;

        public LobbySelectMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbySelectMenu)
        {
            RainMeadow.Debug("LobbySelectMenu created");
            this.pages.Add(new Page(this, null, "main", 0));

            this.scene = new InteractiveMenuScene(this, pages[0], MenuScene.SceneID.Landscape_CC);
            pages[0].subObjects.Add(this.scene);

            pages[0].subObjects.Add(new MenuDarkSprite(this, pages[0]));

            pages[0].subObjects.Add(this.backObject = new SimplerButton(this, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += Back;

            pages[0].subObjects.Add(createbtn = new SimplerButton(this, pages[0], "new lobby", btns, btnsize));
            createbtn.OnClick += (SimplerButton obj) => { RequestLobbyCreate(); };
            LobbyManager.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            LobbyManager.OnLobbyJoined += OnlineManager_OnLobbyJoined;
            LobbyManager.RequestLobbyList();
        }

        private void Back(SimplerButton obj)
        {
            RainMeadow.DebugMethod();
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
            RainMeadow.DebugMethod();
            LobbyManager.CreateLobby();
        }

        void RequestLobbyJoin(LobbyInfo lobby)
        {
            RainMeadow.DebugMethod();
            LobbyManager.JoinLobby(lobby);
        }

        private void OnlineManager_OnLobbyJoined(bool ok)
        {
            RainMeadow.Debug(ok);
            if (ok)
            {
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbyMenu);
            }
        }

        private void OnlineManager_OnLobbyListReceived(bool ok, LobbyInfo[] lobbies)
        {
            RainMeadow.Debug(ok);
            if (ok)
            {
                for (int i = 0; i < lobbies.Length; i++)
                {
                    var lobby = lobbies[i];
                    var btn = new SimplerButton(this, this.pages[0], "join " + lobby.name + " - meadow", new UnityEngine.Vector2(0, 40 + 40 * i) + btns, btnsize);
                    btn.OnClick += (SimplerButton obj) => { RequestLobbyJoin(lobby); };
                    this.pages[0].subObjects.Add(btn);
                }
            }
        }

        public override void ShutDownProcess()
        {
            LobbyManager.OnLobbyListReceived -= OnlineManager_OnLobbyListReceived;
            LobbyManager.OnLobbyJoined -= OnlineManager_OnLobbyJoined;
            base.ShutDownProcess();
        }
    }
}
