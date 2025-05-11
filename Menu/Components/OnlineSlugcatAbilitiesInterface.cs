using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.IO;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class OnlineSlugcatAbilitiesInterface : PositionedMenuObject, IRestorableMenuObject, CheckBox.IOwnCheckBox //for MSC currently
    {
        public OnlineSlugcatAbilitiesInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 spacing, string painCatName, float textSpacing = 300) : base(menu, owner, pos)
        {
            tabWrapper = new(menu, this);
            maulingCheckBox = new(menu, this, this, Vector2.zero, textSpacing, menu.Translate("Disable Mauling:"), DISABLEMAUL, description: $"Block Artificer and {painCatName} to maul held creatures");
            artificerStunCheckBox = new(menu, this, this, -spacing, textSpacing, menu.Translate("Disable Artificer Stun:"), DISABLEARTISTUN, description: "Block Artificer to stun other players");
            sainotCheckBox = new(menu, this, this, -spacing * 2, textSpacing, menu.Translate("Sain't:"), SAINOT, description: "Disable Saint ascendance ability");
            saintAscendanceTimerLabel = new(menu, this, menu.Translate("Saint Ascendance Duration:"), (-spacing * 3) - new Vector2(300, -2), new(151, 20), false);
            saintAscendDurationTimerTextBox = new(RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer, saintAscendanceTimerLabel.pos + new Vector2(300, -2), 50f)
            {
                alignment = FLabelAlignment.Center,
                description = "How long Saint's ascendance ability lasts for. Default 120."
            };
            saintAscendDurationTimerTextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
            {
                if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                arena.arenaSaintAscendanceTimer = saintAscendDurationTimerTextBox.valueInt; //arena lobby calls save config for owner only
            };
            painCatEggCheckBox = new(menu, this, this, -spacing * 4, 300, $"{painCatName} gets egg at 0 throw skill:", PAINCATEGG, description: $"If {painCatName} spawns with 0 throw skill, also spawn with Eggzer0");
            painCatThrowsCheckBox = new(menu, this, this, -spacing * 5, 300, $"{painCatName} can always throw spears:", PAINCATTHROWS, description: $"Always allow {painCatName} to throw spears, even if throw skill is 0");
            painCatLizardCheckBox = new(menu, this, this, -spacing * 6, 300, $"{painCatName} sometimes gets a friend:", PAINCATLIZARD, description: $"Allow {painCatName} to rarely spawn with a little friend");
            resetToOptions = new(menu, this, menu.Translate("Reset Options"), new(300, 30), new(80, 30), "Reset all options to your previously set option");
            resetToOptions.OnClick += _ =>
            {
                menu.PlaySound(SoundID.MENU_Add_Level);
                CallForSync();
            };
            subObjects.AddRange([tabWrapper, maulingCheckBox, artificerStunCheckBox, sainotCheckBox, saintAscendanceTimerLabel, new RestorableUIelementWrapper(tabWrapper, saintAscendDurationTimerTextBox), painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox, resetToOptions]);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.BoundUIconfig = null;
        }
        public override void Update()
        {
            if (!saintAscendDurationTimerTextBox.held && tabWrapper.holdElement)
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
            saintAscendDurationTimerTextBox.greyedOut = isNotOwner;
            if (RainMeadow.isArenaMode(out ArenaMode arena))
            {
                if (!saintAscendDurationTimerTextBox.held && saintAscendDurationTimerTextBox.valueInt != arena.arenaSaintAscendanceTimer)
                    saintAscendDurationTimerTextBox.valueInt = arena.arenaSaintAscendanceTimer;
            }
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            saintAscendanceTimerLabel.label.color = saintAscendDurationTimerTextBox.colorEdge;
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
            if (!RainMeadow.isArenaMode(out ArenaMode arena) || !(OnlineManager.lobby?.isOwner == true)) return;
            string id = box.IDString;
            if (id == DISABLEMAUL)
            {
                arena.disableMaul = c;
                RainMeadow.rainMeadowOptions.BlockMaul.Value = c;
            }
            if (id == DISABLEARTISTUN)
            {
                arena.disableArtiStun = c;
                RainMeadow.rainMeadowOptions.BlockArtiStun.Value = c;
            }
            if (id == SAINOT)
            {
                arena.sainot = c;
                RainMeadow.rainMeadowOptions.ArenaSAINOT.Value = c;
            }
            if (id == PAINCATEGG)
            {
                arena.painCatEgg = c;
                RainMeadow.rainMeadowOptions.PainCatEgg.Value = c;
            }
            if (id == PAINCATTHROWS)
            {
                arena.painCatThrows = c;
                RainMeadow.rainMeadowOptions.PainCatThrows.Value = c;
            }
            if (id == PAINCATLIZARD)
            {
                arena.painCatLizard = c;
                RainMeadow.rainMeadowOptions.PainCatLizard.Value = c;
            }
        }
        public void RestoreSprites() 
        {
            RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.BoundUIconfig = saintAscendDurationTimerTextBox;
        }
        public void RestoreSelectables() { }
        public void CallForOptions()
        {
            if (!RainMeadow.isArenaMode(out ArenaMode arena))
            {
                RainMeadow.Debug("Not in arena mode, cannot save options to arena lobby");
                return;
            }
            maulingCheckBox.Checked = RainMeadow.rainMeadowOptions.BlockMaul.Value;
            artificerStunCheckBox.Checked = RainMeadow.rainMeadowOptions.BlockArtiStun.Value;
            sainotCheckBox.Checked = RainMeadow.rainMeadowOptions.ArenaSAINOT.Value;
            painCatEggCheckBox.Checked = RainMeadow.rainMeadowOptions.PainCatEgg.Value;
            painCatThrowsCheckBox.Checked = RainMeadow.rainMeadowOptions.PainCatThrows.Value;
            painCatLizardCheckBox.Checked = RainMeadow.rainMeadowOptions.PainCatLizard.Value;
            saintAscendDurationTimerTextBox.valueInt = RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value;
            CallForSync();
        }
        public void CallForSync() //call this after ctor if needed for sync at start
        {
            foreach (MenuObject obj in subObjects)
            {
                if (obj is CheckBox checkBox)
                {
                    checkBox.Checked = checkBox.Checked;
                }
            }
            if (!RainMeadow.isArenaMode(out ArenaMode arena))
            {
                RainMeadow.Debug("Not in arena mode, cannot call sync to arena lobby");
                return;
            }
            arena.arenaSaintAscendanceTimer = saintAscendDurationTimerTextBox.valueInt;
        }
        public const string SAINOT = "SAINOT", PAINCATTHROWS = "PAINCATTHROWS", PAINCATEGG = "PAINCATEGG", DISABLEARTISTUN = "DISABLEARTISTUN", DISABLEMAUL = "DISABLEMAUL", PAINCATLIZARD = "PAINCATLIZARD", SAINTASENSIONTIMER = "SAINTASCENSIONTIMER";
        public RestorableMenuTabWrapper tabWrapper;
        public OpTextBox saintAscendDurationTimerTextBox;
        public SimplerButton resetToOptions;
        public RestorableMenuLabel saintAscendanceTimerLabel;
        public RestorableCheckbox maulingCheckBox, artificerStunCheckBox, sainotCheckBox, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox;
    }
}
