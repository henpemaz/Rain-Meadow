using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RainMeadow.UI.Components;
using UnityEngine;
using RWCustom;
using HarmonyLib;

namespace RainMeadow.UI
{
    public class SlugcatSelector : Dialog
    {
        public Action<SlugcatStats.Name[], SlugcatSelector> RecieveSlugcat;
        public FLabel titleName, congratsLabel;
        public Page selectPage;
        public SlugcatRandomizer[] slugcatRandomizers;
        public ButtonScroller.ScrollerButton continueButton;
        public HSLColor rainbowColor = new(0, 1, 0.5f);
        public bool rolling, hasAlreadyRolled, isClosing, queuedToClose, showRainbow;
        public int myRollingCounter, showResultsCounter, resultShownCounter = -1, startEndRollingOrderCounter = 40, endRollingCounter = 120;
        public float desiredSelectPagePosY, congratsLabelAlpha = 0;
        public List<string> losingDescriptions = ["Oops, better luck next time!", "Try your luck again!", "99% of gamblers quit before they win big"],
            closeDesriptions = ["So Close! Keep trying!", "Almost there!"],
            winningDescriptions = ["Congratulations! You have achieved the ultimate gamble skill!!!", "Gahh?? Impossible?!"];

        public static List<string> slugcatSelectorHints = ["Press and hold something on screen to spin the slots / Hope you brought green", "One, two three of a kind / Somewhere on this page you will find", "A joke started from the Meadow Discord / Makes all players broke, and none can afford"];
        public bool FinishedShowingResults => slugcatRandomizers.All(x => !x.rolling && x.desiredResultPosY == x.resultPosY);
        public bool IsMatching => slugcatRandomizers.All(x => x.slugcatButton.slugcat == slugcatRandomizers[0].slugcatButton.slugcat);
        public bool IsCloseMatching => slugcatRandomizers.GroupBy(x => x.slugcatButton.slugcat).OrderByDescending(x => x.Count()).FirstOrDefault()?.Count() == slugcatRandomizers.Length - 1;
        public SlugcatSelector(ProcessManager manager, SlugcatStats.Name[] currentSlugcats, SlugcatStats.Name[] currentlySelectableSlugcats, Action<SlugcatStats.Name[], SlugcatSelector> recieveSlugcats, int perRow = 3, float desiredPortraitScale = 1.2f) : base(manager)
        {
            darkSprite.alpha = 0.85f;
            RecieveSlugcat = recieveSlugcats;
            pages.Add(selectPage = new(this, null, "SlugcatSelect", 1));
            selectPage.pos.y = -1500;
            titleName = new(Custom.GetDisplayFont(), Translate("SCUGSLOTS"))
            {
                scale = 1.7f,
                shader = manager.rainWorld.Shaders["MenuTextCustom"],
            };
            congratsLabel = new(Custom.GetDisplayFont(), "")
            {
                 anchorY = 1,
                 alpha = 0,
                shader = manager.rainWorld.Shaders["MenuTextCustom"],
            };
            selectPage.Container.AddChild(titleName);
            selectPage.Container.AddChild(congratsLabel);
            continueButton = new(this, selectPage, Translate("LET'S ROLL!"), new(manager.rainWorld.options.ScreenSize.x / 2 - 60, PopulateSlugcatRandomiers(currentSlugcats, currentlySelectableSlugcats, perRow, desiredPortraitScale)), new(120, 30))
            {
                Alpha = 0,
                signalText = "ROLL"
            };
            selectPage.SafeAddSubobjects(continueButton);
        }
        public override void Update()
        {
            base.Update();
            PlayQueuedSounds();
            rainbowColor.hue = Mathf.PingPong(showResultsCounter, 40) / 40;
            if (selectPage.pos.y != desiredSelectPagePosY)
            {
                selectPage.pos.y = Custom.LerpAndTick(selectPage.pos.y, desiredSelectPagePosY, 0.35f, 0.1f);
                return;
            }
            if (queuedToClose)
            {
                manager.StopSideProcess(this);
                queuedToClose = false;
                isClosing = true;
            }
            if (continueButton.Alpha < 1)
            {
                continueButton.Alpha += 0.05f;
                if (continueButton.Alpha >= 1)
                {
                    currentPage = 1;
                    if (!manager.menuesMouseMode) selectedObject = continueButton;
                }
                return;
            }
            if (rolling)
            {
                RollingUpdate();
                return;
            }
            if (!hasAlreadyRolled)
            {
                if (RWInput.CheckPauseButton(0))
                    Close(SoundID.MENU_Remove_Level, false);
                return;
            }
            QueueToStopRollingUpdate();
            if (FinishedShowingResults) StopRollingUpdate();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 selectPagepos = selectPage.DrawPos(timeStacker);
            titleName.x = selectPagepos.x + manager.rainWorld.options.ScreenSize.x / 2;
            titleName.y = selectPagepos.y + manager.rainWorld.options.ScreenSize.y - 80;
            congratsLabel.x = titleName.x;
            congratsLabel.y = titleName.y - 20;
            congratsLabel.alpha = congratsLabelAlpha > 0 ? 1 : 0;
            if (!showRainbow) return;
            Color rB = MyRainbowColor();
            titleName.color = Color.Lerp(Color.white, rB, rB.a);
            congratsLabel.color = titleName.color;
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "ROLL")
            {
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                rolling = true;
            }
            if (message == "STOP")
            {
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                rolling = false;
            }
            if (message == "CONTINUE")
                Close(SoundID.MENU_Remove_Level, true);
        }
        public string GetDescription()
        {
            System.Random random = new();
            if (IsMatching)
                return winningDescriptions[random.Next(winningDescriptions.Count)];
            if (IsCloseMatching)
                return closeDesriptions[random.Next(closeDesriptions.Count)];
            return losingDescriptions[random.Next(losingDescriptions.Count)];
        }
        public void Close(SoundID id, bool recieveSlugcat)
        {
            if (isClosing || queuedToClose) return;
            if (recieveSlugcat) RecieveSlugcat?.Invoke([.. slugcatRandomizers.Select(x => x.slugcatButton.slugcat)], this);
            desiredSelectPagePosY = -1500;
            queuedToClose = true;
            PlaySound(id);
        }
        public void RollingUpdate()
        {
            continueButton.buttonBehav.greyedOut = true;
            hasAlreadyRolled = true;
            if (myRollingCounter % startEndRollingOrderCounter == 0)
            {
                SlugcatRandomizer? randomizer = slugcatRandomizers.FirstOrDefault(x => !x.rolling);
                if (randomizer != null)
                {
                    StartRandomizer(randomizer);
                     myRollingCounter = 0;
                }
            }
            if (myRollingCounter >= endRollingCounter) rolling = false;
            myRollingCounter++;
        }
        public void QueueToStopRollingUpdate()
        {
            if (showResultsCounter % startEndRollingOrderCounter == 0)
            {
                StopRandomizer(slugcatRandomizers.FirstOrDefault(x => x.rolling));
                showResultsCounter = 0;
            }
            showResultsCounter++;
        }
        public void StopRollingUpdate()
        {
            resultShownCounter++;
            if (congratsLabel.text == "")
            {
                congratsLabel.text = Translate(GetDescription());
                if (IsMatching)
                    showRainbow = true;
                else PlaySound(SoundID.UI_Multiplayer_Game_Over, 0, 1, 1);
            }
            foreach (SlugcatRandomizer slugcatRandomizer in slugcatRandomizers)
            {
                if (IsMatching)
                {
                    slugcatRandomizer.resultsColor = MyRainbowColor();
                    slugcatRandomizer.flash = false;
                    continue;
                }
                slugcatRandomizer.Lost();
            }
            if (congratsLabelAlpha != 1)
            {
                congratsLabelAlpha = Custom.LerpAndTick(congratsLabel.alpha, 1, 0.1f, 0.01f);
                return;
            }
            if (resultShownCounter >= 10 && continueButton.signalText != "CONTINUE")
            {
                RainMeadow.Debug("Changed continue button to CONTINUE");
                continueButton.menuLabel.text = Translate("CONTINUE");
                continueButton.signalText = "CONTINUE";
                continueButton.buttonBehav.greyedOut = false;
                if (!manager.menuesMouseMode) selectedObject = continueButton;
            }
        }
        public void PlayQueuedSounds()
        {
            if (!IsMatching) return;
            if (resultShownCounter == 0 || resultShownCounter == 3)
                PlaySound(SoundID.Small_Needle_Worm_Intense_Trumpet_Scream, 0, 2, 1);

        }
        public void StartRandomizer(SlugcatRandomizer randomizer)
        {
            PlaySound(SoundID.UI_Multiplayer_Player_Revive);
            randomizer.rolling = true;
        }
        public void StopRandomizer(SlugcatRandomizer? randomizer)
        {
            if (randomizer == null) return;
            randomizer.StopRolling();
            PlaySound(SoundID.UI_Multiplayer_Player_Result_Box_Bump);
        }
        public float PopulateSlugcatRandomiers(SlugcatStats.Name[] currentSlugcats, SlugcatStats.Name[] selectableSlugcats, int buttonsPerRow, float portraitScale)
        {
            float continueButtonYPos = 0;
            List<SlugcatRandomizer> randomizers = [];
            int perRow = Mathf.Max(currentSlugcats.Length % 2 == 0 && buttonsPerRow % 2 != 0? buttonsPerRow - 1: buttonsPerRow, 1), howManyRows = ((currentSlugcats.Length - 1) / perRow) + 1, num = 0, howManyVisibleRows = Mathf.Min(howManyRows, 2);
            float yPosMultipler = (manager.rainWorld.options.ScreenSize.y) / (howManyVisibleRows + 1);
            while (num < howManyRows)
            {
                int numOfButtonsPassed = num * perRow, xAdder = 0, numOfButtonsInRow = Mathf.Min(perRow, currentSlugcats.Length - numOfButtonsPassed);
                float xMultipler = manager.rainWorld.options.ScreenSize.x / (numOfButtonsInRow + 1);
                float yPos = yPosMultipler * (howManyRows - num);
                for (int i = numOfButtonsPassed; i < currentSlugcats.Length && i < (num + 1) * perRow; i++)
                {
                    float xPos = xMultipler * (xAdder + (numOfButtonsInRow % 2 == 0 && perRow % 2 != 0 ? 0.5f : 1));
                    SlugcatRandomizer randomizer = new(this, selectPage, new(xPos, yPos), currentSlugcats[i], selectableSlugcats, portraitScale);
                    randomizers.Add(randomizer);
                    selectPage.SafeAddSubobjects(randomizer);
                    if (num == howManyVisibleRows - 1)
                        continueButtonYPos = randomizers[i].pos.y + randomizers[i].slugcatButton.pos.y - 80;
                    xAdder++;
                }
                num++;
            }
            slugcatRandomizers = [.. randomizers];
            return continueButtonYPos;

        }
        public Color MyRainbowColor(float alpha = 0.5f)
        {
            Color col = rainbowColor.rgb;
            col.a = alpha;
            return col;
        }
        public class SlugcatRandomizer : PositionedMenuObject
        {
            public float desiredResultPosY, lastResultPosY, resultPosY;
            public int resultsCounter, rollingCounter, desiredFlipPortraitCounter = 3;
            public bool rolling, hasAlreadyRolled, flash = true, lost;
            public Color resultsColor = Color.yellow;
            public SlugcatStats.Name[] slugcatList;
            public SlugcatColorableButton slugcatButton;
            public FLabel slugcatResult;
            public SlugcatRandomizer(Menu.Menu menu, MenuObject owner, Vector2 pos, SlugcatStats.Name name, SlugcatStats.Name[] slugcatList, float desiredScale = 1.2f) : base(menu, owner, pos)
            {
                this.slugcatList = slugcatList;
                desiredResultPosY = lastResultPosY = resultPosY = -150;
                float scaleOffset = 100 * desiredScale, offset = scaleOffset * 0.5f;
                slugcatButton = new(menu, this, new(-offset, -offset), new(scaleOffset, scaleOffset), slugcatList.IndexOf(name) == -1 ? slugcatList[0] : name, false)
                {
                    size = new(scaleOffset, scaleOffset) //cuz inv portraits are not scaled properly
                };
                slugcatButton.portrait.texture.filterMode = FilterMode.Bilinear;
                slugcatButton.portrait.sprite.scale = desiredScale;
                slugcatResult = new(Custom.GetDisplayFont(), "")
                {
                    anchorY = 1,
                    shader = menu.manager.rainWorld.Shaders["MenuTextCustom"]
                };
                Container.AddChild(slugcatResult);
                RecursiveRemoveSelectables(slugcatButton);
                this.SafeAddSubobjects(slugcatButton);

            }
            public override void Update()
            {
                base.Update();
                lastResultPosY = resultPosY;
                if (!rolling)
                {
                    if (!hasAlreadyRolled) return;
                    resultsCounter++;
                    slugcatButton.secondaryColor = resultsColor;
                    if (slugcatResult.text == "")
                    {
                        slugcatResult.text = menu.Translate("You got a <SLUGCATNAME>!").Replace("<SLUGCATNAME>", menu.Translate(SlugcatStats.getSlugcatName(slugcatButton.slugcat)));
                        desiredResultPosY = slugcatButton.pos.y - 10;
                    }
                    if (resultPosY != desiredResultPosY)
                    {
                        resultPosY = Custom.LerpAndTick(resultPosY, desiredResultPosY, 0.15f, 0.15f);
                        return;
                    }
                    return;
                }
                hasAlreadyRolled = true;
                if (rollingCounter % desiredFlipPortraitCounter == 0)
                {
                    int index = slugcatList.IndexOf(slugcatButton.slugcat) + 1;
                    if (index >= slugcatList.Length) index = 0;
                    LoadNewSlugcat(slugcatList[index], false);
                    menu.PlaySound(SoundID.MENU_Scroll_Tick);
                }
                rollingCounter++;
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                Vector2 screenPos = DrawPos(timeStacker);
                float resultPos = Mathf.Lerp(lastResultPosY, resultPosY, timeStacker), lerp  = Mathf.PingPong(resultsCounter * 0.75f, 40) / 40, 
                    flashLerp = 0.5f - 0.5f * Mathf.Sin((timeStacker + resultsCounter) / 30 * Mathf.PI * 2);
                slugcatButton.portraitSecondaryLerpFactor = flash? flashLerp : lerp;
                slugcatResult.x = screenPos.x;
                slugcatResult.y = screenPos.y + resultPos;
                slugcatResult.color = Color.Lerp(Color.white, resultsColor == Color.clear? MenuColorEffect.rgbVeryDarkGrey : resultsColor, flashLerp);
                slugcatResult.alpha = hasAlreadyRolled ? Mathf.InverseLerp(desiredResultPosY - 100, desiredResultPosY, resultPos) : 0;
            }
            public void LoadNewSlugcat(SlugcatStats.Name? name, bool isDead)
            {
                slugcatButton.LoadNewSlugcat(name, false, isDead);
            }
            public void Lost()
            {
                if (lost) return;
                lost = true;
                LoadNewSlugcat(slugcatButton.slugcat, true);
                slugcatButton.isBlackPortrait = true;
                resultsColor = Color.clear;
            }
            public void StopRolling(int index = -1)
            {
                rolling = false;
                if (index == -1)
                {
                    System.Random random = new();
                    slugcatButton.LoadNewSlugcat(slugcatList[random.Next(slugcatList.Length)], false, false);
                }
                else slugcatButton.LoadNewSlugcat(slugcatList[index], false, false);
            }
            public void StopRolling(SlugcatStats.Name? desiredSlugcat) => StopRolling(slugcatList.IndexOf(desiredSlugcat));
        }
        
    }
}
