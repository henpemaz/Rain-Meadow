using System;
using System.Collections.Generic;
using Menu;
using MoreSlugcats;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Pages;

public class ArenaSlugcatSelectPage : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    public SimplerButton backButton;
    public MenuLabel slugcatNameLabel, descriptionLabel, readyWarningLabel;
    public EventfulSelectOneButton[] slugcatSelectButtons;
    public FSprite[] descriptionGradients;
    public Vector2[] descriptionGradientsPos;
    public bool readyWarning;
    public int selectedSlugcatIndex = 0, painCatIndex, warningCounter = -1;
    public string painCatName, painCatDescription;
    public string defaultReadyWarningText = "You have been unreadied. Switch back to re-ready yourself automatically";
    public string countdownText;
    public ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
    public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;

    public ArenaSlugcatSelectPage(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName, int painCatIndex) : base(menu, owner, pos)
    {
        this.painCatName = painCatName;
        this.painCatIndex = painCatIndex;

        backButton = new SimplerButton(menu, this, menu.Translate("Back To Lobby"), new Vector2(200f, 50f), new Vector2(110f, 30f), menu.Translate("Go back to main lobby"));
        backButton.OnClick += _ => ArenaMenu?.MovePage(new Vector2(1500f, 0f), 0);

        slugcatSelectButtons = new EventfulSelectOneButton[ArenaHelpers.selectableSlugcats.Count];

        int buttonsInTopRow = (int)Mathf.Floor(ArenaHelpers.selectableSlugcats.Count / 2f);
        int buttonsInBottomRow = ArenaHelpers.selectableSlugcats.Count - buttonsInTopRow;
        float topRowStartingXPos = 633f - (buttonsInTopRow / 2 * 110f - ((buttonsInTopRow % 2 == 0) ? 55f : 0f));
        float bottomRowStartingXPos = 633f - (buttonsInBottomRow / 2 * 110f - ((buttonsInBottomRow % 2 == 0) ? 55f : 0f));
        for (int i = 0; i < ArenaHelpers.selectableSlugcats.Count; i++)
        {
            Vector2 buttonPos = i < buttonsInTopRow ? new Vector2(topRowStartingXPos + 110f * i, 450f) : new Vector2(bottomRowStartingXPos + 110f * (i - buttonsInTopRow), 340f);
            EventfulSelectOneButton btn = new(menu, this, "", "scug select", buttonPos, new Vector2(100f, 100f), slugcatSelectButtons, i);
            SlugcatStats.Name slugcat = ArenaHelpers.selectableSlugcats[i];
            string portraitFileString = ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel ? SlugcatColorableButton.GetFileForSlugcatIndex(slugcat, painCatIndex, randomizeSofSlugcatPortrait: false) : SlugcatColorableButton.GetFileForSlugcat(slugcat, false);
            MenuIllustration portrait = new(menu, btn, "", portraitFileString, btn.size / 2, true, true);
            btn.subObjects.Add(portrait);
            if (i >= buttonsInTopRow)
                btn.TryBind(backButton, right: i + 1 == buttonsInBottomRow, bottom: true);
            subObjects.Add(btn);
            slugcatSelectButtons[i] = btn;
        }

        painCatDescription = ModManager.MSC ? GetPainCatDescription() : "";

        MenuLabel chooseYourSlugcatLabel = new(menu, this, menu.Translate("CHOOSE YOUR SLUGCAT"), new Vector2(680f, 575f), default, true);
        chooseYourSlugcatLabel.label.color = new Color(0.5f, 0.5f, 0.5f);
        chooseYourSlugcatLabel.label.shader = menu.manager.rainWorld.Shaders["MenuTextCustom"];

        readyWarningLabel = new MenuLabel(menu, this, menu.LongTranslate(defaultReadyWarningText), new Vector2(680f, 620f), Vector2.zero, true);

        slugcatNameLabel = new MenuLabel(menu, this, "", new Vector2(680f, 310f), default, true);
        slugcatNameLabel.label.shader = menu.manager.rainWorld.Shaders["MenuText"];
        descriptionLabel = new MenuLabel(menu, this, "", new Vector2(680f, 210f), default, true);
        descriptionLabel.label.color = new Color(0.8f, 0.8f, 0.8f);

        descriptionGradients = new FSprite[4];
        descriptionGradientsPos = new Vector2[4];
        countdownText = $"The match is starting in {Arena.lobbyCountDown}! Ready up!!";

        for (int i = 0; i < descriptionGradients.Length; i++)
        {
            descriptionGradients[i] = new FSprite("LinearGradient200")
            {
                rotation = i % 2 == 0 ? 270f : 90f,
                scaleY = 2f,
                anchorX = 0.6f,
                anchorY = 0f,
            };
            descriptionGradientsPos[i] = new Vector2(680f, i > 1 ? 280f : 125f);
            Container.AddChild(descriptionGradients[i]);
        }

        this.SafeAddSubobjects(backButton, chooseYourSlugcatLabel, readyWarningLabel, slugcatNameLabel, descriptionLabel);
        if (ArenaMenu != null)
        {
            SlugcatStats.Name? savedSlugcat = ArenaMenu.GetArenaSetup.playerClass[0];
            RainMeadow.Debug($"Saved Slugcat: {savedSlugcat?.value ?? "NULL"}");
            SwitchSelectedSlugcat(savedSlugcat);
            ArenaMenu.ChangeScene();
        }

    }

    public void SwitchSelectedSlugcat(SlugcatStats.Name? slugcat)
    {
        SlugcatStats.Name nonNullSlugcat = slugcat ?? SlugcatStats.Name.White;
        selectedSlugcatIndex = Mathf.Max(ArenaHelpers.selectableSlugcats.IndexOf(nonNullSlugcat), 0);
        slugcat = ArenaHelpers.selectableSlugcats[selectedSlugcatIndex];
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
            descriptionLabel.text = menu.LongTranslate(Arena.slugcatSelectDescriptions[Arena.sainot ? "Sainot" : "Saint"]);

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
    public override void Update()
    {
        base.Update();
        if (warningCounter >= 0) warningCounter++;
        if (readyWarning)
            warningCounter = Mathf.Max(warningCounter, 0);
        else warningCounter = -1;
        if (readyWarningLabel != null)
        {
            readyWarningLabel.text = Arena.initiateLobbyCountdown && Arena.lobbyCountDown > 0 ? menu.LongTranslate($"The match is starting in {Arena.lobbyCountDown}! Ready up!!") : menu.LongTranslate(defaultReadyWarningText);
        }

        if (ArenaHelpers.selectableSlugcats[selectedSlugcatIndex] == MoreSlugcatsEnums.SlugcatStatsName.Saint)
            descriptionLabel.text = menu.LongTranslate(Arena.slugcatSelectDescriptions[Arena.sainot ? "Sainot" : "Saint"]);
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
        if (selectedSlugcatIndex == to) return;
        SwitchSelectedSlugcat(ArenaHelpers.selectableSlugcats[to]);
    }
}
