using Menu;
using Steamworks;
using UnityEngine;

namespace RainMeadow
{
    public class LobbySelectMenu : SmartMenu
    {
        Vector2 btns = new Vector2(350, 100);
        Vector2 btnsize = new Vector2(100, 30);
        private SimplerButton createbtn;

        public override MenuScene.SceneID GetScene => MenuScene.SceneID.Landscape_CC;
        public override ProcessManager.ProcessID BackTarget => ProcessManager.ProcessID.MainMenu;

        public LobbySelectMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbySelectMenu)
        {
            pages[0].subObjects.Add(createbtn = new SimplerButton(this, pages[0], "new lobby", btns, btnsize));
            createbtn.OnClick += (SimplerButton obj) => { RequestLobbyCreate(); };
            LobbyManager.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            LobbyManager.OnLobbyJoined += OnlineManager_OnLobbyJoined;
            SteamNetworkingUtils.InitRelayNetworkAccess();
            LobbyManager.RequestLobbyList();
        }

        void RequestLobbyCreate()
        {
            RainMeadow.DebugMe();
            LobbyManager.CreateLobby();
        }

        void RequestLobbyJoin(LobbyInfo lobby)
        {
            RainMeadow.DebugMe();
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
