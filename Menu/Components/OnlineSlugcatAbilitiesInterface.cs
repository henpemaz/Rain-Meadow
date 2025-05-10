using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class OnlineSlugcatAbilitiesInterface : PositionedMenuObject, IRestorableMenuObject, CheckBox.IOwnCheckBox //for MSC currently
    {
        public OnlineSlugcatAbilitiesInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 spacing, string painCatName) : base(menu, owner, pos)
        {
            tabWrapper = new(menu, this);
            maulingCheckBox = new(menu, this, this, Vector2.zero, 300, menu.Translate("Disable Mauling:"), ArenaMode.DISABLEMAUL, description: $"Block Artificer and {painCatName} to maul held creatures");
            artificerStunCheckBox = new(menu, this, this, -spacing, 300, menu.Translate("Disable Artificer Stun:"), ArenaMode.DISABLEARTISTUN, description: "Block Artificer to stun other players");
            sainotCheckBox = new(menu, this, this, -spacing * 2, 300, menu.Translate("Sain't:"), ArenaMode.SAINOT, description: "Disable Saint ascendance ability");
            saintAscendanceTimerLabel = new(menu, this, menu.Translate("Saint Ascendance Duration:"), (-spacing * 3) - new Vector2(300, -2), new(151, 20), false);
            saintAscendDurationTimerTextBox = new(RainMeadow.rainMeadowOptions.ArenaCountDownTimer, (-spacing * 3) - new Vector2(13f, 0f), 50f)
            {
                alignment = FLabelAlignment.Center,
                description = "How long Saint's ascendance ability lasts for. Default 120."
            };
            saintAscendDurationTimerTextBox.OnChange += () =>
            {
               
            };
            painCatEggCheckBox = new(menu, this, this, -spacing * 4, 300, $"{painCatName} gets egg at 0 throw skill:", ArenaMode.PAINCATEGG, description: $"If {painCatName} spawns with 0 throw skill, also spawn with Eggzer0");
            painCatThrowsCheckBox = new(menu, this, this, -spacing * 5, 300, $"{painCatName} can always throw spears:", ArenaMode.PAINCATTHROWS, description: $"Always allow {painCatName} to throw spears, even if throw skill is 0");
            painCatLizardCheckBox = new(menu, this, this, -spacing * 6, 300, $"{painCatName} sometimes gets a friend:", ArenaMode.PAINCATLIZARD, description: $"Allow {painCatName} to rarely spawn with a little friend");
            subObjects.AddRange([maulingCheckBox, artificerStunCheckBox, sainotCheckBox, saintAscendanceTimerLabel, new RestorableUIelementWrapper(tabWrapper, saintAscendDurationTimerTextBox), painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox]);
        }
        public override void Update()
        {
            base.Update();
            bool isNotOwner = !(OnlineManager.lobby?.isOwner == true);
            foreach (MenuObject obj in subObjects)
            {
                if (obj is ButtonTemplate btn)
                    btn.buttonBehav.greyedOut = isNotOwner;
            }
            saintAscendDurationTimerTextBox.greyedOut = isNotOwner;
        }
        public bool GetChecked(CheckBox box)
        {
            string id = box.IDString;
            if (id == ArenaMode.DISABLEMAUL)
            {
                return ArenaHelpers.GetOptionFromArena(id, RainMeadow.rainMeadowOptions.BlockMaul.Value);
            }
            if (id == ArenaMode.DISABLEARTISTUN)
            {
                return ArenaHelpers.GetOptionFromArena(id, RainMeadow.rainMeadowOptions.BlockArtiStun.Value);
            }
            if (id == ArenaMode.PAINCATEGG)
            {
                return ArenaHelpers.GetOptionFromArena(id, RainMeadow.rainMeadowOptions.PainCatEgg.Value);
            }
            if (id == ArenaMode.PAINCATTHROWS)
            {
                return ArenaHelpers.GetOptionFromArena(id, RainMeadow.rainMeadowOptions.PainCatThrows.Value);
            }
            if (id == ArenaMode.PAINCATLIZARD)
            {
                return ArenaHelpers.GetOptionFromArena(id, RainMeadow.rainMeadowOptions.PainCatLizard.Value);
            }
            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            string id = box.IDString;
            ArenaHelpers.SaveOptionToArena(box.IDString, c);
        }
        public void RestoreSprites() { }
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
            saintAscendDurationTimerTextBox.valueInt = RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value;
        }
        public OpTextBox saintAscendDurationTimerTextBox;
        public SimplerButton resetToDefault, resetToOptions;
        public RestorableMenuLabel saintAscendanceTimerLabel;
        public RestorableCheckbox maulingCheckBox, artificerStunCheckBox, sainotCheckBox, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox;
        public MenuTabWrapper tabWrapper;
    }
}
