﻿using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class LobbyMenu : Menu.Menu
    {
        Vector2 btns = new Vector2(350, 100);
        Vector2 btnsize = new Vector2(100, 20);
        private SimplerButton startbtn;

        public LobbyMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbyMenu)
        {
            RainMeadow.Debug("LobbySelectMenu created");
            this.pages.Add(new Page(this, null, "main", 0));

            this.scene = new InteractiveMenuScene(this, pages[0], MenuScene.SceneID.Landscape_CC);
            pages[0].subObjects.Add(this.scene);

            pages[0].subObjects.Add(new MenuDarkSprite(this, pages[0]));

            pages[0].subObjects.Add(this.backObject = new SimplerButton(this, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += Back;

            pages[0].subObjects.Add(startbtn = new SimplerButton(this, pages[0], "START", btns, btnsize));
            //startbtn.buttonBehav.greyedOut = !OnlineManager.lobby.isOwner;
            startbtn.OnClick += (SimplerButton obj) => { StartGame(); };
        }

        private void Back(SimplerButton obj)
        {
            RainMeadow.DebugMethod();
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
            RainMeadow.DebugMethod();
            if (OnlineManager.lobby == null) return;
            manager.menuSetup.startGameCondition = RainMeadow.Ext_StoryGameInitCondition.Online;
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void ShutDownProcess()
        {
            if(manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                LobbyManager.LeaveLobby();
            }
            base.ShutDownProcess();
        }
    }
}
