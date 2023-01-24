using Menu;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class LobbyMenu : Menu.Menu
    {
        private MenuLabel debugLabel;

        Vector2 btns = new Vector2(350, 100);
        Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton startbtn;

        //using System.Runtime.CompilerServices;
        void DebugLog(string message, [CallerMemberName] string callerName = "")
        {
            message = callerName + ": " + message;
            if (debugLabel != null) debugLabel.text = message;
            RainMeadow.sLogger.LogInfo("LobbyMenu." + message);
        }

        public LobbyMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbyMenu)
        {
            DebugLog("LobbySelectMenu created");
            this.pages.Add(new Page(this, null, "main", 0));

            this.scene = new InteractiveMenuScene(this, pages[0], MenuScene.SceneID.Landscape_CC);
            pages[0].subObjects.Add(this.scene);

            pages[0].subObjects.Add(this.backObject = new SimplerButton(this, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += Back;

            pages[0].subObjects.Add(debugLabel = new MenuLabel(this, pages[0], "Start", this.infoLabel.GetPosition() + new Vector2(0, -30), new Vector2(200, 30), false));

            pages[0].subObjects.Add(startbtn = new SimplerButton(this, pages[0], "START", btns, btnsize));
            startbtn.OnClick += (SimplerButton obj) => { StartGame(); };
        }

        private void Back(SimplerButton obj)
        {
            DebugLog("back");
            OnlineManager.instance.LeaveLobby();
            manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
        }

        public override string UpdateInfoText()
        {
            if (this.selectedObject is SimplerButton sb)
            {
                return sb.description;
            }
            return base.UpdateInfoText();
        }

        private void StartGame()
        {
            if (OnlineManager.instance.lobby == null) return;

            manager.menuSetup.startGameCondition = RainMeadow.Ext_StoryGameInitCondition.Online;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
            if (OnlineManager.instance.lobby.owner == OnlineManager.instance.me)
            {
                OnlineManager.instance.BroadcastEvent(new LobbyEvent(LobbyEvent.LobbyEventType.SessionStarted));
            }
        }

        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
        }
    }
}
