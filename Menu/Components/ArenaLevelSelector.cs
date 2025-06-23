using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Menu;
using Menu.Remix;
using MoreSlugcats;
using RainMeadow.UI.Interfaces;
using RWCustom;
using UnityEngine;
using static Menu.Menu;
using static MultiplayerUnlocks;

namespace RainMeadow.UI.Components;

public class ArenaLevelSelector : PositionedMenuObject, IPLEASEUPDATEME
{
    //holy shit wtf is this
    public class LevelPreview : RectangularMenuObject
    {
        public LevelItem? lastSelectedLevel;
        public RoundedRect roundedRect;
        public FSprite imageSprite;
        public string levelName;
        public float toFade, lastToFade, yPos,goalYPos;
        public int sleepCounter, awakeCounter;
        public PlaylistSelector? MyPlaylistSelector => owner as PlaylistSelector;
        public LevelItem[]? LevelItems => [..MyPlaylistSelector?.LevelItems];
        public LevelPreview(Menu.Menu menu, MenuObject owner, bool rightFacing, float xoffset = 20) : base(menu, owner, default, new(ThumbWidth + 20, ThumbHeight + 20))
        {
            roundedRect = new(menu, this, new Vector2(0.01f, 0.01f), size, true);
            subObjects.Add(roundedRect);
            imageSprite = new("Menu_Empty_Level_Thumb", true)
            {
                color = MenuRGB(MenuColors.VeryDarkGrey)
            };
            Container.AddChild(imageSprite);
            levelName = "";
            pos.x = rightFacing ? ((owner as RectangularMenuObject)?.size ?? Vector2.zero).x + xoffset : -(size.x + xoffset);
        }
        public override void Update()
        {
            base.Update();
            lastToFade = toFade;
            awakeCounter++;
            sleepCounter++;
            LevelItem? levelItem = null;
            if (MyPlaylistSelector?.ShowThumbsTransitionState(1) == 0)
            {
                for (int i = 0; i < MyPlaylistSelector.LevelItems.Count; i++)
                {
                    if (MyPlaylistSelector.LevelItems[i].Selected)
                    {
                        levelItem = MyPlaylistSelector.LevelItems[i];
                        break;
                    }
                }
            }
            if (levelItem != null)
            {
                if (levelItem != lastSelectedLevel)
                {
                    awakeCounter = 0;
                    sleepCounter = 0;
                    if (levelItem.thumbLoaded)
                    {
                        levelName = levelItem.name;
                        imageSprite.element = Futile.atlasManager.GetElementWithName(levelName + "_Thumb");
                        imageSprite.color = Color.white;
                    }
                }
                lastSelectedLevel = levelItem;
            }
            else sleepCounter = 0;
            toFade = (levelItem != null && (awakeCounter > 60 || toFade > 0) && sleepCounter < 200) ? Custom.LerpAndTick(toFade, 1f, 0.03f, 0.033333335f) : Custom.LerpAndTick(toFade, 0f, 0.015f, 0.016666668f);
            Vector2 posOfOwner = (owner as PositionedMenuObject)?.ScreenPos ?? Vector2.zero, sizeOfOwner = (owner as RectangularMenuObject)?.size ?? Vector2.zero;
            if (menu.manager.menuesMouseMode)
            { 
                yPos = goalYPos = Mathf.Clamp(Futile.mousePosition.y, posOfOwner.y + size.y * 0.5f, posOfOwner.y + sizeOfOwner.y - size.y * 0.5f);
            }
            else
            {
                if (lastSelectedLevel?.Alpha == 1f) goalYPos = Mathf.Clamp(lastSelectedLevel.DrawY(1) + lastSelectedLevel.size.y * 0.5f, posOfOwner.y + size.y * 0.5f, posOfOwner.y + sizeOfOwner.y - size.y * 0.5f);
                yPos = Custom.LerpAndTick(yPos, goalYPos, 0.09f, 1.25f);
            }
            pos.y = yPos - posOfOwner.y - size.y * 0.5f;
        }
        public override void GrafUpdate(float timeStacker)
        {
            Vector2 drawPos = DrawPos(timeStacker), drawSize = DrawSize(timeStacker);
            imageSprite.x = drawPos.x + drawSize.x / 2;
            imageSprite.y = drawPos.y + drawSize.y / 2;
            float num = Custom.SCurve(Mathf.Lerp(lastToFade, toFade, timeStacker), 0.75f);
            imageSprite.alpha = Mathf.Pow(num, 0.7f);
            base.GrafUpdate(timeStacker);
            for (int i = 0; i < 17; i++)
            {
                roundedRect.sprites[i].color = i < 9? Color.black : MenuRGB(MenuColors.VeryDarkGrey);
                roundedRect.sprites[i].alpha = i < 9? 0.5f * Mathf.Pow(num, 1.5f) : num;
                roundedRect.sprites[i].isVisible = true;
            }
        }
        public override void RemoveSprites()
        {
            imageSprite.RemoveFromContainer();
            base.RemoveSprites();
        }
    }
    public class LevelItem : ButtonTemplate, ButtonScroller.IPartOfButtonScroller, IHaveADescription
    {
        public MenuLabel label;
        public FSprite thumbnailSprite;
        public FSprite? outlineDividerSprite, bkgDividerSprite;
        public RoundedRect roundedRect;
        public LevelItem? levelItemAbove, levelItemBelow;
        public bool thumbLoaded, lastSelected, doAThumbFade, showThumbDivider;
        public float labelSelectedBlink, labelLastSelectedBlink, lastFade, fade, thumbChangeFade, lastThumbChangeFade, fadeAway;
        public string name, description;
        public PlaylistSelector? MyPlaylistSelector => owner as PlaylistSelector;
        public bool ShowThumbDivider => ShowThumbsTransitionState(1f) > 0.5f;
        public float Alpha { get; set; }
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = new(value.x, thumbLoaded? value.y : size.y); }
        public string Description => description;
        public FContainer MyLevelContainer => MyPlaylistSelector?.levelContainer ?? Container;
        public FContainer MyDividerContainer => MyPlaylistSelector?.dividerContainer ?? Container;
        public override bool CurrentlySelectableMouse => !buttonBehav.greyedOut && Alpha > 0.5f;
        public override bool CurrentlySelectableNonMouse => !buttonBehav.greyedOut && Alpha > 0.5f;
        public LevelItem(Menu.Menu menu, MenuObject owner, string levelName, string description) : base(menu, owner, default, new Vector2(120f, 20f))
        {
            if (MyLevelContainer != Container)
            {
                myContainer.RemoveFromContainer(); //<- move to MyLevelContainer;
                MyLevelContainer.AddChild(Container);
            }
            name = levelName;
            this.description = description;
            buttonBehav = new(this);
            label = new(menu, this, LevelDisplayName(levelName), Vector2.zero, new Vector2(size.x, 20f), false);
            roundedRect = new(menu, this, Vector2.zero, new Vector2(size.x, size.y), true);

            thumbLoaded = MyPlaylistSelector?.MyLevelSelector?.IsThumbnailLoaded(name) == true;
            thumbnailSprite = thumbLoaded ? new FSprite($"{name}_Thumb") : new FSprite("Menu_Empty_Level_Thumb") { color = MenuRGB(MenuColors.DarkGrey) };

            this.SafeAddSubobjects(label, roundedRect);
            Container.AddChild(thumbnailSprite);
        }
        public override Color MyColor(float timeStacker)
        {
            if (buttonBehav.greyedOut) return MenuColor(MenuColors.DarkGrey).rgb;
            return HSLColor.Lerp(MenuColor(MenuColors.MediumGrey), MenuColor(MenuColors.White), Mathf.Max(Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker))).rgb;
        }
        public override void Clicked()
        {
            if (fade < 1 || fadeAway > 0) return;
            if (MyPlaylistSelector?.LevelItems?.Contains(this) == true)
                MyPlaylistSelector.LevelItemClicked(MyPlaylistSelector.LevelItems.IndexOf(this));

        }
        public override void Update()
        {
            base.Update();
            buttonBehav.greyedOut = MyPlaylistSelector?.MyLevelSelector?.ForceGreyOutAll == true;
            lastFade = fade;
            labelLastSelectedBlink = labelSelectedBlink;

            if (Selected)
            {
                if (!lastSelected) labelSelectedBlink = 0; 
                labelSelectedBlink = Mathf.Max(0, labelSelectedBlink - 1 / Mathf.Lerp(10, 40, labelSelectedBlink));
            }
            else labelSelectedBlink = 0f;
            lastSelected = Selected;
            if (owner is PlaylistSelector selector) size.y = selector.buttonHeight;
            lastThumbChangeFade = thumbChangeFade;
            if (doAThumbFade)
            {
                thumbChangeFade = Custom.LerpAndTick(thumbChangeFade, 1f, 0.08f, 1f / 30f);
                if (thumbChangeFade == 1f)
                {
                    thumbnailSprite.element = Futile.atlasManager.GetElementWithName(name + "_Thumb");
                    thumbnailSprite.color = new Color(1f, 1f, 1f);
                    doAThumbFade = false;
                }
            }
            else thumbChangeFade = Custom.LerpAndTick(thumbChangeFade, 0f, 0.08f, 1f / 30f);

            float desiredSquashFactor = 1;
            if (fadeAway > 0)
            {
                fadeAway += 0.1f;
                if (fadeAway >= 1)
                {
                    MyPlaylistSelector?.HandleLevelItemFade(this);
                    return;
                }
                desiredSquashFactor *= 1 - fadeAway;
            }
            fade = Custom.LerpAndTick(fade, desiredSquashFactor, 0.08f, 0.1f);
            fade = Mathf.Lerp(fade, desiredSquashFactor, Mathf.InverseLerp(0.5f, 0.45f, Mathf.Abs(0.5f - ShowThumbsTransitionState(1))));
            float num3 = Custom.SCurve(ShowThumbsTransitionState(1) * Mathf.InverseLerp(0, 0.8f, fade), 0.5f);
            roundedRect.size = new(size.x, size.y * (0.3f + 0.7f * Mathf.Pow(num3, 0.5f)));
            roundedRect.pos = new(0.01f, -0.49f + size.y * 0.125f * Mathf.Pow(1f - num3, 1.5f));
            roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);
            roundedRect.addSize = new Vector2(10f, 6f) * 0.5f * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * (float)Math.PI)) * (buttonBehav.clicked ? 0f : 1f); roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);

            if (!thumbLoaded && (Selected || Alpha > 0.5f))
                MyPlaylistSelector?.MyLevelSelector?.BumpUpThumbnailLoad(name);

            if (outlineDividerSprite != null && showThumbDivider != ShowThumbDivider)
            {
                showThumbDivider = ShowThumbDivider;
                outlineDividerSprite.element = Futile.atlasManager.GetElementWithName(showThumbDivider ? "listDivider2" : "listDivider");
                roundedRect.pos.y += 3f;
            }
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 myPos = DrawPos(timeStacker);
            float fadeFactor = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.3f),
                thumbTransitionState = ShowThumbsTransitionState(timeStacker),
                alphaThumbFactor = thumbTransitionState * Mathf.InverseLerp(0, 0.8f, fadeFactor); 

            thumbnailSprite.x = myPos.x + size.x / 2;
            thumbnailSprite.y = myPos.y + 20 + ThumbHeight * alphaThumbFactor / 2;
            thumbnailSprite.alpha = fadeFactor * (0.85f + 0.15f * Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker)) * Mathf.Pow(thumbTransitionState, 1.5f) * (1 - Mathf.Lerp(0, 0, timeStacker));
            thumbnailSprite.scaleX = ThumbWidth * (0.5f + 0.5f * Mathf.Pow(alphaThumbFactor, 0.3f)) / thumbnailSprite.element.sourcePixelSize.x;
            thumbnailSprite.scaleY = ThumbHeight * alphaThumbFactor / thumbnailSprite.element.sourcePixelSize.y;

            float buttonBehavSin = Mathf.Lerp(1, 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 15 * Mathf.PI),
                Mathf.Lerp(buttonBehav.lastExtraSizeBump, buttonBehav.extraSizeBump, timeStacker) * fadeFactor * Mathf.Lerp(1, 0.5f, thumbTransitionState)),
                a = Mathf.Max(Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));

            label.label.color = Color.Lerp(MenuRGB(MenuColors.Black), MyColor(timeStacker), Mathf.Lerp(fadeFactor * buttonBehavSin, UnityEngine.Random.value, Mathf.Lerp(labelLastSelectedBlink, labelSelectedBlink, timeStacker)));
            label.label.alpha = Mathf.Pow(fadeFactor, 2);
            Color color = HSLColor.Lerp(MenuColor(MenuColors.VeryDarkGrey), HSLColor.Lerp(MenuColor(MenuColors.DarkGrey), MenuColor(MenuColors.MediumGrey), buttonBehavSin), a).rgb,
                rectColor = Color.Lerp(MenuRGB(MenuColors.Black), MenuRGB(MenuColors.DarkGrey), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));

            if (outlineDividerSprite != null)
            {
                outlineDividerSprite.x = thumbnailSprite.x;
                outlineDividerSprite.y = Mathf.Lerp(myPos.y, levelItemBelow!.DrawY(timeStacker) + levelItemBelow!.DrawSize(timeStacker).y, 0.5f) - 1 * thumbTransitionState - (10 - MyPlaylistSelector?.buttonSpacing ?? 0);
                outlineDividerSprite.alpha = Mathf.Min(fadeFactor * Alpha, Custom.SCurve(Mathf.Lerp(levelItemBelow.lastFade, levelItemBelow.fade, timeStacker) * levelItemBelow.Alpha, 0.3f));
                if (showThumbDivider)
                {
                    outlineDividerSprite.alpha *= Mathf.InverseLerp(0.75f, 1f, thumbTransitionState);
                    outlineDividerSprite.scaleY = 0.5f + 0.5f * Mathf.InverseLerp(0.75f, 1, thumbTransitionState);
                    bkgDividerSprite!.x = outlineDividerSprite.x;
                    bkgDividerSprite!.y = outlineDividerSprite.y;
                    bkgDividerSprite!.scaleY = outlineDividerSprite.scaleY;
                    bkgDividerSprite!.alpha = outlineDividerSprite.alpha;
                    bkgDividerSprite!.isVisible = true;
                }
                else
                {
                    bkgDividerSprite!.isVisible = false;
                    outlineDividerSprite.y = myPos.y;
                    outlineDividerSprite.alpha *= Mathf.InverseLerp(0.25f, 0, thumbTransitionState);
                    outlineDividerSprite.scaleY = 1;
                }
            }
            for (int i = 0; i < 17; i++)
            {
                roundedRect.sprites[i].color = i < 9 ? rectColor : color;
                roundedRect.sprites[i].alpha = fadeFactor * thumbTransitionState * (i < 9 ? 0.5f : 1);
                roundedRect.sprites[i].isVisible = thumbTransitionState * fadeFactor > 0;
            }
        }
        public void AddDividers(LevelItem nxt)
        {
            showThumbDivider = ShowThumbDivider;
            outlineDividerSprite = new FSprite(showThumbDivider ? "listDivider2" : "listDivider");
            bkgDividerSprite = new FSprite("listDivider2bkg");
            outlineDividerSprite.color = MenuRGB(MenuColors.DarkGrey);
            bkgDividerSprite.color = Color.black;

            MyDividerContainer.AddChild(bkgDividerSprite);
            MyDividerContainer.AddChild(outlineDividerSprite);
            levelItemBelow = nxt;
            nxt.levelItemAbove = this;
        }
        public void ThumbnailHasBeenLoaded()
        {
            thumbLoaded = true;
            doAThumbFade = true;
        }
        public float ShowThumbsTransitionState(float timeStacker) => MyPlaylistSelector?.ShowThumbsTransitionState(timeStacker) ?? 1;
        public void StartFadeAway() => fadeAway = Mathf.Max(fadeAway, 0.01f);
    }
    public class PlaylistSelector : ButtonScroller
    {
        public const string AddOnClick = "Add level to playlist", RemoveOnClick = "Remove level from playlist";
        public FContainer dividerContainer, levelContainer;
        public SideButton showThumbsButton;
        public LevelPreview levelPreviewer;
        public float showThumbsTransitionState, lastShowThumbsTransitionState;
        public override float MaxVisibleItemsShown => (int)base.MaxVisibleItemsShown;
        public override float DownScrollOffset { get => SavedScrollPos; set => SavedScrollPos = Mathf.Clamp((int)value, 0, (int)MaxDownScroll); }
        public virtual bool ShowThumbsStatus
        {
            get => MyLevelSelector?.GetGameTypeSetup?.allLevelsThumbs == true;
            set
            {
                if (MyLevelSelector?.GetGameTypeSetup != null) MyLevelSelector.GetGameTypeSetup.allLevelsThumbs = value;
            }
        }
        public virtual int SavedScrollPos
        {
            get
            {
                if (MyLevelSelector?.GetGameTypeSetup != null) return MyLevelSelector.GetGameTypeSetup.allLevelsScroll;
                return 0;
            }
            set
            {
                if (MyLevelSelector?.GetGameTypeSetup != null) MyLevelSelector.GetGameTypeSetup.allLevelsScroll = value;
            }
        }
        public ArenaLevelSelector? MyLevelSelector => owner as ArenaLevelSelector;
        public List<LevelItem> LevelItems => [.. buttons.Cast<LevelItem>()];
        public PlaylistSelector(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, 5, 120, new(80, 10), sliderPosOffset: new(0, 9), sliderSizeYOffset: - 40, startEndWithSpacing: true)
        {
            showThumbsTransitionState = ShowThumbsStatus ? 1 : 0;
            AddScrollUpDownButtons(upButtonYPosOffset: 20, downButtonYPosOffset: -44);
            dividerContainer = new();
            Container.AddChild(dividerContainer);
            levelContainer = new();
            Container.AddChild(levelContainer);

            subObjects.Add(levelPreviewer = new LevelPreview(menu, this, this is PlaylistHolder));
            showThumbsButton = AddSideButton(ShowThumbsStatus? "Menu_Symbol_Show_Thumbs" : "Menu_Symbol_Show_List", signal: "THUMBS");
            showThumbsButton.OnClick += btn =>
             {
                 ShowThumbsStatus = !ShowThumbsStatus;
                 btn.UpdateSymbol(ShowThumbsStatus ? "Menu_Symbol_Show_Thumbs" : "Menu_Symbol_Show_List");
                 menu.PlaySound(ShowThumbsStatus ? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
             };
            LoadLevelsInit();
        }
        public override void RemoveSprites()
        {
            dividerContainer.RemoveFromContainer();
            levelContainer.RemoveFromContainer();
            base.RemoveSprites();
        }
        public override void Update()
        {
            base.Update();
            lastShowThumbsTransitionState = showThumbsTransitionState;
            showThumbsTransitionState = Custom.LerpAndTick(showThumbsTransitionState, ShowThumbsStatus ? 1f : 0f, 0.015f, 1f / 30f);

            if (showThumbsTransitionState > 0 && showThumbsTransitionState < 1) ConstrainScroll();

            buttonHeight = Mathf.Lerp(20, 30 + ThumbHeight, ShowThumbsTransitionState(1f));
            buttonSpacing = (buttonHeight - 20) / 6;
        }
        public override float GetIdealYPosWithScroll(int elementIndex)
        {
            float scrollPos = StepsDownOfItem(elementIndex) - scrollOffset;
            float idealScrollPos = UpperBound - scrollPos * ButtonHeightAndSpacing;
            LevelItem? lvlItem = buttons.GetValueOrDefault(elementIndex) as LevelItem;
            return idealScrollPos + (((lvlItem?.levelItemBelow != null)? 3 : 0) - (lvlItem?.levelItemAbove != null? 3 : 0) * showThumbsTransitionState);
        }
        public override float GetCurrentScrollOffset()
        {
            float scrollPos = base.GetCurrentScrollOffset();
            int intScrollPos = (int)scrollPos;
            if (intScrollPos > 0 && intScrollPos == Math.Max(0, buttons.Count - (MaxVisibleItemsShown - 1)))
            {
                for (int i = intScrollPos; i < buttons.Count; i++)
                {
                    if (buttons[i] is LevelItem lvlItem) scrollPos -= lvlItem.fadeAway;
                }
            }
            return scrollPos;
        }
        public float StepsDownOfItem(int itemIndex)
        {
            float num = 0f;
            for (int i = 0; i <= Math.Min(itemIndex, buttons.Count - 1); i++)
                num += ((i > 0) ? Mathf.Pow(Custom.SCurve(1 - (buttons[i - 1] is LevelItem lvlItem ? lvlItem.fadeAway : 0), 0.3f), 0.5f) : 1);
            return num;
        }
        public override float GetAmountOfAlphaByCrossingBounds(Vector2 combinedPos)
        {
            float y = combinedPos.y;
            float elementUpperBound = y + buttonHeight;
            return y < LowerBound? 1 - Math.Min(1, (LowerBound - y) / buttonHeight) : elementUpperBound > UpperBound ? 1 - Math.Min(1, (elementUpperBound - UpperBound) / buttonHeight) : 1;
        }
        public virtual void LoadLevelsInit()
        {
            if (MyLevelSelector == null) return;
            for (int i = 0; i < MyLevelSelector.allLevels.Count; i++)
                AddLevelItem(new(menu, this, MyLevelSelector.allLevels[i], AddOnClick));
            for (int i = 0; i < buttons.Count - 1; i++)
            {
                if (buttons[i] is not LevelItem levelItem || buttons[i + 1] is not LevelItem nextLevelItem) 
                    continue;
                if (MyLevelSelector.LevelListSortNumber(levelItem.name) != MyLevelSelector.LevelListSortNumber(nextLevelItem.name)) 
                    levelItem.AddDividers(nextLevelItem);
            }
        }
        public virtual void HandleLevelItemFade(LevelItem item) { }
        public virtual void LevelItemClicked(int index) => MyLevelSelector?.AddItemToSelectedList(MyLevelSelector.allLevels[index]);
        public void AddLevelItem(LevelItem item) => AddScrollObjects(item);
        public void RemoveLevelItem(LevelItem item, bool constrainScroll = true) => RemoveButton(item, constrainScroll);
        public float ShowThumbsTransitionState(float timeStacker) => Custom.SCurve(Mathf.Pow(Mathf.Max(0, Mathf.Lerp(lastShowThumbsTransitionState, showThumbsTransitionState, timeStacker)), 0.7f), 0.3f);
    }
    public class PlaylistHolder : PlaylistSelector
    {
        public SideButton clearButton, shuffleButton;
        public int clearAllCounter = -1, mismatchCounter;
        public override int SavedScrollPos
        {
            get
            {
                if (MyLevelSelector?.GetGameTypeSetup != null) return MyLevelSelector.GetGameTypeSetup.playListScroll;
                return 0;
            }
            set
            {
                if (MyLevelSelector?.GetGameTypeSetup != null) MyLevelSelector.GetGameTypeSetup.playListScroll = value;
            }
        }
        public override bool ShowThumbsStatus
        {
            get => MyLevelSelector?.GetGameTypeSetup?.playListThumbs == true;
            set
            {
                if (MyLevelSelector?.GetGameTypeSetup != null) MyLevelSelector.GetGameTypeSetup.playListThumbs = value;
            }
        }
        public bool ShuffleStatus
        {
            get => MyLevelSelector?.GetGameTypeSetup?.shufflePlaylist == true;
            set
            {
                if (MyLevelSelector?.GetGameTypeSetup != null) MyLevelSelector.GetGameTypeSetup.shufflePlaylist = value;
            }
        }
        public bool IsMismatched => MyLevelSelector?.SelectedPlayList != null && (LevelItems.Count != MyLevelSelector.SelectedPlayList.Count || !MyLevelSelector.SelectedPlayList.SequenceEqual(LevelItems.Select(x => x.name)));

        public PlaylistHolder(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            clearButton = AddSideButton("Menu_Symbol_Clear_All", menu.Translate("Clear playlist"), menu.Translate("Clear playlist"), "");
            clearButton.maintainOutlineColorWhenGreyedOut = true;
            clearButton.OnClick += _ =>
            {
                clearAllCounter = 1;
                menu.PlaySound(_.buttonBehav.greyedOut ? SoundID.MENU_Button_Standard_Button_Pressed : SoundID.MENU_Greyed_Out_Button_Clicked);
            };
            shuffleButton = AddSideButton(ShuffleStatus ? "Menu_Symbol_Shuffle" : "Menu_Symbol_Dont_Shuffle", menu.Translate(ShuffleStatus ? "Shuffling Levels" : "Playing in order"), "", "SHUFFLE");
            shuffleButton.OnClick += _ =>
            {
                ShuffleStatus = !ShuffleStatus;
                _.label.text = menu.Translate(ShuffleStatus ? "Shuffling Levels" : "Playing in order");
                _.UpdateSymbol(ShuffleStatus ? "Menu_Symbol_Shuffle" : "Menu_Symbol_Dont_Shuffle");
                menu.PlaySound(ShuffleStatus ? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
            };
        }
        public override void Update()
        {
            base.Update();
            UpdatePlaylist();
        }
        public override void LevelItemClicked(int index)
        {
            if (MyLevelSelector == null) return;
            MyLevelSelector.RemoveLevelFromPlayList(index);
            menu.selectedObject = null;
            if (!menu.manager.menuesMouseMode)
            {
                int num = index - 1;
                while (num >= 0 && num < LevelItems.Count)
                {
                    if (LevelItems[num].fadeAway == 0)
                    {
                        menu.selectedObject = LevelItems[num];
                        break;
                    }
                    num--;
                }
                if (menu.selectedObject == null)
                {
                    int num2 = index + 1;
                    while (num2 >= 0 && num2 < LevelItems.Count)
                    {
                        if (LevelItems[num2].fadeAway == 0)
                        {
                            menu.selectedObject = LevelItems[num2];
                            break;
                        }
                        num2++;
                    }
                }
            }
            LevelItems[index].StartFadeAway();
        }
        public override void HandleLevelItemFade(LevelItem item) => RemoveLevelItem(item, true);
        public override void LoadLevelsInit()
        {
            if (MyLevelSelector?.SelectedPlayList == null) return;
            for (int j = 0; j < MyLevelSelector.SelectedPlayList.Count; j++)
                AddLevelItem(new LevelItem(menu, this, MyLevelSelector.SelectedPlayList[j], "Remove level from playlist"));
        }
        public void UpdatePlaylist()
        {
            clearButton.buttonBehav.greyedOut = LevelItems.Count == 0 || clearAllCounter > 0 || MyLevelSelector?.ForceGreyOutAll == true;
            if (clearAllCounter > 0)
            {
                clearAllCounter--;
                if (clearAllCounter < 1 && buttons.Count > 0)
                {
                    clearAllCounter = 4;
                    bool isClearingObj = false;
                    for (int i = LevelItems.Count - 1; i >= 0; i--)
                    {
                        if (LevelItems[i].fadeAway == 0)
                        {
                            isClearingObj = true;
                            LevelItems[i].StartFadeAway();
                            RemovePlaylistLevelItem(LevelItems[i]);
                            ConstrainScroll();
                            break;
                        }
                    }
                    if (!isClearingObj) MyLevelSelector?.SelectedPlayList?.Clear();
                }
                return;
            }
            mismatchCounter = IsMismatched ? mismatchCounter + 1 : 0;
            if (mismatchCounter == 80) ResolvePlaylistMismatch();

        }
        public void ResolvePlaylistMismatch()
        {
            if (MyLevelSelector?.SelectedPlayList == null) return;
            for (int i = LevelItems.Count - 1; i >= 0; i--)
                RemoveLevelItem(LevelItems[i], false);
            for (int j = 0; j < MyLevelSelector.SelectedPlayList.Count; j++)
                AddLevelItem(new LevelItem(menu, this, MyLevelSelector.SelectedPlayList[j], "Remove level from playlist"));
            ConstrainScroll();
            mismatchCounter = 0;

        }
        public void RemovePlaylistLevelItem(LevelItem levelItem)
        {
            if (MyLevelSelector?.SelectedPlayList == null) return;
            for (int i = MyLevelSelector.SelectedPlayList.Count - 1; i >= 0; i--)
            {
                if (MyLevelSelector.SelectedPlayList[i] == levelItem.name)
                {
                    MyLevelSelector.RemoveLevelFromPlayList(i);
                    break;
                }
            }

        }

    }


    public PlaylistSelector allLevelsPlaylist;
    public PlaylistHolder selectedLevelsPlaylist;
    public List<LevelUnlockID> unlockBatchIds = [];
    public List<string> allLevels = [], thumbsToBeLoaded = [], loadedThumbTextures = [];
    public int thumbLoadDelay;
    public static int ThumbWidth => 100;
    public static int ThumbHeight => 50;
    public ArenaSetup GetArenaSetup => menu.manager.arenaSetup;
    public ArenaSetup.GameTypeID CurrentGameType => GetArenaSetup.currentGameType;
    public ArenaSetup.GameTypeSetup GetGameTypeSetup => GetArenaSetup.GetOrInitiateGameTypeSetup(CurrentGameType);
    public List<string> SelectedPlayList => GetGameTypeSetup.playList;
    public bool ForceGreyOutAll => !(OnlineManager.lobby?.isOwner == true);
    public bool IsHidden { get; set; }
    public ArenaLevelSelector(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {

        (owner?.Container ?? menu.container).AddChild(myContainer = new());
        for (int i = LevelUnlockID.Hidden.Index + 1; i < ExtEnum<LevelUnlockID>.values.Count; i++)
        {
            string entry = ExtEnum<LevelUnlockID>.values.GetEntry(i);
            LevelUnlockID unlockId = new(entry);
            unlockBatchIds.Add(unlockId);
        }
        // move Hub and challenge maps to the bottom of the list since most challenge maps are not too friendly for normal arena play, and Hub image is just ugly to look at
        unlockBatchIds.Add(LevelUnlockID.Hidden);
        if (ModManager.MSC)
        {
            unlockBatchIds.Remove(MoreSlugcatsEnums.LevelUnlockID.ChallengeOnly);
            unlockBatchIds.Add(MoreSlugcatsEnums.LevelUnlockID.ChallengeOnly);
        }

        string[] rawLevelDirData = AssetManager.ListDirectory("Levels");
        for (int j = 0; j < rawLevelDirData.Length; j++)
        {
            if (rawLevelDirData[j].Substring(rawLevelDirData[j].Length - 4, 4) != ".txt" || rawLevelDirData[j].Substring(rawLevelDirData[j].Length - 13, 13) == "_settings.txt" || rawLevelDirData[j].Substring(rawLevelDirData[j].Length - 10, 10) == "_arena.txt" || rawLevelDirData[j].Contains("unlockall"))
                continue;

            string[] levelName = rawLevelDirData[j].Substring(0, rawLevelDirData[j].Length - 4).Split(Path.DirectorySeparatorChar);
            allLevels.Add(levelName[levelName.Length - 1]);
        }

        allLevels.Sort((A, B) => LevelListSortString(A).CompareTo(LevelListSortString(B)));

        thumbsToBeLoaded = [.. allLevels];

        allLevelsPlaylist = new(menu, this, default);
        selectedLevelsPlaylist = new(menu, this, new Vector2(200, 0));
     
        this.SafeAddSubobjects(allLevelsPlaylist, selectedLevelsPlaylist);
    }
    public override void Update()
    {
        base.Update();
        LoadThumbSprite();
    }
    public void LoadNewPlaylist(List<string> playlist, bool addToOwnPlaylist)
    {
        List<string>? playListToClear = addToOwnPlaylist ? SelectedPlayList : playlist,
            playListToRead = addToOwnPlaylist ? playlist : SelectedPlayList;
        playListToClear?.Clear();
        if (playListToRead != null) playListToClear?.AddRange(playListToRead);
    }
    public int LevelListSortNumber(string levelName)
    {
        LevelUnlockID levelUnlockID = LevelLockID(levelName);
        if (levelUnlockID == LevelUnlockID.Default)
            return 0;

        for (int i = 0; i < unlockBatchIds.Count; i++)
            if (unlockBatchIds[i] == levelUnlockID)
                return 1 + i;

        return 0;
    }
    public string LevelListSortString(string levelName) => LevelListSortNumber(levelName).ToString("000") + LevelDisplayName(levelName);
    public void AddItemToSelectedList(string name)
    {
        SelectedPlayList.Add(name);
        LevelItem item = new(menu, selectedLevelsPlaylist, name, "Remove level from playlist");
        selectedLevelsPlaylist.AddLevelItem(item);
        selectedLevelsPlaylist.DownScrollOffset = selectedLevelsPlaylist.MaxDownScroll;
        selectedLevelsPlaylist.ConstrainScroll();
        menu.PlaySound(SoundID.MENU_Add_Level);
    }
    public void RemoveLevelFromPlayList(int index)
    {
        if (index < 0 || index >= SelectedPlayList.Count) return;
        SelectedPlayList.RemoveAt(index);
        menu.PlaySound(SoundID.MENU_Remove_Level);
    }
    public bool IsThumbnailLoaded(string levelName) => loadedThumbTextures.Contains(levelName);
    public void BumpUpThumbnailLoad(string levelName)
    {
        if (thumbsToBeLoaded.Count > 0 && thumbsToBeLoaded[0] == levelName) return;
        if (IsThumbnailLoaded(levelName)) return;
        for (int i = thumbsToBeLoaded.Count - 1; i >= 0; i--)
            if (thumbsToBeLoaded[i] == levelName) thumbsToBeLoaded.RemoveAt(i);
        thumbsToBeLoaded.Insert(0, levelName);
    }
    public void LoadThumbSprite()
    {
        if (thumbLoadDelay > 0) thumbLoadDelay--;

        if (thumbsToBeLoaded.Count <= 0 || thumbLoadDelay >= 1) return;

        string thumbToBeLoaded = thumbsToBeLoaded[0];
        thumbsToBeLoaded.RemoveAt(0);

        string renderedThumbnailPath = AssetManager.ResolveFilePath($"Levels{Path.DirectorySeparatorChar}{thumbToBeLoaded}_Thumb.png");
        string thumbnailPath;

        bool hasRenderedThumbnail;
        if (File.Exists(renderedThumbnailPath))
        {
            thumbnailPath = renderedThumbnailPath;
            hasRenderedThumbnail = true;
        }
        else
        {
            thumbnailPath = AssetManager.ResolveFilePath($"Levels{Path.DirectorySeparatorChar}{thumbToBeLoaded}_1.png");
            hasRenderedThumbnail = false;
        }

        Texture2D texture2D = new(1, 1, TextureFormat.ARGB32, mipChain: false);
        if (File.Exists(thumbnailPath))
        {
            AssetManager.SafeWWWLoadTexture(ref texture2D, "file:///" + thumbnailPath, clampWrapMode: true, crispPixels: false);
            if (!hasRenderedThumbnail)
                TextureScale.Bilinear(texture2D, ThumbWidth, ThumbHeight);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 5; j++)
                    texture2D.SetPixel((i <= 1) ? (ThumbWidth - 1) : 0, (i % 2 == 0) ? j : (ThumbHeight - 1 - j), new Color(0f, 0f, 0f, 0f));

                for (int k = 0; k < 3; k++)
                    texture2D.SetPixel((i > 1) ? 1 : (ThumbWidth - 2), (i % 2 == 0) ? k : (ThumbHeight - 1 - k), new Color(0f, 0f, 0f, 0f));

                for (int l = 0; l < 2; l++)
                    texture2D.SetPixel((i > 1) ? 2 : (ThumbWidth - 3), (i % 2 == 0) ? l : (ThumbHeight - 1 - l), new Color(0f, 0f, 0f, 0f));

                texture2D.SetPixel((i > 1) ? 3 : (ThumbWidth - 4), (i % 2 != 0) ? (ThumbHeight - 1) : 0, new Color(0f, 0f, 0f, 0f));
                texture2D.SetPixel((i > 1) ? 4 : (ThumbWidth - 5), (i % 2 != 0) ? (ThumbHeight - 1) : 0, new Color(0f, 0f, 0f, 0f));
            }
        }

        texture2D.filterMode = FilterMode.Point;
        texture2D.Apply();

        loadedThumbTextures.Add(thumbToBeLoaded);
        HeavyTexturesCache.LoadAndCacheAtlasFromTexture($"{thumbToBeLoaded}_Thumb", texture2D, textureFromAsset: false);

        ButtonScroller.IPartOfButtonScroller[] levelItems = [.. allLevelsPlaylist.buttons, .. selectedLevelsPlaylist.buttons];
        for (int i = 0; i < levelItems.Length; i++)
        {
            if (levelItems[i] is not LevelItem levelItem || levelItem.name != thumbToBeLoaded) continue;
            levelItem.ThumbnailHasBeenLoaded();
        }

        thumbLoadDelay = 2;
    }
}