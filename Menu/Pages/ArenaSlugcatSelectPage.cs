using System.Collections.Generic;
using System.Text.RegularExpressions;
using Menu;
using MoreSlugcats;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow.UI.Pages;

public class ArenaSlugcatSelectPage : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    public SimplerButton backButton;
    public MenuLabel slugcatNameLabel, descriptionLabel;
    public List<SlugcatStats.Name> allSlugcats = ArenaHelpers.allSlugcats;
    public EventfulSelectOneButton[] slugcatSelectButtons;
    public FSprite[] descriptionGradients;
    public Vector2[] descriptionGradientsPos;
    public int selectedSlugcatIndex = 0;
    public string painCatName;
    public string? painCatDescription;
    public ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
    public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;

    public ArenaSlugcatSelectPage(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName) : base(menu, owner, pos)
    {
        this.painCatName = painCatName;

        backButton = new SimplerButton(menu, this, "Back To Lobby", new Vector2(200f, 50f), new Vector2(110f, 30f), "Go back to main lobby");
        backButton.OnClick += _ => ArenaMenu?.MovePage(new Vector2(1500f, 0f), 0);

        slugcatSelectButtons = new EventfulSelectOneButton[allSlugcats.Count];

        int buttonsInTopRow = (int)Mathf.Floor(allSlugcats.Count / 2f);
        int buttonsInBottomRow = allSlugcats.Count - buttonsInTopRow;
        float topRowStartingXPos = 633f - (buttonsInTopRow / 2 * 110f - ((buttonsInTopRow % 2 == 0) ? 55f : 0f));
        float bottomRowStartingXPos = 633f - (buttonsInBottomRow / 2 * 110f - ((buttonsInBottomRow % 2 == 0) ? 55f : 0f));

        MenuIllustration painCatPortrait = null;
        EventfulSelectOneButton painCatButton = null;
        for (int i = 0; i < allSlugcats.Count; i++)
        {
            int index = i;

            Vector2 buttonPos = i < buttonsInTopRow ? new Vector2(topRowStartingXPos + 110f * i, 450f) : new Vector2(bottomRowStartingXPos + 110f * (i - buttonsInTopRow), 340f);
            EventfulSelectOneButton btn = new(menu, this, "", "scug select", buttonPos, new Vector2(100f, 100f), slugcatSelectButtons, i);

            MenuIllustration portrait = new(menu, btn, "", SlugcatColorableButton.GetFileForSlugcat(allSlugcats[i], false), btn.size / 2, true, true);
            btn.subObjects.Add(portrait);

            if (allSlugcats[i] == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                painCatButton = btn;
                painCatPortrait = portrait;

                if (ArenaMenu is not null)
                    painCatDescription = Arena.slugcatSelectPainCatDescriptions
                    [
                        Random.Range(0, 3) == 1 ? // 33% chance for portrait specific description, 66% for general quotes
                        int.Parse(Regex.Match(painCatPortrait.fileName, @"\d+").Value[0].ToString()) :
                        Random.Range(5, Arena.slugcatSelectPainCatDescriptions.Count)
                    ].Replace("<USERNAME>", OnlineManager.mePlayer.id.name);
            }

            subObjects.Add(btn);
            slugcatSelectButtons[i] = btn;
        }

        SimplerButton randomizePainCat = new(menu, this, $"Randomize {painCatName} select data", new Vector2(1056f, 50f), new Vector2(300f, 30f));
        randomizePainCat.OnClick += _ =>
        {
            painCatPortrait.RemoveSprites();
            RemoveSubObject(painCatPortrait);
            this.SafeAddSubobjects(painCatPortrait = new(menu, painCatButton, "", SlugcatColorableButton.GetFileForSlugcat(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, false), painCatButton.size / 2, true, true));
            bool usePortraitDescription = UnityEngine.Random.Range(0, 3) == 1;
            int portraitIndex = int.Parse(Regex.Match(painCatPortrait.fileName, @"\d+").Value[0].ToString());
            int descriptionIndex = usePortraitDescription ? UnityEngine.Random.Range(5, Arena.slugcatSelectPainCatDescriptions.Count) : portraitIndex;
            RainMeadow.Debug($"description index: {descriptionIndex}; portrait index: {portraitIndex}; random: {usePortraitDescription}; portrait name: {painCatPortrait.fileName}");

            painCatDescription = Arena.slugcatSelectPainCatDescriptions[descriptionIndex].Replace("<USERNAME>", OnlineManager.mePlayer.id.name);
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
        slugcat = allSlugcats.IndexOf(slugcat) == -1 ? allSlugcats[0] : slugcat;
        selectedSlugcatIndex = allSlugcats.IndexOf(slugcat);
        ArenaMenu?.SwitchSelectedSlugcat(slugcat);
        if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
        {
            descriptionLabel.text = painCatDescription;
            slugcatNameLabel.text = painCatName;
            return;
        }
        descriptionLabel.text = menu.Translate(Arena.slugcatSelectDescriptions.TryGetValue(slugcat.value, out string desc)? desc : Arena.slugcatSelectDescriptions[SlugcatStats.Name.White.value]);
        slugcatNameLabel.text = menu.Translate(Arena.slugcatSelectDisplayNames.TryGetValue(slugcat.value, out string name) ? name : $"THE {SlugcatStats.getSlugcatName(slugcat).ToUpper()}");
        if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer && UnityEngine.Random.Range(0, 1000) == 0)
        {
            menu.PlaySound(RainMeadow.Ext_SoundID.Fartificer);
            slugcatNameLabel.text = menu.Translate("THE FARTIFICER");
        }
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
        SwitchSelectedSlugcat(allSlugcats[to]);
    }
}