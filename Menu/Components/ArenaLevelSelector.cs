using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Menu;
using Menu.Remix;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using static Menu.Menu;
using static MultiplayerUnlocks;

namespace RainMeadow.UI.Components;

public class ArenaLevelSelector : PositionedMenuObject
{
    //holy shit wtf is this
    public class LevelItem : ButtonTemplate, ButtonScroller.IPartOfButtonScroller, IHaveADescription
    {
        public MenuLabel label;
        public FSprite thumbnailSprite;
        public FSprite? dividerSprite, dividerSprite2;
        public RoundedRect roundedRect;
        public LevelItem? dividerAbove, dividerBelow;
        public bool thumbLoaded, lastSelected, doAThumbFade;
        public float labelSelectedBlink, labelLastSelectedBlink, lastAlpha, thumbChangeFade, lastThumbChangeFade;
        public string name, description;
        public event Action<LevelItem>? OnClick;
        public bool ShowThumbDivider => ShowThumbsTransitionState(1f) > 0.5f;

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

        public float ShowThumbsTransitionState(float timeStacker) => (owner as PlaylistSelector)?.ShowThumbsTransitionState(timeStacker) ?? 1f;

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
            else
                thumbChangeFade = Custom.LerpAndTick(thumbChangeFade, 0f, 0.08f, 1f / 30f);

            float num3 = Custom.SCurve(ShowThumbsTransitionState(1f) * Mathf.InverseLerp(0f, 0.8f, Alpha), 0.5f);

            roundedRect.size = new Vector2(size.x, size.y * (0.3f + 0.7f * Mathf.Pow(num3, 0.5f)));
            roundedRect.pos = new Vector2(0.01f, -0.49f + size.y * 0.125f * Mathf.Pow(1f - num3, 1.5f));
            roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);
            roundedRect.addSize = new Vector2(10f, 6f) * 0.5f * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * (float)Math.PI)) * (buttonBehav.clicked ? 0f : 1f); roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);

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

        public override void Clicked() => OnClick?.Invoke(this);

        public float Alpha { get; set; }
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }

        public string Description => description;
    }

    public class PlaylistSelector : VerticalScrollSelector
    {
        public FContainer dividerContainer;
        public bool showThumbs = true;
        public float showThumbsTransitionState, lastShowThumbsTransitionState;

        public PlaylistSelector(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, new Vector2(120f, 80f), 5)
        {
            dividerContainer = new FContainer();

            scrollUpButton!.pos.y += 10f;
            scrollDownButton!.pos.y -= 10f;

            AddSideButton("Menu_Symbol_Show_Thumbs", description: "Showing level thumbnails").OnClick += btn =>
            {
                showThumbs = !showThumbs;
                btn.description = showThumbs ? "Showing level thumbnails" : "Showing level names";
                btn.UpdateSymbol(showThumbs ? "Menu_Symbol_Show_Thumbs" : "Menu_Symbol_Show_List");
                menu.PlaySound(showThumbs ? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
            };
        }

        public float ShowThumbsTransitionState(float timeStacker) => Custom.SCurve(Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lastShowThumbsTransitionState, showThumbsTransitionState, timeStacker)), 0.7f), 0.3f);

        public override void Update()
        {
            base.Update();

            lastShowThumbsTransitionState = showThumbsTransitionState;
            showThumbsTransitionState = Custom.LerpAndTick(showThumbsTransitionState, showThumbs ? 1f : 0f, 0.015f, 1f / 30f);

            if (showThumbsTransitionState > 0f && showThumbsTransitionState < 1f)
                ConstrainScroll();

            elementHeight = Mathf.Lerp(20f, 30f + ThumbHeight, ShowThumbsTransitionState(1f));
            elementSpacing = (elementHeight - 20) / 6;
        }
    }

    public VerticalScrollSelector allLevelsPlaylist, selectedLevelsPlaylist;
    public List<LevelUnlockID> unlockBatchIds = [];
    public List<string> allLevels = [], thumbsToBeLoaded = [], loadedThumbTextures = [];
    public bool showThumbs = true, shuffle;
    public int thumbLoadDelay;
    public static int ThumbWidth => 100;
    public static int ThumbHeight => 50;

    public ArenaLevelSelector(Menu.Menu menu, MenuObject owner, Vector2 pos, bool shuffleSetup)
        : base(menu, owner, pos)
    {
        shuffle = shuffleSetup;

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

        allLevelsPlaylist = new PlaylistSelector(menu, this, default);
        selectedLevelsPlaylist = new PlaylistSelector(menu, this, new Vector2(200, 0));

        VerticalScrollSelector.SideButton clearButton = selectedLevelsPlaylist.AddSideButton("Menu_Symbol_Clear_All", "Clear playlist", "Clear playlist");
        clearButton.OnClick += _ =>
        {
            LevelItem[] levelItems = [.. selectedLevelsPlaylist.scrollElements.Cast<LevelItem>()];
            selectedLevelsPlaylist.RemoveScrollElements(levelItems);
            this.ClearMenuObjectIList(levelItems);
        };
        clearButton.OnUpdate += btn => btn.buttonBehav.greyedOut = selectedLevelsPlaylist.scrollElements.Count == 0;

        selectedLevelsPlaylist.AddSideButton(shuffle ? "Menu_Symbol_Shuffle" : "Menu_Symbol_Dont_Shuffle", shuffle ? "Shuffling Levels" : "Playing in order", shuffle ? "Playing levels in random order" : "Playing levels in selected order").OnClick += btn =>
        {
            shuffle = !shuffle;
            btn.label.text = shuffle ? "Shuffling Levels" : "Playing in order";
            btn.description = shuffle ? "Playing levels in random order" : "Playing levels in selected order";
            btn.UpdateSymbol(shuffle ? "Menu_Symbol_Shuffle" : "Menu_Symbol_Dont_Shuffle");
            menu.PlaySound(shuffle ? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
        };

        LevelItem[] levelItems = new LevelItem[allLevels.Count];

        void AddItemToSelectedList(LevelItem levelItem)
        {
            void RemoveItemFromSelectedList(LevelItem levelItem)
            {
                selectedLevelsPlaylist.RemoveScrollElements(levelItem);
                this.ClearMenuObject(levelItem);
            }
            LevelItem item = new(menu, selectedLevelsPlaylist, levelItem.name, "Remove level from playlist");
            item.OnClick += RemoveItemFromSelectedList;
            selectedLevelsPlaylist.AddScrollElements(item);
        }

        for (int i = 0; i < allLevels.Count; i++)
        {
            levelItems[i] = new LevelItem(menu, allLevelsPlaylist, allLevels[i], "Add level to playlist");
            levelItems[i].OnClick += AddItemToSelectedList;
        }

        allLevelsPlaylist.AddScrollElements(levelItems);

        for (int i = 0; i < allLevelsPlaylist.scrollElements.Count - 1; i++)
        {
            if (allLevelsPlaylist.scrollElements[i] is not LevelItem levelItem || allLevelsPlaylist.scrollElements[i + 1] is not LevelItem nextLevelItem) continue;
            if (LevelListSortNumber(levelItem.name) != LevelListSortNumber(nextLevelItem.name))
                levelItem.AddDividers(nextLevelItem);
        }

        this.SafeAddSubobjects(allLevelsPlaylist, selectedLevelsPlaylist);
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
    public bool IsThumbnailLoaded(string levelName) => loadedThumbTextures.Contains(levelName);
    public override void Update()
    {
        base.Update();
        LoadThumbSprite();
    }
}