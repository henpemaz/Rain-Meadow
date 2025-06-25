using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using Menu.Remix;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class OnlineSlugcatAbilitiesInterface : PositionedMenuObject, CheckBox.IOwnCheckBox //for MSC currently
    {
        public OnlineSlugcatAbilitiesInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 spacing, string painCatName, float textSpacing = 300) : base(menu, owner, pos)
        {
            tabWrapper = new(menu, this);
            blockMaulCheckBox = new(menu, this, this, Vector2.zero, textSpacing, Translate("Disable Mauling:"), DISABLEMAUL, false, Translate("Prevent Artificer and <PAINCATNAME> from mauling"));
            blockArtiStunCheckBox = new(menu, this, this, -spacing, textSpacing, Translate("Disable Artificer Stun:"), DISABLEARTISTUN, false, Translate("Prevent Artificer from stunning other players"));
            sainotCheckBox = new(menu, this, this, -spacing * 2, textSpacing, Translate("Sain't:"), SAINOT, false, Translate("Disable Saint ascendance ability, but allow it to throw spears"));
            saintAscendanceTimerLabel = new(menu, this, Translate("Saint Ascendance Duration:"), (-spacing * 3) - new Vector2(300, -2), new(151, 20), false);
            saintAscendDurationTimerTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value), new(saintAscendanceTimerLabel.pos.x + 292.5f, saintAscendanceTimerLabel.pos.y - 2), 40)
            {
                alignment = FLabelAlignment.Center,
                description = Translate("How long Saint's ascendance ability lasts for. Default 3s.")
            };
            saintAscendDurationTimerTextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                arena.arenaSaintAscendanceTimer = saintAscendDurationTimerTextBox.valueInt;
            };
            painCatEggCheckBox = new(menu, this, this, -spacing * 4, 300, Translate("<PAINCATNAME> gets egg at 0 throw skill:"), PAINCATEGG, description: Translate("If <PAINCATNAME> spawns with 0 throw skill, also spawn with Eggzer0"));
            painCatThrowsCheckBox = new(menu, this, this, -spacing * 5, 300, Translate("<PAINCATNAME> can always throw spears:"), PAINCATTHROWS, description: Translate("Always allow <PAINCATNAME> to throw spears, even if throw skill is 0"));
            painCatLizardCheckBox = new(menu, this, this, -spacing * 6, 300, Translate("<PAINCATNAME> sometimes gets a friend:"), PAINCATLIZARD, description: Translate("Allow <PAINCATNAME> to rarely spawn with a little friend"));
            new UIelementWrapper(tabWrapper, saintAscendDurationTimerTextBox);
            this.SafeAddSubobjects([tabWrapper, blockMaulCheckBox, blockArtiStunCheckBox, sainotCheckBox, saintAscendanceTimerLabel, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox]);
            string Translate(string s) => menu.LongTranslate(s).Replace("<PAINCATNAME>", painCatName);
        }
        public override void Update()
        {
            if (tabWrapper.IsAllRemixUINotHeld() && tabWrapper.holdElement) tabWrapper.holdElement = false;
            base.Update();
            bool isNotOwner = !(OnlineManager.lobby?.isOwner == true);
            foreach (MenuObject obj in subObjects)
            {
                if (obj is ButtonTemplate btn)
                    btn.buttonBehav.greyedOut = isNotOwner;
            }
            saintAscendDurationTimerTextBox.greyedOut = isNotOwner;
            saintAscendDurationTimerTextBox.held = saintAscendDurationTimerTextBox._KeyboardOn;
            if (RainMeadow.isArenaMode(out ArenaMode arena))
            {
                if (!saintAscendDurationTimerTextBox.held && saintAscendDurationTimerTextBox.valueInt != arena.arenaSaintAscendanceTimer) saintAscendDurationTimerTextBox.valueInt = arena.arenaSaintAscendanceTimer;
            }
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            saintAscendanceTimerLabel.label.color = saintAscendDurationTimerTextBox.rect.colorEdge;
        }
        public bool GetChecked(CheckBox box)
        {
            string id = box.IDString;
            if (RainMeadow.isArenaMode(out ArenaMode arena))
            {
                if (id == DISABLEMAUL) return arena.disableMaul;
                if (id == DISABLEARTISTUN) return arena.disableArtiStun;
                if (id == SAINOT) return arena.sainot;
                if (id == PAINCATEGG) return arena.painCatEgg;
                if (id == PAINCATTHROWS) return arena.painCatThrows;
                if (id == PAINCATLIZARD) return arena.painCatLizard;
            }
            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
            string id = box.IDString;
            if (id == DISABLEMAUL) arena.disableMaul = c; //owner can only edit it, its fine
            if (id == DISABLEARTISTUN) arena.disableArtiStun = c;
            if (id == SAINOT) arena.sainot = c;
            if (id == PAINCATEGG) arena.painCatEgg = c;
            if (id == PAINCATTHROWS) arena.painCatThrows = c;
            if (id == PAINCATLIZARD) arena.painCatLizard = c;
        }
        public void CallForSync() //call this after ctor if needed for sync at start
        {
            // dusty says this does something, just trust them future Timbits
            foreach (MenuObject obj in subObjects)
            {
                if (obj is CheckBox checkBox)
                {
                    checkBox.Checked = checkBox.Checked;
                }
            }
            if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
            arena.arenaSaintAscendanceTimer = saintAscendDurationTimerTextBox.valueInt;
        }

        public const string SAINOT = "SAINOT", PAINCATTHROWS = "PAINCATTHROWS", PAINCATEGG = "PAINCATEGG", DISABLEARTISTUN = "DISABLEARTISTUN", DISABLEMAUL = "DISABLEMAUL", PAINCATLIZARD = "PAINCATLIZARD", SAINTASENSIONTIMER = "SAINTASCENSIONTIMER";
        public MenuTabWrapper tabWrapper;
        public OpTextBox saintAscendDurationTimerTextBox;
        public MenuLabel saintAscendanceTimerLabel;
        public RestorableCheckbox blockMaulCheckBox, blockArtiStunCheckBox, sainotCheckBox, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox;
    }
}
