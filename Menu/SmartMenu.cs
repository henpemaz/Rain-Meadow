using UnityEngine;
using Menu;
using Menu.Remix;

namespace RainMeadow
{
    public abstract class SmartMenu : Menu.Menu
    {
        protected ProcessManager.ProcessID backTarget;
        protected Page mainPage;
        protected MenuTabWrapper tabWrapper;

        public abstract MenuScene.SceneID GetScene { get; }

        protected SmartMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            backTarget = manager.oldProcess.ID;
            this.pages.Add(this.mainPage = new Page(this, null, "main", 0));
            mainPage.subObjects.Add(this.scene = new InteractiveMenuScene(this, mainPage, this.GetScene));
            mainPage.subObjects.Add(new MenuDarkSprite(this, mainPage));
            mainPage.subObjects.Add(this.tabWrapper = new MenuTabWrapper(this, mainPage));
            mainPage.subObjects.Add(this.backObject = new SimplerButton(this, mainPage, "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += Back;
        }

        private void Back(SimplerButton obj)
        {
            manager.RequestMainProcessSwitch(this.backTarget);
        }

        public override string UpdateInfoText()
        {
            if (this.selectedObject is IHaveADescription ihad)
            {
                return ihad.Description;
            }
            return base.UpdateInfoText();
        }
    }
}