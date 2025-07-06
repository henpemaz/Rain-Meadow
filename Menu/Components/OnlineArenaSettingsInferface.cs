﻿using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;
using UnityEngine;
using Menu.Remix.MixedUI.ValueTypes;
using System.Linq;

namespace RainMeadow.UI.Components
{
    public class OnlineArenaSettingsInferface : PositionedMenuObject, CheckBox.IOwnCheckBox, MultipleChoiceArray.IOwnMultipleChoiceArray
    {
        public ArenaSetup GetArenaSetup => menu.manager.arenaSetup;
        public ArenaSetup.GameTypeSetup GetGameTypeSetup => GetArenaSetup.GetOrInitiateGameTypeSetup(GetArenaSetup.currentGameType);
        public bool SettingsDisabled => (menu as ArenaOnlineLobbyMenu)?.SettingsDisabled ?? true;

        public OnlineArenaSettingsInferface(Menu.Menu menu, MenuObject owner, Vector2 pos, string currentGameMode, List<ListItem> gameModes, float settingsWidth = 300) : base(menu, owner, pos)
        {
            tabWrapper = new(menu, this);
            if (GetGameTypeSetup.gameType != ArenaSetup.GameTypeID.Competitive)
            {
                RainMeadow.Error("THIS IS NOT COMPETITIVE MODE!");
            }
            float textWidthOfSpearHit = 95;
            spearsHitCheckbox = new(menu, this, this, new(0, 425), textWidthOfSpearHit, menu.Translate("Spears Hit:"), "SPEARSHIT", false);
            evilAICheckBox = new(menu, this, this, new(settingsWidth - 24, spearsHitCheckbox.pos.y), InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 120 : 100, menu.Translate("Aggressive AI:"), "EVILAI", false);
            divSprites = [new("pixel"), new("pixel")];
            divSpritePos = new Vector2[divSprites.Length];
            for (int i = 0; i < divSprites.Length; i++)
            {
                divSprites[i].anchorX = 0;
                divSprites[i].scaleX = settingsWidth + textWidthOfSpearHit;
                divSprites[i].scaleY = 2;
                divSprites[i].color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
                Container.AddChild(divSprites[i]);
                divSpritePos[i] = new(-textWidthOfSpearHit, 402 - (171 * i));
            }
            roomRepeatArray = new(menu, this, this, new(0, 355), menu.Translate("Repeat Rooms:"), "ROOMREPEAT", InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 115 : 95, settingsWidth, 5, true, false);
            for (int i = 0; i < roomRepeatArray.buttons.Length; i++)
                roomRepeatArray.buttons[i].label.text = $"{i + 1}x";
            rainTimerArray = new(menu, this, this, new(0, 305), menu.Translate("Rain Timer:"), "SESSIONLENGTH", InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 100f : 95f, settingsWidth, 6, false, menu.CurrLang == InGameTranslator.LanguageID.French || menu.CurrLang == InGameTranslator.LanguageID.Spanish || menu.CurrLang == InGameTranslator.LanguageID.Portuguese);
            wildlifeArray = new(menu, this, this, new(0, 255), menu.Translate("Wildlife:"), "WILDLIFE", 95, settingsWidth, 4, false, false);

            stealItemCheckBox = new CheckBox(menu, this, this, new Vector2(55f, 180f), 150f, menu.Translate("Allow Item Stealing:"), "ITEMSTEAL");
            allowMidGameJoinCheckbox = new CheckBox(menu, this, this, new(settingsWidth - 24, stealItemCheckBox.pos.y), 150, menu.Translate("Allow Mid-Game Join:"), "MIDGAMEJOIN");

            countdownTimerLabel = new(menu, this, menu.Translate("Countdown Timer:"), new Vector2(-95, stealItemCheckBox.pos.y - 38), new Vector2(0, 20), false);
            countdownTimerTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value), new(55, countdownTimerLabel.pos.y - 6), 50)
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("How long the grace timer at the beginning of rounds lasts for. Default 5s."),
            };
            countdownTimerTextBox.OnValueUpdate += (config, value, lastValue) =>
            {
                if (RainMeadow.isArenaMode(out ArenaMode arena))
                    arena.setupTime = countdownTimerTextBox.valueInt;
            };

            weaponCollisionCheckBox = new(menu, this, this, new(settingsWidth - 24, countdownTimerTextBox.PosY), 100, menu.Translate("Better Hitbox:"), "WEAPONCOLLISIONFIX");

            arenaGameModeLabel = new(menu, this, menu.Translate("Arena Game Mode:"), new Vector2(countdownTimerLabel.pos.x, countdownTimerTextBox.pos.y - 35), new Vector2(0, 20), false);
            arenaGameModeComboBox = new OpComboBox2(new Configurable<string>(currentGameMode), new Vector2(55, arenaGameModeLabel.pos.y - 6.5f), 175, gameModes) { description = menu.Translate("The game mode for this match") };
            arenaGameModeComboBox.OnValueChanged += (config, value, lastValue) =>
            {
                if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                arena.currentGameMode = value;
            };

            countdownWrapper = new UIelementWrapper(tabWrapper, countdownTimerTextBox);
            gameModeWrapper = new UIelementWrapper(tabWrapper, arenaGameModeComboBox);

            this.SafeAddSubobjects(tabWrapper, spearsHitCheckbox, evilAICheckBox, roomRepeatArray, rainTimerArray, wildlifeArray, countdownTimerLabel, arenaGameModeLabel, stealItemCheckBox, allowMidGameJoinCheckbox, weaponCollisionCheckBox);
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
            countdownTimerLabel.label.color = countdownTimerTextBox.rect.colorEdge;
            arenaGameModeLabel.label.color = arenaGameModeComboBox._rect.colorEdge;
        }
        public override void Update()
        {
            if (tabWrapper.IsAllRemixUINotHeld() && tabWrapper.holdElement) tabWrapper.holdElement = false;
            base.Update();

            foreach (MenuObject obj in subObjects)
            {
                if (obj is ButtonTemplate btn)
                    btn.buttonBehav.greyedOut = SettingsDisabled;
                if (obj is MultipleChoiceArray array)
                    array.greyedOut = SettingsDisabled;
            }
            countdownTimerTextBox.greyedOut = SettingsDisabled;
            arenaGameModeComboBox.greyedOut = SettingsDisabled;
            if (RainMeadow.isArenaMode(out ArenaMode arena))
            {
                if (!countdownTimerTextBox.held && countdownTimerTextBox.valueInt != arena.setupTime) countdownTimerTextBox.valueInt = arena.setupTime;
                if (!arenaGameModeComboBox.held && !gameModeComboBoxLastHeld) arenaGameModeComboBox.value = arena.currentGameMode;
            }
        }
        public bool GetChecked(CheckBox box)
        {
            string id = box.IDString;
            if (id == "SPEARSHIT") return ArenaHelpers.GetOptionFromArena(id, GetGameTypeSetup.spearsHitPlayers);
            if (id == "EVILAI") return ArenaHelpers.GetOptionFromArena(id, GetGameTypeSetup.evilAI);
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (id == "ITEMSTEAL") return arena.itemSteal;
                if (id == "MIDGAMEJOIN") return arena.allowJoiningMidRound;
                if (id == "WEAPONCOLLISIONFIX") return arena.weaponCollisionFix;
            }
            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            string id = box.IDString;
            if (id == "SPEARSHIT") GetGameTypeSetup.spearsHitPlayers = c;
            if (id == "EVILAI") GetGameTypeSetup.evilAI = c;
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (id == "ITEMSTEAL")
                {
                    arena.itemSteal = c;
                    return;
                }
                if (id == "MIDGAMEJOIN")
                {
                    arena.allowJoiningMidRound = c;
                    return;
                }
                if (id == "WEAPONCOLLISIONFIX")
                {
                    arena.weaponCollisionFix = c;
                    return;
                }
            }

            ArenaHelpers.SaveOptionToArena(box.IDString, c);
        }
        public int GetSelected(MultipleChoiceArray array)
        {
            if (array.IDString == "ROOMREPEAT")
                return ArenaHelpers.GetOptionFromArena(array.IDString, GetGameTypeSetup.levelRepeats - 1);
            if (array.IDString == "SESSIONLENGTH")
                return ArenaHelpers.GetOptionFromArena(array.IDString, GetGameTypeSetup.sessionTimeLengthIndex);
            if (array.IDString == "WILDLIFE" && GetGameTypeSetup.wildLifeSetting.Index != -1)
                return ArenaHelpers.GetOptionFromArena(array.IDString, GetGameTypeSetup.wildLifeSetting.Index);
            return 0;
        }
        public void SetSelected(MultipleChoiceArray array, int i)
        {
            if (array.IDString == "ROOMREPEAT")
                GetGameTypeSetup.levelRepeats = i + 1;
            if (array.IDString == "SESSIONLENGTH")
                GetGameTypeSetup.sessionTimeLengthIndex = i;
            if (array.IDString == "WILDLIFE")
                GetGameTypeSetup.wildLifeSetting = new ArenaSetup.GameTypeSetup.WildLifeSetting(ExtEnum<ArenaSetup.GameTypeSetup.WildLifeSetting>.values.GetEntry(i), false);
            ArenaHelpers.SaveOptionToArena(array.IDString, i);
        }
        public void CallForSync() //call this after ctor if needed for sync at start
        {
            foreach (MenuObject obj in subObjects)
            {
                if (obj is CheckBox checkBox)
                    checkBox.Checked = checkBox.Checked;
                if (obj is MultipleChoiceArray array)
                    array.CheckedButton = array.CheckedButton;
            }
            if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
            arena.setupTime = countdownTimerTextBox.valueInt;
            arena.currentGameMode = arenaGameModeComboBox.value;
        }

        public bool gameModeComboBoxLastHeld;
        public Vector2[] divSpritePos;
        public FSprite[] divSprites;
        public OpTextBox countdownTimerTextBox;
        public OpComboBox arenaGameModeComboBox;
        public CheckBox spearsHitCheckbox, evilAICheckBox, stealItemCheckBox, allowMidGameJoinCheckbox, weaponCollisionCheckBox;
        public ProperlyAlignedMenuLabel countdownTimerLabel, arenaGameModeLabel;
        public MultipleChoiceArray roomRepeatArray, rainTimerArray, wildlifeArray;
        public UIelementWrapper countdownWrapper, gameModeWrapper;
        public MenuTabWrapper tabWrapper;
    }
}
