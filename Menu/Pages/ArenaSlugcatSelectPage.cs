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
    public MenuLabel slugcatNameLabel, descriptionLabel;
    public EventfulSelectOneButton[] slugcatSelectButtons;
    public FSprite[] descriptionGradients;
    public Vector2[] descriptionGradientsPos;
    public int selectedSlugcatIndex = 0;
    public string painCatName;
    public string? painCatDescription, painCatPortraitFileString;
    public ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
    public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;
    private Dictionary<string, int> count = [];

    public ArenaSlugcatSelectPage(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName) : base(menu, owner, pos)
    {
        this.painCatName = painCatName;

        backButton = new SimplerButton(menu, this, "Back To Lobby", new Vector2(200f, 50f), new Vector2(110f, 30f), "Go back to main lobby");
        backButton.OnClick += _ => ArenaMenu?.MovePage(new Vector2(1500f, 0f), 0);

        slugcatSelectButtons = new EventfulSelectOneButton[ArenaHelpers.selectableSlugcats.Count];

        int buttonsInTopRow = (int)Mathf.Floor(ArenaHelpers.selectableSlugcats.Count / 2f);
        int buttonsInBottomRow = ArenaHelpers.selectableSlugcats.Count - buttonsInTopRow;
        float topRowStartingXPos = 633f - (buttonsInTopRow / 2 * 110f - ((buttonsInTopRow % 2 == 0) ? 55f : 0f));
        float bottomRowStartingXPos = 633f - (buttonsInBottomRow / 2 * 110f - ((buttonsInBottomRow % 2 == 0) ? 55f : 0f));

        MenuIllustration portrait2 = null;
        EventfulSelectOneButton painCatButton = null;
        for (int i = 0; i < ArenaHelpers.selectableSlugcats.Count; i++)
        {
            int index = i;

            Vector2 buttonPos = i < buttonsInTopRow ? new Vector2(topRowStartingXPos + 110f * i, 450f) : new Vector2(bottomRowStartingXPos + 110f * (i - buttonsInTopRow), 340f);
            EventfulSelectOneButton btn = new(menu, this, "", "scug select", buttonPos, new Vector2(100f, 100f), slugcatSelectButtons, i);

            string portraitFileString = SlugcatColorableButton.GetFileForSlugcat(ArenaHelpers.selectableSlugcats[i], false);
            MenuIllustration portrait = new(menu, btn, "", portraitFileString, btn.size / 2, true, true);
            portrait2 = portrait;
            btn.subObjects.Add(portrait);

            if (ArenaHelpers.selectableSlugcats[i] == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                painCatButton = btn;
                painCatPortraitFileString = portraitFileString;

                if (ArenaMenu is not null)
                    painCatDescription = GetPainCatDescription();
            }

            subObjects.Add(btn);
            slugcatSelectButtons[i] = btn;
        }

        SimplerButton randomizePainCat = new(menu, this, $"Randomize {painCatName} select data", new Vector2(1056f, 50f), new Vector2(300f, 30f));
        randomizePainCat.OnClick += _ =>
            {
                bool seenIt = true;
                int timer = 0;
                while (seenIt && timer < 10000)
                {
                    this.ClearMenuObject(portrait2);
                    painCatPortraitFileString = SlugcatColorableButton.GetFileForSlugcat(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, false);
                    subObjects.Add(portrait2 = new MenuIllustration(menu, painCatButton, "", painCatPortraitFileString, painCatButton.size / 2, true, true));
                    this.painCatName = Arena.slugcatSelectPainCatNames[UnityEngine.Random.Range(0, Arena.slugcatSelectPainCatNames.Count)];
                    painCatDescription = GetPainCatDescription();
                    seenIt = count.ContainsKey(painCatDescription);
                    if (seenIt) count[painCatDescription]++;
                    else count[painCatDescription] = 1;

                    timer++;
                }
                if (timer >= 10000) RainMeadow.Debug("Broke loop after 10000 cycles");

                SwitchSelectedSlugcat(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
            };

        MenuLabel chooseYourSlugcatLabel = new(menu, this, menu.Translate("CHOOSE YOUR SLUGCAT"), new Vector2(680f, 575f), default, true);
        chooseYourSlugcatLabel.label.color = new Color(0.5f, 0.5f, 0.5f);
        chooseYourSlugcatLabel.label.shader = menu.manager.rainWorld.Shaders["MenuTextCustom"];
        slugcatNameLabel = new MenuLabel(menu, this, "", new Vector2(680f, 310f), default, true);
        slugcatNameLabel.label.shader = menu.manager.rainWorld.Shaders["MenuText"];
        descriptionLabel = new MenuLabel(menu, this, "", new Vector2(680f, 210f), default, true);
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
            descriptionGradientsPos[i] = new Vector2(680f, i > 1 ? 280f : 125f);
            Container.AddChild(descriptionGradients[i]);
        }

        this.SafeAddSubobjects(backButton, chooseYourSlugcatLabel, slugcatNameLabel, descriptionLabel, randomizePainCat);
        if (ArenaMenu != null)
        {
            SlugcatStats.Name? savedSlugcat = ArenaMenu.GetArenaSetup.playerClass[0];
            RainMeadow.Debug($"Saved Slugcat: {savedSlugcat?.value ?? "NULL"}");
            SwitchSelectedSlugcat(savedSlugcat);
            ArenaMenu.ChangeScene(ArenaMenu.slugcatScene);
        }
    }

    public void SwitchSelectedSlugcat(SlugcatStats.Name? slugcat)
    {
        selectedSlugcatIndex = ArenaHelpers.selectableSlugcats.IndexOf(slugcat);
        try
        {
            slugcat = ArenaHelpers.selectableSlugcats[selectedSlugcatIndex];
        }
        catch (System.IndexOutOfRangeException)
        {
            selectedSlugcatIndex = 0;
            slugcat = ArenaHelpers.selectableSlugcats[0];
        }

        ArenaMenu?.SwitchSelectedSlugcat(slugcat);

        if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
        {
            descriptionLabel.text = Custom.ReplaceLineDelimeters(menu.Translate(painCatDescription));
            slugcatNameLabel.text = menu.Translate(painCatName.ToUpper());
            return;
        }
        descriptionLabel.text = Custom.ReplaceLineDelimeters(menu.Translate(Arena.slugcatSelectDescriptions.TryGetValue(slugcat.value, out string desc)? desc : Arena.slugcatSelectDescriptions[SlugcatStats.Name.White.value]));
        slugcatNameLabel.text = menu.Translate(Arena.slugcatSelectDisplayNames.TryGetValue(slugcat.value, out string name) ? name : $"THE {SlugcatStats.getSlugcatName(slugcat).ToUpper()}");
        if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer && UnityEngine.Random.Range(0, 1000) == 0)
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

        // should in theory never fail but I don't trust that
        try
        {
            List<string>? invPortraitDescriptions = null;
            char? painCatPortraitIdentifier = painCatPortraitFileString?[19];
            if (painCatPortraitIdentifier is not null)
            {
                if (painCatPortraitIdentifier == '0') invPortraitDescriptions = Arena.slugcatSelectPainCatSmileyDescriptions;
                else if (painCatPortraitIdentifier == '1') invPortraitDescriptions = Arena.slugcatSelectPainCatUwUDescriptions;
                else if (painCatPortraitIdentifier == '2') invPortraitDescriptions = Arena.slugcatSelectPainCatWaveDescriptions;
                else if (painCatPortraitIdentifier == '3') invPortraitDescriptions = Arena.slugcatSelectPainCatDeadDescriptions;

                if (invPortraitDescriptions is not null) descriptionCategories.Add(invPortraitDescriptions, 0.3f);
            }
        }
        catch (Exception e)
        {
            RainMeadow.Error($"Unable to properly access identifier character from PainCat portrait string. Perhaps the file format changed?\n{e}");
        }

        List<string> descriptions = descriptionCategories.GetRandom();
        return Custom.ReplaceLineDelimeters(descriptions[UnityEngine.Random.Range(0, descriptions.Count)]).Replace("<USERNAME>", OnlineManager.mePlayer.id.name);
    }

    public override void Update()
    {
        base.Update();

        if (ArenaMenu is not null)
            for (int i = 0; i < slugcatSelectButtons.Length; i++)
                slugcatSelectButtons[i].buttonBehav.greyedOut = ArenaMenu.pendingBgChange;
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
    }

    public int GetCurrentlySelectedOfSeries(string series) => selectedSlugcatIndex; // no need to check series (for now) since there is only one SelectOneButton in this menu

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        if (selectedSlugcatIndex == to) return;
        SwitchSelectedSlugcat(ArenaHelpers.selectableSlugcats[to]);
    }
}