using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class LobbyMenu : SmartMenu
    {
        Vector2 btns = new Vector2(350, 100);
        Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton startbtn;

        public override MenuScene.SceneID GetScene => MenuScene.SceneID.Landscape_CC;
        public LobbyMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbyMenu)
        {
            RainMeadow.Debug("LobbySelectMenu created");
            
            pages[0].subObjects.Add(startbtn = new SimplerButton(this, pages[0], "START", btns, btnsize));
            startbtn.buttonBehav.greyedOut = !OnlineManager.lobby.isAvailable;
            OnlineManager.lobby.OnLobbyAvailable += OnLobbyAvailable;
            startbtn.OnClick += (SimplerButton obj) => { StartGame(); };
        }

        void OnLobbyAvailable()
        {
            startbtn.buttonBehav.greyedOut = false;
        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;
            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.menuSetup.startGameCondition = RainMeadow.Ext_StoryGameInitCondition.Online;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby != null) OnlineManager.lobby.OnLobbyAvailable -= OnLobbyAvailable;
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                LobbyManager.LeaveLobby();
            }
            base.ShutDownProcess();
        }
    }
}
