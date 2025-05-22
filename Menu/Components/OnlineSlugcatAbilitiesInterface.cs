using ArenaMode = RainMeadow.ArenaOnlineGameMode;
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
            blockMaulCheckBox = new(menu, this, this, Vector2.zero, textSpacing, menu.Translate("Disable Mauling:"), DISABLEMAUL, description: $"Block Artificer and {painCatName} to maul held creatures");
            blockArtiStunCheckBox = new(menu, this, this, -spacing, textSpacing, menu.Translate("Disable Artificer Stun:"), DISABLEARTISTUN, description: "Block Artificer to stun other players");
            sainotCheckBox = new(menu, this, this, -spacing * 2, textSpacing, menu.Translate("Sain't:"), SAINOT, description: "Disable Saint ascendance ability");
            saintAscendanceTimerLabel = new(menu, this, menu.Translate("Saint Ascendance Duration:"), (-spacing * 3) - new Vector2(300, -2), new(151, 20), false);
            saintAscendDurationTimerDragger = new(new Configurable<int>(RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value), saintAscendanceTimerLabel.pos.x + 300, saintAscendanceTimerLabel.pos.y - 2)
            {
                description = "How long Saint's ascendance ability lasts for. Scroll or move up/down while holding jump to configure it. Default 120.",
                max = int.MaxValue //who will want this
            };
            painCatEggCheckBox = new(menu, this, this, -spacing * 4, 300, $"{painCatName} gets egg at 0 throw skill:", PAINCATEGG, description: $"If {painCatName} spawns with 0 throw skill, also spawn with Eggzer0");
            painCatThrowsCheckBox = new(menu, this, this, -spacing * 5, 300, $"{painCatName} can always throw spears:", PAINCATTHROWS, description: $"Always allow {painCatName} to throw spears, even if throw skill is 0");
            painCatLizardCheckBox = new(menu, this, this, -spacing * 6, 300, $"{painCatName} sometimes gets a friend:", PAINCATLIZARD, description: $"Allow {painCatName} to rarely spawn with a little friend");
            this.SafeAddSubobjects([tabWrapper, blockMaulCheckBox, blockArtiStunCheckBox, sainotCheckBox, saintAscendanceTimerLabel, new UIelementWrapper(tabWrapper, saintAscendDurationTimerDragger), painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox]);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
        }
        public override void Update()
        {
            if (this.IsAllRemixUINotHeld() && tabWrapper.holdElement)
            {
                tabWrapper.holdElement = false;
            }
            base.Update();
            bool isNotOwner = !(OnlineManager.lobby?.isOwner == true);
            foreach (MenuObject obj in subObjects)
            {
                if (obj is ButtonTemplate btn)
                    btn.buttonBehav.greyedOut = isNotOwner;
            }
            saintAscendDurationTimerDragger.greyedOut = isNotOwner;
            if (RainMeadow.isArenaMode(out ArenaMode arena))
            {
                if (saintAscendDurationTimerDragger.greyedOut || (!saintAscendDurationTimerDragger.MouseOver && !saintAscendDurationTimerDragger.held)) saintAscendDurationTimerDragger.SetValueInt(arena.arenaSaintAscendanceTimer);
                else arena.arenaSaintAscendanceTimer = saintAscendDurationTimerDragger.GetValueInt();
            }
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            saintAscendanceTimerLabel.label.color = saintAscendDurationTimerDragger.rect.colorEdge;
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
            if (id == DISABLEARTISTUN)  arena.disableArtiStun = c;
            if (id == SAINOT) arena.sainot = c;
            if (id == PAINCATEGG) arena.painCatEgg = c;
            if (id == PAINCATTHROWS) arena.painCatThrows = c;
            if (id == PAINCATLIZARD) arena.painCatLizard = c;
        }
        public void RestoreSprites() 
        {
        }
        public void RestoreSelectables() { }
        public void CallForSync() //call this after ctor if needed for sync at start
        {
            foreach (MenuObject obj in subObjects)
            {
                if (obj is CheckBox checkBox)
                {
                    checkBox.Checked = checkBox.Checked;
                }
            }
            if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
            arena.arenaSaintAscendanceTimer = saintAscendDurationTimerDragger.GetValueInt();
        }
        public const string SAINOT = "SAINOT", PAINCATTHROWS = "PAINCATTHROWS", PAINCATEGG = "PAINCATEGG", DISABLEARTISTUN = "DISABLEARTISTUN", DISABLEMAUL = "DISABLEMAUL", PAINCATLIZARD = "PAINCATLIZARD", SAINTASENSIONTIMER = "SAINTASCENSIONTIMER";
        public MenuTabWrapper tabWrapper;
        public OpDragger saintAscendDurationTimerDragger;
        public MenuLabel saintAscendanceTimerLabel;
        public RestorableCheckbox blockMaulCheckBox, blockArtiStunCheckBox, sainotCheckBox, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox;
    }
}
