using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;
using UnityEngine;
using Menu.Remix.MixedUI.ValueTypes;

namespace RainMeadow.UI.Components
{
    public class OnlineArenaSettingsInferface : PositionedMenuObject, CheckBox.IOwnCheckBox, MultipleChoiceArray.IOwnMultipleChoiceArray, IRestorableMenuObject
    {
        public ArenaSetup GetArenaSetup => menu.manager.arenaSetup;
        public ArenaSetup.GameTypeSetup GetGameTypeSetup => GetArenaSetup.GetOrInitiateGameTypeSetup(GetArenaSetup.currentGameType);
        public OnlineArenaSettingsInferface(Menu.Menu menu, MenuObject owner, Vector2 pos, string currentGameMode, List<ListItem> gameModes, float settingsWidth = 300) : base(menu, owner, pos)
        {
            tabWrapper = new(menu, this);
            if (GetGameTypeSetup.gameType != ArenaSetup.GameTypeID.Competitive)
            {
                RainMeadow.Error("THIS IS NOT COMPETITIVE MODE!");
            }
            float textWidthOfSpearHit = 95;
            spearsHitCheckbox = new(menu, this, this, new(0, 220), textWidthOfSpearHit, menu.Translate("Spears Hit:"), "SPEARSHIT", false);
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
                divSpritePos[i] = new(-textWidthOfSpearHit, 197 - (171 * i));
            }
            roomRepeatArray = new(menu, this, this, new(0, 150), menu.Translate("Repeat Rooms:"), "ROOMREPEAT", InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 115 : 95, settingsWidth, 5, true, false);
            for (int i = 0; i < roomRepeatArray.buttons.Length; i++)
                roomRepeatArray.buttons[i].label.text = $"{i + 1}x";
            rainTimerArray = new(menu, this, this, new(0, 100), menu.Translate("Rain Timer:"), "SESSIONLENGTH", InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 100f : 95f, settingsWidth, 6, false, menu.CurrLang == InGameTranslator.LanguageID.French || menu.CurrLang == InGameTranslator.LanguageID.Spanish || menu.CurrLang == InGameTranslator.LanguageID.Portuguese);
            wildlifeArray = new(menu, this, this, new(0, 50), menu.Translate("Wildlife:"), "WILDLIFE", 95, settingsWidth, 4, false, false);

            countdownTimerLabel = new(menu, this, menu.Translate("Countdown Timer:"), new Vector2(-15, -153), new Vector2(105, 20), false);
            countdownTimerDragger = new(new Configurable<int>(RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value), countdownTimerLabel.pos.x + 135, countdownTimerLabel.pos.y + 3)
            {
                description = "How long the grace timer at the beginning of rounds lasts for. Default 5s.",
                max = int.MaxValue //unless a psycho is here nobody would want this xd
            };
            arenaGameModeLabel = new(menu, this, "Arena Game Mode:", new Vector2(-95, -103), new Vector2(105, 20), false);
            arenaGameModeComboBox = new OpComboBox2(new Configurable<string>(currentGameMode), new Vector2(arenaGameModeLabel.pos.x + 215, arenaGameModeLabel.pos.y - 2), 175f, gameModes);
            arenaGameModeComboBox.OnValueChanged += (config, value, lastValue) =>
            {
                if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                arena.currentGameMode = value;
            };
            this.SafeAddSubobjects(tabWrapper, spearsHitCheckbox, evilAICheckBox, roomRepeatArray, rainTimerArray, wildlifeArray, countdownTimerLabel, 
                new UIelementWrapper(tabWrapper, countdownTimerDragger), arenaGameModeLabel, new UIelementWrapper(tabWrapper, arenaGameModeComboBox));
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
            countdownTimerLabel.label.color = countdownTimerDragger.rect.colorEdge;
            arenaGameModeLabel.label.color = arenaGameModeComboBox._rect.colorEdge;
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
            countdownTimerDragger.greyedOut = isNotOwner;
            arenaGameModeComboBox.greyedOut = isNotOwner;
            if (RainMeadow.isArenaMode(out ArenaMode arena))
            {
                if (countdownTimerDragger.greyedOut || (!countdownTimerDragger.held && !countdownTimerDragger.MouseOver)) countdownTimerDragger.SetValueInt(arena.setupTime);
                else arena.setupTime = countdownTimerDragger.GetValueInt();

                if (!arenaGameModeComboBox.held && !gameModeComboBoxLastHeld)
                {
                    arenaGameModeComboBox.value = arena.currentGameMode;
                }
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
            foreach (FSprite sprite in divSprites)
            {
                Container.AddChild(sprite);
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
            if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
            arena.setupTime = countdownTimerDragger.GetValueInt();
            arena.currentGameMode = arenaGameModeComboBox.value;
        }

        public bool gameModeComboBoxLastHeld;
        public Vector2[] divSpritePos;
        public FSprite[] divSprites;
        public OpDragger countdownTimerDragger;
        public OpComboBox arenaGameModeComboBox;
        public MenuLabel countdownTimerLabel, arenaGameModeLabel;
        public CheckBox spearsHitCheckbox, evilAICheckBox;
        public MultipleChoiceArray roomRepeatArray, rainTimerArray, wildlifeArray;
        public MenuTabWrapper tabWrapper;
    }
}
