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

public class ArenaLevelSelector : PositionedMenuObject, ICanHideMenuObject
{
    //holy shit wtf is this
    public class LevelItem : ButtonTemplate, ButtonScroller.IPartOfButtonScroller, IHaveADescription, ICanHideMenuObject
    {
        public MenuLabel label;
        public FSprite thumbnailSprite;
        public FSprite? dividerSprite, dividerSprite2;
        public RoundedRect roundedRect;
        public LevelItem? dividerAbove, dividerBelow;
        public bool thumbLoaded, lastSelected, doAThumbFade;
        public float labelSelectedBlink, labelLastSelectedBlink, lastAlpha, thumbChangeFade, lastThumbChangeFade, fadeAway;
        public string name, description;
        public PlaylistSelector? MyPlaylistSelector => owner as PlaylistSelector;
        public bool ShowThumbDivider => ShowThumbsTransitionState(1f) > 0.5f;
        public float Alpha { get; set; }
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public string Description => description;

        public LevelItem(Menu.Menu menu, MenuObject owner, string levelName, string description) : base(menu, owner, default, new Vector2(120f, 20f))
        {
            name = levelName;
            this.description = description;
            buttonBehav = new ButtonBehavior(this);
            label = new MenuLabel(menu, this, LevelDisplayName(levelName), default, new Vector2(size.x, 20f), false);
            roundedRect = new RoundedRect(menu, this, default, new Vector2(size.x, size.y), true);

            thumbLoaded = owner.owner is ArenaLevelSelector levelSelector && levelSelector.IsThumbnailLoaded(name);
            thumbnailSprite = thumbLoaded ? new FSprite($"{name}_Thumb") : new FSprite("Menu_Empty_Level_Thumb") { color = MenuRGB(MenuColors.DarkGrey) };

            if (thumbLoaded && owner is VerticalScrollSelector scrollSelector) size.y = scrollSelector.elementHeight;

            this.SafeAddSubobjects(label, roundedRect);
            Container.AddChild(thumbnailSprite);
        }


        public override Color MyColor(float timeStacker)
        {
            if (buttonBehav.greyedOut)
            {
                return HSLColor.Lerp(MenuColor(MenuColors.DarkGrey), MenuColor(MenuColors.Black), 0).rgb;
            }

            float a = Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker);
            a = Mathf.Max(a, Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
            HSLColor hSLColor = HSLColor.Lerp(MenuColor(MenuColors.MediumGrey), MenuColor(MenuColors.White), a);
            return HSLColor.Lerp(hSLColor, MenuColor(MenuColors.Black), 0).rgb;
        }

        public override void Update()
        {
            base.Update();

            labelLastSelectedBlink = labelSelectedBlink;
            lastAlpha = Alpha;

            if (Selected)
            {
                if (!lastSelected) labelSelectedBlink = 0;
                labelSelectedBlink = Mathf.Max(0f, labelSelectedBlink - 1f / Mathf.Lerp(10f, 40f, labelSelectedBlink));
            }
            else labelSelectedBlink = 0f;

            if (owner is PlaylistSelector selector)
                size.y = selector.elementHeight * ShowThumbsTransitionState(1f);

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
            if (fadeAway > 0)
            {
                fadeAway += 0.1f;
                if (fadeAway >= 1)
                {
                    MyPlaylistSelector?.HandleLevelItemFade(this);
                    return;
                }
            }
            float num3 = Custom.SCurve(ShowThumbsTransitionState(1f) * Mathf.InverseLerp(0f, 0.8f, Alpha), 0.5f);
            roundedRect.size = new Vector2(size.x, size.y * (0.3f + 0.7f * Mathf.Pow(num3, 0.5f)));
            roundedRect.pos = new Vector2(0.01f, -0.49f + size.y * 0.125f * Mathf.Pow(1f - num3, 1.5f));
            roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);
            roundedRect.addSize = new Vector2(10f, 6f) * 0.5f * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * (float)Math.PI)) * (buttonBehav.clicked ? 0f : 1f); roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);

            if (!thumbLoaded && (Selected || Alpha > 0.5f))
                MyPlaylistSelector?.MyLevelSelector?.BumpUpThumbnailLoad(name);

            if (dividerSprite is not null)
            {
                dividerSprite.element = Futile.atlasManager.GetElementWithName(ShowThumbDivider ? "listDivider2" : "listDivider");
                roundedRect.pos.y += 3f;
            }
            if (dividerAbove is not null) roundedRect.pos.y -= 3f;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            float thumbTransitionState = ShowThumbsTransitionState(timeStacker);
            float num3 = thumbTransitionState * Mathf.InverseLerp(0f, 0.8f, Alpha);

            thumbnailSprite.x = DrawX(timeStacker) + size.x / 2f;
            thumbnailSprite.y = DrawY(timeStacker) + 20f + ThumbHeight * num3 / 2f;
            thumbnailSprite.alpha = Alpha * (0.85f + 0.15f * Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker)) * Mathf.Pow(thumbTransitionState, 1.5f) * (1f - Mathf.Lerp(0, 0, timeStacker));
            thumbnailSprite.scaleX = (float)ThumbWidth * (0.5f + 0.5f * Mathf.Pow(num3, 0.3f)) / thumbnailSprite.element.sourcePixelSize.x;
            thumbnailSprite.scaleY = (float)ThumbHeight * num3 / thumbnailSprite.element.sourcePixelSize.y;

            float a = Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker);
            a = Mathf.Max(a, Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
            float num4 = Mathf.Lerp(1f, 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * (float)Math.PI * 2f), Mathf.Lerp(buttonBehav.lastExtraSizeBump, buttonBehav.extraSizeBump, timeStacker) * Alpha * Mathf.Lerp(1f, 0.5f, 1));

            label.label.color = Color.Lerp(MenuRGB(MenuColors.Black), MyColor(timeStacker), Mathf.Lerp(Alpha * num4, UnityEngine.Random.value, Mathf.Lerp(labelLastSelectedBlink, labelSelectedBlink, timeStacker)));

            Color color = HSLColor.Lerp(MenuColor(MenuColors.VeryDarkGrey), HSLColor.Lerp(MenuColor(MenuColors.DarkGrey), MenuColor(MenuColors.MediumGrey), num4), a).rgb;
            Color rectColor = Color.Lerp(MenuRGB(MenuColors.Black), MenuRGB(MenuColors.DarkGrey), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));

            if (dividerSprite is not null)
            {
                dividerSprite.x = thumbnailSprite.x;
                dividerSprite.y = Mathf.Lerp(DrawY(timeStacker), dividerBelow!.DrawY(timeStacker) + dividerBelow!.DrawSize(timeStacker).y, 0.5f) - 1f * thumbTransitionState - (10 - (owner as PlaylistSelector)?.elementSpacing ?? 0);
                dividerSprite.alpha = Mathf.Min(Alpha, Custom.SCurve(Mathf.Lerp(dividerBelow!.lastAlpha, dividerBelow!.Alpha, timeStacker), 0.3f));
                if (ShowThumbDivider)
                {
                    dividerSprite.alpha *= Mathf.InverseLerp(0.75f, 1f, thumbTransitionState);
                    dividerSprite.scaleY = 0.5f + 0.5f * Mathf.InverseLerp(0.75f, 1f, thumbTransitionState);
                    dividerSprite2!.x = dividerSprite.x;
                    dividerSprite2!.y = dividerSprite.y;
                    dividerSprite2!.scaleY = dividerSprite.scaleY;
                    dividerSprite2!.alpha = dividerSprite.alpha;
                }
                else
                {
                    dividerSprite.alpha *= Mathf.InverseLerp(0.25f, 0f, thumbTransitionState);
                    dividerSprite.scaleY = 1f;
                }
            }

            if (ShowThumbsTransitionState(timeStacker) > 0f)
            {
                for (int i = 0; i < 9; i++)
                {
                    roundedRect.sprites[i].color = rectColor;
                    roundedRect.sprites[i].alpha = Alpha * thumbTransitionState * 0.5f;
                    roundedRect.sprites[i].isVisible = true;
                }

                for (int j = 9; j < 17; j++)
                {
                    roundedRect.sprites[j].color = color;
                    roundedRect.sprites[j].alpha = Alpha * thumbTransitionState;
                    roundedRect.sprites[j].isVisible = true;
                }
            }
            else
                for (int i = 0; i < 17; i++)
                    roundedRect.sprites[i].isVisible = false;
        }
        public override void Clicked()
        {
            if (MyPlaylistSelector?.LevelItems?.Contains(this) == true) 
                MyPlaylistSelector.LevelItemClicked(MyPlaylistSelector.LevelItems.IndexOf(this));

        }
        public void HiddenUpdate() => Update();
        public void HiddenGrafUpdate(float timeStacker) => GrafUpdate(timeStacker);
        public void AddDividers(LevelItem nxt)
        {
            dividerSprite = new FSprite(ShowThumbDivider ? "listDivider2" : "listDivider");
            dividerSprite2 = new FSprite("listDivider2bkg");
            dividerSprite.color = MenuRGB(MenuColors.DarkGrey);
            dividerSprite2.color = Color.black;

            dividerBelow = nxt;
            nxt.dividerAbove = this;

            Container.AddChild(dividerSprite);
            (owner as PlaylistSelector)?.dividerContainer.AddChild(dividerSprite2);
        }
        public void ThumbnailHasBeenLoaded()
        {
            thumbLoaded = true;
            doAThumbFade = true;
        }
        public float ShowThumbsTransitionState(float timeStacker) => MyPlaylistSelector?.ShowThumbsTransitionState(timeStacker) ?? 1f;
        public void StartFadeAway() => fadeAway = Mathf.Max(fadeAway, 0.01f);
    }

    public class PlaylistSelector : VerticalScrollSelector
    {
        public const string AddOnClick = "Add level to playlist", RemoveOnClick = "Remove level from playlist";
        public FContainer dividerContainer;
        public SideButton showThumbsButton;
        public float showThumbsTransitionState, lastShowThumbsTransitionState;
        public override int ScrollPos
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
        public virtual bool ShowThumbsStatus
        {
            get => MyLevelSelector?.GetGameTypeSetup?.allLevelsThumbs == true;
            set
            {
                if (MyLevelSelector?.GetGameTypeSetup != null) MyLevelSelector.GetGameTypeSetup.allLevelsThumbs = value;
            }
        }
        public ArenaLevelSelector? MyLevelSelector => owner as ArenaLevelSelector;
        public List<LevelItem> LevelItems => [.. scrollElements.Cast<LevelItem>()];
        public PlaylistSelector(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, new Vector2(120f, 80f), 5)
        {
            RainMeadow.DebugMe();
            dividerContainer = new FContainer();

            scrollUpButton!.pos.y += 10f;
            scrollDownButton!.pos.y -= 10f;

            showThumbsButton = AddSideButton(ShowThumbsStatus? "Menu_Symbol_Show_Thumbs" : "Menu_Symbol_Show_List", signal: "THUMBS");
            showThumbsButton.OnClick += btn =>
             {
                 ShowThumbsStatus = !ShowThumbsStatus;
                 btn.UpdateSymbol(ShowThumbsStatus ? "Menu_Symbol_Show_Thumbs" : "Menu_Symbol_Show_List");
                 menu.PlaySound(ShowThumbsStatus ? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
             };
            LoadLevelsInit();
            RainMeadow.DebugMe();
        }
        public override void RemoveSprites()
        {
            this.dividerContainer.RemoveFromContainer();
            base.RemoveSprites();
        }
        public override void HiddenUpdate()
        {
            base.HiddenUpdate();
            lastShowThumbsTransitionState = showThumbsTransitionState;
            showThumbsTransitionState = Custom.LerpAndTick(showThumbsTransitionState, ShowThumbsStatus ? 1f : 0f, 0.015f, 1f / 30f);

            if (showThumbsTransitionState > 0f && showThumbsTransitionState < 1f) ConstrainScroll();

            elementHeight = Mathf.Lerp(20f, 30f + ThumbHeight, ShowThumbsTransitionState(1f));
            elementSpacing = (elementHeight - 20) / 6;
        }
        public override void HiddenGrafUpdate(float timeStacker) => base.HiddenGrafUpdate(timeStacker);
        public virtual void LoadLevelsInit()
        {
            if (MyLevelSelector == null) return;
            for (int i = 0; i < MyLevelSelector.allLevels.Count; i++)
                AddLevelItem(new(menu, this, MyLevelSelector.allLevels[i], AddOnClick));
            for (int i = 0; i < scrollElements.Count - 1; i++)
            {
                if (scrollElements[i] is not LevelItem levelItem || scrollElements[i + 1] is not LevelItem nextLevelItem) 
                    continue;
                if (MyLevelSelector.LevelListSortNumber(levelItem.name) != MyLevelSelector.LevelListSortNumber(nextLevelItem.name)) 
                    levelItem.AddDividers(nextLevelItem);
            }
            ConstrainScroll();
        }
        public virtual void HandleLevelItemFade(LevelItem item) { }
        public virtual void LevelItemClicked(int index) => MyLevelSelector?.AddItemToSelectedList(MyLevelSelector.allLevels[index]);
        public void AddLevelItem(LevelItem item) => AddScrollElements(item);
        public void RemoveLevelItem(LevelItem item, bool constrainScroll = true)
        {
            this.ClearMenuObject(item);
            RemoveScrollElements(constrainScroll, item);
        }
        public float ShowThumbsTransitionState(float timeStacker) => Custom.SCurve(Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lastShowThumbsTransitionState, showThumbsTransitionState, timeStacker)), 0.7f), 0.3f);
    }
    public class PlaylistHolder : PlaylistSelector
    {
        public SideButton clearButton, shuffleButton;
        public int clearAllCounter = -1, mismatchCounter;
        public override int ScrollPos
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
        public bool IsMismatched => MyLevelSelector != null && LevelItems.Count != MyLevelSelector.PlayList.Count;

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
        public override void HiddenUpdate()
        {
            base.HiddenUpdate();
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
        public override void LoadLevelsInit() => ResolvePlaylistMismatch();
        public void UpdatePlaylist()
        {
            clearButton.buttonBehav.greyedOut = LevelItems.Count == 0 || clearAllCounter > 0;
            if (clearAllCounter > 0)
            {
                clearAllCounter--;
                if (clearAllCounter < 1 && scrollElements.Count > 0)
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
                    if (!isClearingObj) MyLevelSelector?.PlayList?.Clear();
                }
                return;
            }
            mismatchCounter = IsMismatched ? mismatchCounter + 1 : 0;
            if (mismatchCounter == 80) ResolvePlaylistMismatch();

        }
        public void ResolvePlaylistMismatch()
        {
            if (MyLevelSelector == null) return;
            for (int i = LevelItems.Count - 1; i >= 0; i--)
                RemoveLevelItem(LevelItems[i], false);
            for (int j = 0; j < MyLevelSelector.PlayList.Count; j++)
                AddLevelItem(new LevelItem(menu, this, MyLevelSelector.PlayList[j], "Remove level from playlist"));
            ConstrainScroll();
            mismatchCounter = 0;

        }
        public void RemovePlaylistLevelItem(LevelItem levelItem)
        {
            if (MyLevelSelector == null) return;
            for (int i = MyLevelSelector.PlayList.Count - 1; i >= 0; i--)
            {
                if (MyLevelSelector.PlayList[i] == levelItem.name)
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

    public List<string> PlayList => GetGameTypeSetup.playList;

    public ArenaLevelSelector(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {

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
    public void HiddenUpdate() => LoadThumbSprite();
    public void HiddenGrafUpdate(float timeStacker)
    {
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
        PlayList.Add(name);
        LevelItem item = new(menu, selectedLevelsPlaylist, name, "Remove level from playlist");
        selectedLevelsPlaylist.AddLevelItem(item);
        selectedLevelsPlaylist.ScrollPos = selectedLevelsPlaylist.MaximumScrollPos;
        selectedLevelsPlaylist.ConstrainScroll();
        menu.PlaySound(SoundID.MENU_Add_Level);
    }
    public void RemoveLevelFromPlayList(int index)
    {
        if (index < 0 || index >= PlayList.Count) return;
        PlayList.RemoveAt(index);
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

        ButtonScroller.IPartOfButtonScroller[] levelItems = [.. allLevelsPlaylist.scrollElements, .. selectedLevelsPlaylist.scrollElements];
        for (int i = 0; i < levelItems.Length; i++)
        {
            if (levelItems[i] is not LevelItem levelItem || levelItem.name != thumbToBeLoaded) continue;
            levelItem.ThumbnailHasBeenLoaded();
        }

        thumbLoadDelay = 2;
    }
}