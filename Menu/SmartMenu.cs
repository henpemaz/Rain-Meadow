using UnityEngine;
using Menu;

namespace RainMeadow
{
    public abstract class SmartMenu : Menu.Menu
    {
        public abstract MenuScene.SceneID GetScene { get; }
        public abstract ProcessManager.ProcessID BackTarget { get; }

        protected SmartMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            this.pages.Add(new Page(this, null, "main", 0));

            this.scene = new InteractiveMenuScene(this, pages[0], this.GetScene);
            pages[0].subObjects.Add(this.scene);

            pages[0].subObjects.Add(new MenuDarkSprite(this, pages[0]));

            pages[0].subObjects.Add(this.backObject = new SimplerButton(this, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += Back;
        }

        private void Back(SimplerButton obj)
        {
            RainMeadow.DebugMe();
            manager.RequestMainProcessSwitch(this.BackTarget);
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