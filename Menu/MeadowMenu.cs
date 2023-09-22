using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowMenu : SmartMenu
    {
        private Vector2 btns = new Vector2(350, 100);
        private Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton startbtn;

        public override MenuScene.SceneID GetScene => MenuScene.SceneID.Landscape_CC;
        public MeadowMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.MeadowMenu)
        {
            RainMeadow.DebugMe();

            pages[0].subObjects.Add(startbtn = new SimplerButton(this, pages[0], "START", btns, btnsize));
            startbtn.buttonBehav.greyedOut = !OnlineManager.lobby.isAvailable;
            OnlineManager.lobby.OnLobbyAvailable += OnLobbyAvailable;
            startbtn.OnClick += (SimplerButton obj) => { StartGame(); };
        }

        private void OnLobbyAvailable()
        {
            startbtn.buttonBehav.greyedOut = false;
        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;
            manager.arenaSitting = null;
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby != null) OnlineManager.lobby.OnLobbyAvailable -= OnLobbyAvailable;
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                MatchmakingManager.instance.LeaveLobby();
            }
            base.ShutDownProcess();
        }
    }
}
