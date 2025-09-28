using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using MoreSlugcats;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;
using static RainMeadow.UI.Components.TabContainer;

namespace RainMeadow.UI.Pages;

public class ArenaSlugcatSelectPage : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    public SimplerButton backButton;
    public MenuLabel slugcatNameLabel, descriptionLabel, readyWarningLabel;
    public EventfulSelectOneButton[] slugcatSelectButtons;
    public MenuIllustration[] slugcatIllustrations;
    public List<SlugcatStats.Name[]> slugcatSelectNamePages;
    public SimplerSymbolButton prevButton;
    public SimplerSymbolButton nextButton;
    public FSprite[] descriptionGradients;
    public Vector2[] descriptionGradientsPos;
    public bool readyWarning, lastBanSlugInput, banSlugInput, lastSainot;
    public int selectedSlugcatIndex = 0, painCatIndex, warningCounter = -1, currentSlugcatSelectPage = 0;
    public string painCatName, painCatDescription;
    public string defaultReadyWarningText = "You have been unreadied. Switch back to re-ready yourself automatically";

    public int maxScugsPerRow = 6;
    public ArenaOnlineGameMode? Arena => OnlineManager.lobby?.gameMode as ArenaOnlineGameMode;
    public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;

    public ArenaSlugcatSelectPage(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName, int painCatIndex) : base(menu, owner, pos)
    {
        this.painCatName = painCatName;
        this.painCatIndex = painCatIndex;

        backButton = new SimplerButton(menu, this, menu.Translate("Back To Lobby"), new Vector2(200f, 50f), new Vector2(110f, 30f), menu.Translate("Go back to main lobby"));
        backButton.OnClick += _ => ArenaMenu?.MovePage(new Vector2(1500f, 0f), 0);
        backButton.OnClick += _ => ArenaMenu.selectedObject = ArenaMenu.arenaMainLobbyPage.readyButton; //Ideally this'd be the portrait button that you came from, but actually navigating there is a super evil hardcode.

        CreateArrowButtons();

        slugcatSelectButtons = new EventfulSelectOneButton[ArenaHelpers.selectableSlugcats.Count];
        slugcatIllustrations = new MenuIllustration[ArenaHelpers.selectableSlugcats.Count];
        slugcatSelectNamePages = new List<SlugcatStats.Name[]>();
        for (int i=0; i<Mathf.Ceil(ArenaHelpers.selectableSlugcats.Count / (2*maxScugsPerRow))+1; i++)
        {
            slugcatSelectNamePages.Add(new SlugcatStats.Name[Math.Min(2*maxScugsPerRow, ArenaHelpers.selectableSlugcats.Count - (2*maxScugsPerRow*i))]);
            RainMeadow.Debug("Page " + i + " should hold " + slugcatSelectNamePages[i].Length + " scugs:");
            for (int j=0; j<slugcatSelectNamePages[i].Length; j++)
            {
                slugcatSelectNamePages[i][j] = ArenaHelpers.selectableSlugcats[(i*(2*maxScugsPerRow))+j];
                RainMeadow.Debug("    " + j + ": " + slugcatSelectNamePages[i][j]);
            }
        }
        SwitchSlugcatTabBy(0);

        painCatDescription = ModManager.MSC ? GetPainCatDescription() : "";

        MenuLabel chooseYourSlugcatLabel = new(menu, this, menu.Translate("CHOOSE YOUR SLUGCAT"), new Vector2(680f, 575f), default, true);
        chooseYourSlugcatLabel.label.color = new Color(0.5f, 0.5f, 0.5f);
        chooseYourSlugcatLabel.label.shader = menu.manager.rainWorld.Shaders["MenuTextCustom"];

        readyWarningLabel = new MenuLabel(menu, this, menu.LongTranslate(defaultReadyWarningText), new Vector2(680f, 620f), Vector2.zero, true);

        slugcatNameLabel = new MenuLabel(menu, this, "", new Vector2(680f, OnlineManager.lobby.isOwner ? 280f : 310), default, true);
        slugcatNameLabel.label.shader = menu.manager.rainWorld.Shaders["MenuText"];
        descriptionLabel = new MenuLabel(menu, this, "", new Vector2(680f, OnlineManager.lobby.isOwner ? 180 : 210f), default, true);
        descriptionLabel.label.color = new Color(0.8f, 0.8f, 0.8f);

        descriptionGradients = new FSprite[4];
        descriptionGradientsPos = new Vector2[4];

        for (int i = 0; i < descriptionGradients.Length; i++)
        {
            descriptionGradients[i] = new FSprite("LinearGradient200")
            {
                rotation = i % 2 == 0 ? 270f : 90f,
                scaleY = 2f,
                anchorX = 0.6f,
                anchorY = 0f,
            };
            descriptionGradientsPos[i] = new Vector2(680f, i > 1 ? (OnlineManager.lobby.isOwner ? 240f : 280f) : 125f);
            Container.AddChild(descriptionGradients[i]);
        }

        this.SafeAddSubobjects(backButton, prevButton, nextButton, chooseYourSlugcatLabel, readyWarningLabel, slugcatNameLabel, descriptionLabel);
        if (ArenaMenu != null)
        {
            SlugcatStats.Name? savedSlugcat = ArenaMenu.GetArenaSetup.playerClass[0];
            RainMeadow.Debug($"Saved Slugcat: {savedSlugcat?.value ?? "NULL"}");
            SwitchSelectedSlugcat(savedSlugcat);
            ArenaMenu.ChangeScene();
        }

    }

    public void SwitchSlugcatTabBy(int increasePageBy)
    {
        foreach (MenuObject oldButton in slugcatSelectButtons)
            this.ClearMenuObject(oldButton);

        currentSlugcatSelectPage = Extensions.RealModulo((currentSlugcatSelectPage + increasePageBy), slugcatSelectNamePages.Count);
        slugcatSelectButtons = new EventfulSelectOneButton[slugcatSelectNamePages[currentSlugcatSelectPage].Length];
        slugcatIllustrations = new MenuIllustration[slugcatSelectNamePages[currentSlugcatSelectPage].Length];

        int currentButtonsInTopRow = (int)Mathf.Ceil(slugcatSelectNamePages[currentSlugcatSelectPage].Length / 2f);
        int currentButtonsInBottomRow = slugcatSelectNamePages[currentSlugcatSelectPage].Length - currentButtonsInTopRow;
        float currentTopRowStartingXPos = 633f - (currentButtonsInTopRow / 2 * 110f - ((currentButtonsInTopRow % 2 == 0) ? 55f : 0f));
        float currentBottomRowStartingXPos = 633f - (currentButtonsInBottomRow / 2 * 110f - ((currentButtonsInBottomRow % 2 == 0) ? 55f : 0f));
        float currentSingleRowStartingXPos = 688f - (slugcatSelectNamePages[currentSlugcatSelectPage].Length * 55f);
        for (int i = 0; i < slugcatSelectNamePages[currentSlugcatSelectPage].Length; i++)
        {
            Vector2 buttonPos;
            if (slugcatSelectNamePages[currentSlugcatSelectPage].Length <= maxScugsPerRow)
                buttonPos = new Vector2(currentSingleRowStartingXPos + 110f * i, 395f);
            else
                buttonPos = i < currentButtonsInTopRow ? new Vector2(currentTopRowStartingXPos + 110f * i, 450f) : new Vector2(currentBottomRowStartingXPos + 110f * (i - currentButtonsInTopRow), 340f);

            EventfulSelectOneButton btn = new(menu, this, "", "scug select", buttonPos, new Vector2(100f, 100f), slugcatSelectButtons, i + (currentSlugcatSelectPage*2*maxScugsPerRow));
            SlugcatStats.Name slugcat = slugcatSelectNamePages[currentSlugcatSelectPage][i];
            string portraitFileString = ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel ? SlugcatColorableButton.GetFileForSlugcatIndex(slugcat, painCatIndex, randomizeSofSlugcatPortrait: false) : SlugcatColorableButton.GetFileForSlugcat(slugcat, false);
            slugcatIllustrations[i] = new(menu, btn, "", portraitFileString, btn.size / 2, false, true);
            btn.subObjects.Add(slugcatIllustrations[i]);
            slugcatSelectButtons[i] = btn;
            subObjects.Add(btn);
        }

        if (slugcatSelectNamePages[currentSlugcatSelectPage].Length <= maxScugsPerRow)
        {
            //Enforce row order
            Extensions.TrySequentialMutualBind(menu,
                new List<MenuObject>() { prevButton }.Concat(
                slugcatSelectButtons.Concat(
                new List<MenuObject>() { nextButton })).ToList(), leftRight: true, loopLastIndex: true);
            //Chain up/down edges to the back button
            Extensions.TryMassBind(slugcatSelectButtons.Cast<MenuObject>().ToList(), backButton, top: true, bottom: true);
        }
        else
        {
            //Group up elements
            int midpoint = (slugcatSelectButtons.Length + 1) / 2;
            List<MenuObject> TopRowElements = slugcatSelectButtons.Take(midpoint).Cast<MenuObject>().ToList();
            List<MenuObject> BottomRowElements = slugcatSelectButtons.Skip(midpoint).Cast<MenuObject>().ToList();
            //Enforce row order
            Extensions.TrySequentialMutualBind(menu,
                new List<MenuObject>() { prevButton }.Concat(
                BottomRowElements.Concat( //Bottom row first so the top row takes priority when moving from the side buttons.
                new List<MenuObject>() { nextButton })).ToList(), leftRight: true, loopLastIndex: true);
            Extensions.TrySequentialMutualBind(menu,
                new List<MenuObject>() { prevButton }.Concat(
                TopRowElements.Concat(
                new List<MenuObject>() { nextButton })).ToList(), leftRight: true, loopLastIndex: true);
            //Link the two rows to each other (I should maybe make this an extension but eh)
            TopRowElements.Reverse(); //Goal is to have switching between unequal rows to be / instead of \, so we want the odd one out to be on the left.
            BottomRowElements.Reverse(); //To switch it back to \ if wanted, remove these two reverses and change the eventual .Last()s to .First()s.
            for (int i=0; i<BottomRowElements.Count; i++)
            {
                Extensions.TryMutualBind(menu, BottomRowElements[i], TopRowElements[i], bottomTop: true);
            }
            Extensions.TryBind(TopRowElements.Last(), BottomRowElements.Last(), bottom: true); //If counts are equal this does nothing new, if counts are unequal it fixes the top left entry.
            //Chain up/down edges to the back button
            Extensions.TryMassBind(TopRowElements, backButton, top: true);
            Extensions.TryMassBind(BottomRowElements, backButton, bottom: true);
        }
    }

    public void CreateArrowButtons()
    {
        float prevNextButtonsPadding = 20f;
        int maxButtonsInRow;
        float maxRowStartingXPos;
        if (ArenaHelpers.selectableSlugcats.Count <= maxScugsPerRow)
        {
            maxButtonsInRow = ArenaHelpers.selectableSlugcats.Count;
            maxRowStartingXPos = 688f - (maxButtonsInRow * 55f);
        }
        else
        {
            maxButtonsInRow = (int)Mathf.Ceil(Math.Min(maxScugsPerRow, ArenaHelpers.selectableSlugcats.Count / 2f));
            maxRowStartingXPos = 633f - (maxButtonsInRow / 2 * 110f - ((maxButtonsInRow % 2 == 0) ? 55f : 0f));
        }
        prevButton = new SimplerSymbolButton(menu, this, "Menu_Symbol_Arrow", "PREVSINGAL", new(maxRowStartingXPos - prevNextButtonsPadding - 10f - 24f, 433f));
        nextButton = new SimplerSymbolButton(menu, this, "Menu_Symbol_Arrow", "NEXTSINGAL", new(maxRowStartingXPos + prevNextButtonsPadding + (110f * maxButtonsInRow), 433f));
        prevButton.symbolSprite.rotation = 270;
        nextButton.symbolSprite.rotation = 90;
        prevButton.OnClick += _ => SwitchSlugcatTabBy(-1);
        nextButton.OnClick += _ => SwitchSlugcatTabBy(1);
        prevButton.OnClick += _ => menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        nextButton.OnClick += _ => menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        if (ArenaHelpers.selectableSlugcats.Count <= 2 * maxScugsPerRow)
        {
            prevButton.buttonBehav.greyedOut = true;
            nextButton.buttonBehav.greyedOut = true;
        }

        List<MenuObject> ArrowButtons = new List<MenuObject> { prevButton, nextButton };
        Extensions.TrySequentialMutualBind(menu, ArrowButtons, leftRight: true, loopLastIndex: true);
        Extensions.TryMassBind(ArrowButtons, backButton, top: true, bottom: true);
        Extensions.TryBind(backButton, prevButton, left: true, top: true, bottom: true);
        Extensions.TryBind(backButton, nextButton, right: true);
    }

    public void SwitchSelectedSlugcat(SlugcatStats.Name? slugcat)
    {
        SlugcatStats.Name nonNullSlugcat = slugcat ?? SlugcatStats.Name.White;
        selectedSlugcatIndex = Mathf.Max(ArenaHelpers.selectableSlugcats.IndexOf(nonNullSlugcat), 0);
        nonNullSlugcat = ArenaHelpers.selectableSlugcats[selectedSlugcatIndex];
        ArenaMenu?.SwitchSelectedSlugcat(nonNullSlugcat);
        if (nonNullSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
        {
            descriptionLabel.text = menu.LongTranslate(painCatDescription).Replace("<USERNAME>", OnlineManager.mePlayer.id.name);
            slugcatNameLabel.text = menu.Translate(painCatName.ToUpper());
            return;
        }

        descriptionLabel.text = menu.LongTranslate(Arena.slugcatSelectDescriptions.TryGetValue(nonNullSlugcat.value, out string desc) ? desc : Arena.slugcatSelectDescriptions[SlugcatStats.Name.White.value]);
        slugcatNameLabel.text = menu.Translate(Arena.slugcatSelectDisplayNames.TryGetValue(nonNullSlugcat.value, out string name) ? name : $"THE {SlugcatStats.getSlugcatName(slugcat).ToUpper()}");

        if (nonNullSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Saint)
        {
            descriptionLabel.text = menu.LongTranslate(Arena.slugcatSelectDescriptions[Arena.sainot ? "Sainot" : "Saint"]);
            if (UnityEngine.Random.Range(0, 1000) == 0) descriptionLabel.text = menu.Translate("you could have saved them");
        }

        if (nonNullSlugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer && UnityEngine.Random.Range(0, 1000) == 0)
        {
            menu.PlaySound(RainMeadow.Ext_SoundID.Fartificer);
            slugcatNameLabel.text = menu.Translate("THE FARTIFICER");
        }
    }

    public string GetPainCatDescription()
    {

        WeightedList<List<string>> descriptionCategories = new();
        descriptionCategories.Add(Arena.slugcatSelectPainCatNormalDescriptions, 0.4f);
        descriptionCategories.Add(Arena.slugcatSelectPainCatJokeDescriptions, 0.1f);
        descriptionCategories.Add(Arena.slugcatSelectPainCatQuoteDescriptions, 0.2f);
        descriptionCategories.Add(Arena.slugcatSelectPainCatDevJokeDescriptions, 0.01f);

        if (painCatName == "Inv") descriptionCategories.Add(["inv? like invalidunits?"], 0.01f);

        List<string>? invPortraitDescriptions = null;

        if (painCatIndex == 0) invPortraitDescriptions = Arena.slugcatSelectPainCatSmileyDescriptions;
        else if (painCatIndex == 1) invPortraitDescriptions = Arena.slugcatSelectPainCatUwUDescriptions;
        else if (painCatIndex == 2) invPortraitDescriptions = Arena.slugcatSelectPainCatWaveDescriptions;
        else if (painCatIndex == 3) invPortraitDescriptions = Arena.slugcatSelectPainCatDeadDescriptions;

        if (invPortraitDescriptions is not null) descriptionCategories.Add(invPortraitDescriptions, 0.3f);

        List<string> descriptions = descriptionCategories.GetRandom();
        return descriptions[UnityEngine.Random.Range(0, descriptions.Count)];
    }
    public void OnMakingNewAvailableSlugcats()
    {
        SlugcatStats.Name[] newAvaliableSlugs = Arena!.AvailableSlugcats();
        if (newAvaliableSlugs.Length == 1 && newAvaliableSlugs.Contains(RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat))
            ArenaMenu?.SetTemporaryDescription(menu.Translate("Players will rotate through all slugcats"), 200);
        else ArenaMenu?.SetTemporaryDescription("", 1); //remove desc above
    }
    public void OnSlugcatPressedBan(int index)
    {
        SlugcatStats.Name[] availableSlugs = Arena!.AvailableSlugcats();
        bool oneSlugLeft = availableSlugs.Length - 1 == 0;
        if (!Arena.bannedSlugs.Contains(index) && oneSlugLeft)
        {
            menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            return;
        }
        menu.PlaySound(Arena.AddRemoveBannedSlug(index) ? SoundID.MENU_Checkbox_Uncheck : SoundID.MENU_Checkbox_Check); //true = no wawa
        OnMakingNewAvailableSlugcats();
    }
    public override void Update()
    {
        base.Update();
        lastBanSlugInput = banSlugInput;
        banSlugInput = RWInput.PlayerInput(0).pckp;
        if (warningCounter >= 0) warningCounter++;
        if (readyWarning)
            warningCounter = Mathf.Max(warningCounter, 0);
        else warningCounter = -1;
        if (readyWarningLabel != null)
        {
            readyWarningLabel.text = Arena != null && Arena.initiateLobbyCountdown && Arena.lobbyCountDown > 0 ? menu.LongTranslate($"The match is starting in <COUNTDOWN>! Ready up!!").Replace("<COUNTDOWN>", Arena.lobbyCountDown.ToString()) : menu.LongTranslate(defaultReadyWarningText);
        }
        if (Arena != null && OnlineManager.lobby.isOwner && banSlugInput && !lastBanSlugInput && slugcatSelectButtons.Contains(menu.selectedObject))
            OnSlugcatPressedBan(((EventfulSelectOneButton)menu.selectedObject).buttonArrayIndex);
        for (int i = 0; i < slugcatIllustrations.Length; i++)
        {
            bool banned = Arena?.bannedSlugs?.Contains(i + (currentSlugcatSelectPage * 2 * maxScugsPerRow)) == true;
            MenuIllustration illu = slugcatIllustrations[i];
            SlugcatStats.Name slugcat = slugcatSelectNamePages[currentSlugcatSelectPage][i];
            string file = slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel ? SlugcatColorableButton.GetFileForSlugcatIndex(slugcat, painCatIndex, banned, false) : SlugcatColorableButton.GetFileForSlugcat(slugcat, false, banned);
            illu.fileName = file;
            illu.LoadFile();
            illu.sprite.SetElementByName(illu.fileName);
            //WHY CANT MENUILLUSTRATION HAVE THEIR OWN SETNEWIMAGE.
            //Menuscene has a similar thing happening for gourmand dream scenes, Multiplayer has it, IT COULD HAVE MADE EVERYONE'S LIVES EASIER
            illu.color = banned ? MenuColorEffect.rgbDarkGrey : Color.white;
        }
        if (Arena != null)
        {
            int newSlugIndex = Arena.GetNewAvailableSlugcatIndex(selectedSlugcatIndex);
            if (newSlugIndex != selectedSlugcatIndex)
            {
                SwitchSelectedSlugcat(ArenaHelpers.selectableSlugcats[newSlugIndex]);
                ArenaMenu?.ChangeScene();
            }
            lastSainot = Arena.sainot;

        }
        if (Arena != null && ArenaHelpers.selectableSlugcats[selectedSlugcatIndex] == MoreSlugcatsEnums.SlugcatStatsName.Saint && Arena.sainot != lastSainot)
        {
            descriptionLabel.text = menu.LongTranslate(Arena.slugcatSelectDescriptions[Arena.sainot ? "Sainot" : "Saint"]);
            if (UnityEngine.Random.Range(0, 1000) == 0) descriptionLabel.text = menu.Translate("you could have saved them");
        }
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (owner is not PositionedMenuObject positionedOwner) return;

        for (int i = 0; i < descriptionGradients.Length; i++)
        {
            FSprite gradientSprite = descriptionGradients[i];
            Vector2 gradientSpritePos = descriptionGradientsPos[i];

            gradientSprite.x = positionedOwner.DrawX(timeStacker) + gradientSpritePos.x;
            gradientSprite.y = positionedOwner.DrawY(timeStacker) + gradientSpritePos.y;
        }
        if (readyWarning)
        {
            readyWarningLabel.label.color = Color.Lerp(MenuColorEffect.rgbWhite, new Color(0.85f, 0.35f, 0.4f), 0.5f - 0.5f * Mathf.Sin((timeStacker + warningCounter) / 30 * Mathf.PI * 2));
            readyWarningLabel.label.alpha = 1;
        }
        else readyWarningLabel.label.alpha = 0;
    }

    public int GetCurrentlySelectedOfSeries(string series) => selectedSlugcatIndex; // no need to check series (for now) since there is only one SelectOneButton in this menu

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        if (Arena?.bannedSlugs?.Contains(to) == true)
        {
            menu.PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            return;
        }
        if (selectedSlugcatIndex == to) return;
        SwitchSelectedSlugcat(ArenaHelpers.selectableSlugcats[to]);
    }
}
