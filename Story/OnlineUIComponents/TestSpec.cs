using HUD;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HarmonyLib;
using Menu;

namespace RainMeadow
{
    public class TestSpec : Menu.Menu
    {
        private List<PlayerSpecificOnlineHud> indicators = new();

        public RainWorldGame game;

        public TestSpec(ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)

        {
            this.game = game;
            pages.Add(new Page(this, null, "spectator", 0));
            InitSpectatorMode();
            selectedObject = null;
        }

        public override void Update()
        {
            base.Update();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }

        public void InitSpectatorMode()
        {


            this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("PLAYERS"), new Vector2(1190, 553), new(110, 30), true));
            var btn = new SimplerButton(this, this.pages[0], "test", new Vector2(1190, 515) - 0 * new Vector2(0, 38), new(110, 30));
            this.pages[0].subObjects.Add(btn);
            btn.toggled = false;
            btn.OnClick += (_) =>
            {
                btn.toggled = !btn.toggled;
            };



        }


    }
}
