using Menu;
using Menu.Remix;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public abstract class SmartMenu : Menu.Menu
    {
        protected ProcessManager.ProcessID backTarget;
        protected Page mainPage;
        public MenuTabWrapper tabWrapper;
        private bool isExiting;
        private bool isInit = true;

        public abstract MenuScene.SceneID GetScene { get; }

        protected SmartMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            backTarget = manager.oldProcess.ID;
            this.pages.Add(this.mainPage = new Page(this, null, "main", 0));
            if (this.GetScene != null) mainPage.subObjects.Add(this.scene = new InteractiveMenuScene(this, mainPage, this.GetScene));
            mainPage.subObjects.Add(new MenuDarkSprite(this, mainPage));
            mainPage.subObjects.Add(this.tabWrapper = new MenuTabWrapper(this, mainPage));
            // what the fuck why the fuck are these added
            tabWrapper.myContainer._childNodes.ToList().ForEach(c => mainPage.Container.AddChild(c));
            tabWrapper.myContainer.RemoveFromContainer();
            tabWrapper.myContainer = mainPage.Container;
            tabWrapper._tab._container._childNodes.ToList().ForEach(c => mainPage.Container.AddChild(c));
            tabWrapper._tab._container.RemoveFromContainer();
            typeof(Menu.Remix.MixedUI.OpTab).GetField("_container", (System.Reflection.BindingFlags)0xFFFFFFF).SetValue(tabWrapper._tab, mainPage.Container);

            mainPage.subObjects.Add(this.backObject = new SimplerButton(this, mainPage, "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += Back;

            isExiting = RWInput.CheckPauseButton(0);
        }

        private void Back(SimplerButton obj)
        {
            manager.RequestMainProcessSwitch(this.backTarget);
            base.PlaySound(SoundID.MENU_Switch_Page_Out);
        }

        public override string UpdateInfoText()
        {
            if (this.selectedObject is IHaveADescription ihad)
            {
                return ihad.Description;
            }
            return base.UpdateInfoText();
        }

        public override void Update()
        {
            base.Update();

            if (RWInput.CheckPauseButton(0) && !isExiting)
            {
                manager.RequestMainProcessSwitch(this.backTarget);
                base.PlaySound(SoundID.MENU_Switch_Page_Out);
                isExiting = true;
            }
            else if (!RWInput.CheckPauseButton(0) && isInit)
            {
                isExiting = false;
                isInit = false;
            }
        }
    }
}