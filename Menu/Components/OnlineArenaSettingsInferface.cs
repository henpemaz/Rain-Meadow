using Menu;
using RainMeadow.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class OnlineArenaSettingsInferface : PositionedMenuObject, CheckBox.IOwnCheckBox, MultipleChoiceArray.IOwnMultipleChoiceArray, IRestorableMenuObject
    {
        public ArenaSetup GetArenaSetup => menu.manager.arenaSetup;
        public ArenaSetup.GameTypeSetup GetGameTypeSetup => GetArenaSetup.GetOrInitiateGameTypeSetup(GetArenaSetup.currentGameType);
        public OnlineArenaSettingsInferface(Menu.Menu menu, MenuObject owner, Vector2 pos, float settingsWidth = 300) : base(menu, owner, pos)
        {
            if (GetGameTypeSetup.gameType != ArenaSetup.GameTypeID.Competitive)
            {
                RainMeadow.Error("THIS IS NOT COMPETITIVE MODE!");
            }
            spearsHitCheckbox = new(menu, this, this, new(0, 220), 95, menu.Translate("Spears Hit:"), "SPEARSHIT", false);
            evilAICheckBox = new(menu, this, this, new(settingsWidth - 24, spearsHitCheckbox.pos.y), InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 120 : 100, menu.Translate("Aggressive AI:"), "EVILAI", false);
            divSprites = [new("pixel"), new("pixel")];
            divSpritePos = new Vector2[divSprites.Length];
            for (int i = 0; i < divSprites.Length; i++)
            {
                divSprites[i].anchorX = 0;
                divSprites[i].scaleX = settingsWidth;
                divSprites[i].scaleY = 2;
                divSprites[i].color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
                Container.AddChild(divSprites[i]);
                divSpritePos[i] = new(0, 197 - (171 * i));
            }
            roomRepeatArray = new(menu, this, this, new(0, 150), menu.Translate("Repeat Rooms:"), "ROOMREPEAT", InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 115 : 95, settingsWidth, 5, true, false);
            for (int i = 0; i < roomRepeatArray.buttons.Length; i++)
                roomRepeatArray.buttons[i].label.text = $"{i + 1}x";
            rainTimerArray = new(menu, this, this, new(0, 100), menu.Translate("Rain Timer:"), "SESSIONLENGTH", InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 100f : 95f, settingsWidth, 6, false, menu.CurrLang == InGameTranslator.LanguageID.French || menu.CurrLang == InGameTranslator.LanguageID.Spanish || menu.CurrLang == InGameTranslator.LanguageID.Portuguese);
            wildlifeArray = new(menu, this, this, new(0, 50), menu.Translate("Wildlife:"), "WILDLIFE", 95f, settingsWidth, 4, false, false);
            this.SafeAddSubobjects(spearsHitCheckbox, evilAICheckBox, roomRepeatArray, rainTimerArray, wildlifeArray);
        }
        public override void RemoveSprites()
        {
            for (int i = 0; i < divSprites.Length; i++)
            {
                divSprites[i].RemoveFromContainer();
            }
            base.RemoveSprites();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 pos = DrawPos(timeStacker);
            for (int i = 0; i < divSprites.Length; i++)
            {
                divSprites[i].x = pos.x + divSpritePos[i].x;
                divSprites[i].y = pos.y + divSpritePos[i].y;
            }
        }
        public override void Update()
        {
            base.Update();
            bool isNotOwner = !(OnlineManager.lobby?.isOwner == true);
            foreach (MenuObject obj in subObjects)
            {
                if (obj is ButtonTemplate btn)
                    btn.buttonBehav.greyedOut = isNotOwner;
                if (obj is MultipleChoiceArray array)
                    array.greyedOut = isNotOwner;
            }
        }
        public bool GetChecked(CheckBox box)
        {
            if (box.IDString == "SPEARSHIT")
            {
                return ArenaHelpers.GetOptionFromArena(box.IDString, GetGameTypeSetup.spearsHitPlayers);
            }
            if (box.IDString == "EVILAI")
            {
                return ArenaHelpers.GetOptionFromArena(box.IDString, GetGameTypeSetup.evilAI);
            }
            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            if (box.IDString == "SPEARSHIT")
            {
                GetGameTypeSetup.spearsHitPlayers = c;
            }
            if (box.IDString == "EVILAI")
            {
                GetGameTypeSetup.evilAI = c;
            }
            ArenaHelpers.SaveOptionToArena(box.IDString, c);
        }
        public int GetSelected(MultipleChoiceArray array)
        {
            if (array.IDString == "ROOMREPEAT")
            {
                return ArenaHelpers.GetOptionFromArena(array.IDString, GetGameTypeSetup.levelRepeats - 1);
            }
            if (array.IDString == "SESSIONLENGTH")
            {
                return ArenaHelpers.GetOptionFromArena(array.IDString, GetGameTypeSetup.sessionTimeLengthIndex);
            }
            if (array.IDString == "WILDLIFE" && GetGameTypeSetup.wildLifeSetting.Index != -1)
            {
                return ArenaHelpers.GetOptionFromArena(array.IDString, GetGameTypeSetup.wildLifeSetting.Index);
            }
            return 0;
        }
        public void SetSelected(MultipleChoiceArray array, int i)
        {
            if (array.IDString == "ROOMREPEAT")
            {
                GetGameTypeSetup.levelRepeats = i + 1;
            }
            if (array.IDString == "SESSIONLENGTH")
            {
                GetGameTypeSetup.sessionTimeLengthIndex = i;
            }
            if (array.IDString == "WILDLIFE")
            {
                GetGameTypeSetup.wildLifeSetting = new ArenaSetup.GameTypeSetup.WildLifeSetting(ExtEnum<ArenaSetup.GameTypeSetup.WildLifeSetting>.values.GetEntry(i), false);
            }
            ArenaHelpers.SaveOptionToArena(array.IDString, i);
        }
        public void RestoreSprites()
        {
            for (int i = 0; i < divSprites.Length; i++)
            {
                Container.AddChild(divSprites[i]);
            }
        }
        public void RestoreSelectables()
        { }
        public void CallForSync() //call this after ctor if needed for sync at start
        {
            foreach (MenuObject obj in subObjects)
            {
                if (obj is CheckBox checkBox)
                {
                    checkBox.Checked = checkBox.Checked;
                }
                if (obj is MultipleChoiceArray array)
                {
                    array.CheckedButton = array.CheckedButton;
                }
            }
        }
        public Vector2[] divSpritePos;
        public FSprite[] divSprites;
        public RestorableCheckbox spearsHitCheckbox, evilAICheckBox;
        public RestorableMultipleChoiceArray roomRepeatArray, rainTimerArray, wildlifeArray;
    }
}
