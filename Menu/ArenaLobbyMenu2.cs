using System.Collections.Generic;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI;

public class ArenaLobbyMenu2 : SmartMenu, SelectOneButton.SelectOneButtonOwner
{
    public List<SlugcatStats.Name> allSlugcats = ArenaHelpers.AllSlugcats();
    public SimplerButton playButton;
    public FSprite[] settingsDivSprites;
    public Vector2[] settingsDivSpritesPos;
    public RestorableMenuLabel countdownTimerLabel;
    public RestorableMenuLabel? saintAscendanceTimerLabel;
    public SimplerCheckbox spearsHitCheckbox, aggressiveAICheckBox;
    public SimplerCheckbox? maulingCheckBox, artificerStunCheckBox, sainotCheckBox, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox;
    public SimplerMultipleChoiceArray roomRepeatChoiceArray, rainTimerChoiceArray, wildlifeChoiceArray;
    // public TextBox countdownTimerTextBox, saintAscendDurationTimerTextBox;
    // public ComboBox arenaGameModeComboBox;
    public EventfulSelectOneButton[] slugcatSelectButtons;
    public TabContainer tabContainer;
    public string[] PainCatNames => ["Inv", "Enot", "Paincat", "Sofanthiel", "Gorbo"]; // not using "???" cause it might cause some confusion to players who don't know Inv
    public string? painCatName;
    public int selectedSlugcatIndex = 0;
    public Page slugcatSelectPage;
    public bool pagesMoving = false;
    public float pageMovementProgress = 0f;
    public Vector2[] oldPagesPos = [];
    public Vector2 newPagePos = Vector2.zero;
    public bool pageFullyTransitioned = true;
    public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;

    public ArenaLobbyMenu2(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ArenaLobbyMenu)
    {
        RainMeadow.DebugMe();

        pages.Add(slugcatSelectPage = new Page(this, null, "slugcat select", 1));
        slugcatSelectPage.pos.x += 1500f;

        scene.AddIllustration(new MenuIllustration(this, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
        scene.AddIllustration(new MenuIllustration(this, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
        scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.shader = manager.rainWorld.Shaders["MenuText"];

        playButton = new(this, mainPage, Utils.Translate("READY?"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
        playButton.OnClick += _ => MovePage(new Vector2(-1500f, 0f), 1);
        mainPage.subObjects.Add(playButton);

        tabContainer = new TabContainer(this, mainPage, new Vector2(470f, 125f), new Vector2(450, 475));
        mainPage.subObjects.Add(tabContainer);

        Vector2 matchSettingsOffset = new(120f, 205f);
        float settingsElementWidth = 300f;

        spearsHitCheckbox = new SimplerCheckbox(this, tabContainer, matchSettingsOffset + new Vector2(0f, 220f), 95f, Translate("Friendly Fire:"));
        spearsHitCheckbox.OnClick += c => RainMeadow.Debug($"friendly fire: {c}");

        aggressiveAICheckBox = new SimplerCheckbox(this, tabContainer, matchSettingsOffset + new Vector2(settingsElementWidth - 24f, 220f), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 120f : 100f, Translate("Aggressive AI:"));
        aggressiveAICheckBox.OnClick += c => RainMeadow.Debug($"aggressive ai: {c}");

        roomRepeatChoiceArray = new SimplerMultipleChoiceArray(this, tabContainer, matchSettingsOffset + new Vector2(0f, 150f), Translate("Repeat Rooms:"), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 115f : 95f, settingsElementWidth, 5, textInBoxes: true);
        roomRepeatChoiceArray.OnClick += i => RainMeadow.Debug($"room repeat: pressed {i}");
        for (int i = 0; i < roomRepeatChoiceArray.buttons.Length; i++)
            roomRepeatChoiceArray.buttons[i].label.text = $"{i + 1}x";

        rainTimerChoiceArray = new SimplerMultipleChoiceArray(this, tabContainer, matchSettingsOffset + new Vector2(0f, 100f), Translate("Rain Timer:"), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 100f : 95f, settingsElementWidth, 6, splitText: CurrLang == InGameTranslator.LanguageID.French || CurrLang == InGameTranslator.LanguageID.Spanish || CurrLang == InGameTranslator.LanguageID.Portuguese);
        rainTimerChoiceArray.OnClick += i => RainMeadow.Debug($"rain timer: pressed {i}");

        wildlifeChoiceArray = new SimplerMultipleChoiceArray(this, tabContainer, matchSettingsOffset + new Vector2(0f, 50f), Translate("Wildlife:"), 95f, settingsElementWidth, 4);
        wildlifeChoiceArray.OnClick += i => RainMeadow.Debug($"wildlife: pressed {i}");

        countdownTimerLabel = new RestorableMenuLabel(this, tabContainer, Translate("Countdown Timer:"), new Vector2(25f, 160f), new Vector2(105f, 20f), false);

        // settingsMenuLabels = new MenuLabel[2];
        // settingsMenuLabels[0] = new MenuLabel(this, mainPage, "Countdown Timer:", matchSettingsOffset + new Vector2(0f, -17f), new Vector2(0f, 30f), false);
        // settingsMenuLabels[1] = new MenuLabel(this, mainPage, "Saint Ascend Time:", matchSettingsOffset + new Vector2(125f, -67f), new Vector2(0f, 30f), false);
        // mainPage.subObjects.AddRange(settingsMenuLabels);

        // countdownTimerTextBox = new OpTextBox(new Configurable<int>(5), matchSettingsOffset + new Vector2(215f, -20f), 95f);
        // countdownTimerTextBox.OnChange += () => { RainMeadow.Debug($"countdown timer textbox: {countdownTimerTextBox.value}"); };
        // UIelementWrapper countdownTimerTextBoxWrapper = new(tabWrapper, countdownTimerTextBox);
        // mainPage.subObjects.Add(countdownTimerTextBoxWrapper);

        tabContainer.AddTab("Arena Playlist", []);
        tabContainer.AddTab(
            "Match Settings",
            [
                spearsHitCheckbox,
                aggressiveAICheckBox,
                roomRepeatChoiceArray,
                rainTimerChoiceArray,
                wildlifeChoiceArray,
                countdownTimerLabel,
                // countdownTimerTextBox,
                // arenaGameModeComboBox,
            ]
        );

        settingsDivSprites = new FSprite[2];
        settingsDivSpritesPos = new Vector2[2];
        for (int i = 0; i < settingsDivSprites.Length; i++)
        {
            settingsDivSprites[i] = new FSprite("pixel")
            {
                anchorX = 0f,
                scaleX = settingsElementWidth + 95f,
                scaleY = 2f,
                color = MenuRGB(MenuColors.VeryDarkGrey),
            };
            settingsDivSpritesPos[i] = tabContainer.pos + matchSettingsOffset + new Vector2(-95f, 197f - (171 * i));
        }

        if (ModManager.MSC)
        {
            Vector2 abilitySettingsOffset = new(360f, 380f);
            Vector2 abilitySettingsSpacing = new(0f, 50f);
            painCatName = PainCatNames[Random.Range(0, PainCatNames.Length)];

            maulingCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset, 300f, Translate("Enable Mauling:"));
            maulingCheckBox.OnClick += i => RainMeadow.Debug($"mauling: {i}");

            artificerStunCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, Translate("Artificer Stuns Players:"));
            artificerStunCheckBox.OnClick += i => RainMeadow.Debug($"artificer stuns: {i}");

            sainotCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, "Sain't:");
            sainotCheckBox.OnClick += i => RainMeadow.Debug($"sain't: {i}");

            saintAscendanceTimerLabel = new RestorableMenuLabel(this, tabContainer, "Saint Ascendance Duration:", (abilitySettingsOffset -= abilitySettingsSpacing) - new Vector2(300f, 0f), new Vector2(151f, 20f), false);

            painCatEggCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, $"{painCatName} gets egg at 0 throw skill:");
            painCatEggCheckBox.OnClick += c => RainMeadow.Debug($"paincat egg: {c}");

            painCatThrowsCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, $"{painCatName} can always throw spears:");
            painCatThrowsCheckBox.OnClick += c => RainMeadow.Debug($"paincat spear: {c}");

            painCatLizardCheckBox = new SimplerCheckbox(this, tabContainer, abilitySettingsOffset -= abilitySettingsSpacing, 300f, $"{painCatName} sometimes gets a friend:");
            painCatLizardCheckBox.OnClick += c => RainMeadow.Debug($"paincat friend: {c}");

            tabContainer.AddTab(
             "Slugcat Abilities",
                [
                    maulingCheckBox,
                    artificerStunCheckBox,
                    sainotCheckBox,
                    saintAscendanceTimerLabel,
                    // saintAscendDurationTimerTextBox,
                    painCatLizardCheckBox,
                    painCatEggCheckBox,
                    painCatThrowsCheckBox,
                ]
            );
        }

        SimplerButton swapBackButton = new(this, slugcatSelectPage, "Change Page Back", new Vector2(600f, 300f), new Vector2(200f, 30f));
        swapBackButton.OnClick += _ => MovePage(new Vector2(1500f, 0f), 0);
        slugcatSelectPage.subObjects.Add(swapBackButton);

        slugcatSelectButtons = new EventfulSelectOneButton[allSlugcats.Count];
        int buttonsInTopRow = (int)Mathf.Floor(allSlugcats.Count / 2f);
        int buttonsInBottomRow = allSlugcats.Count - buttonsInTopRow;
        float topRowStartingXPos = 633f - (buttonsInTopRow / 2 * 110f - ((buttonsInTopRow % 2 == 0) ? 55f : 0f));
        float bottomRowStartingXPos = 633f - (buttonsInBottomRow / 2 * 110f - ((buttonsInBottomRow % 2 == 0) ? 55f : 0f));
        RainMeadow.Debug(bottomRowStartingXPos);

        for (int i = 0; i < allSlugcats.Count; i++)
        {
            Vector2 pos = i < buttonsInTopRow ? new Vector2(topRowStartingXPos + 110f * i, 450f) : new Vector2(bottomRowStartingXPos + 110f * (i - buttonsInTopRow), 340f);
            EventfulSelectOneButton btn = new(this, slugcatSelectPage, "", "scug select", pos, new Vector2(100f, 100f), slugcatSelectButtons, i);

            MenuIllustration portrait = new(this, btn, "", ArenaImage(allSlugcats[i], i), btn.size / 2, true, true);
            // portrait.sprite.SetElementByName(portrait.fileName);
            btn.subObjects.Add(portrait);

            slugcatSelectPage.subObjects.Add(btn);
            slugcatSelectButtons[i] = btn;
        }
    }

    public void MovePage(Vector2 direction, int index)
    {
        if (pagesMoving) return;

        pagesMoving = true;
        pageMovementProgress = 0f;
        newPagePos = direction;
        oldPagesPos = new Vector2[pages.Count];
        for (int i = 0; i < oldPagesPos.Length; i++)
            oldPagesPos[i] = pages[i].pos;

        currentPage = index;
        pageFullyTransitioned = false;

        PlaySound(SoundID.MENU_Next_Slugcat);
    }

    public string ArenaImage(SlugcatStats.Name classID, int color)
    {
        if (classID == null) return $"MultiplayerPortrait{color}2";

        List<SlugcatStats.Name> baseGameSlugs = ArenaHelpers.BaseGameSlugcats();
        List<SlugcatStats.Name> vanillaSlugs = ArenaHelpers.VanillaSlugs();
        List<SlugcatStats.Name> mscSlugs = ArenaHelpers.MSCSlugs();

        RainMeadow.Debug($"Player is playing as {classID} with color index {color}");

        if (vanillaSlugs.Contains(classID)) return $"MultiplayerPortrait{color}1";

        if (ModManager.Watcher && classID == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
            return $"MultiplayerPortrait{3}1"; // take advantage of nightcat profile pic

        if (ModManager.MSC && mscSlugs.Contains(classID))
        {
            if (classID == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                int randomChoice = Random.Range(0, 5);
                return $"MultiplayerPortrait{randomChoice}1-{allSlugcats[color]}";
            }
            return $"MultiplayerPortrait41-{allSlugcats[color]}";
        }

        if (!baseGameSlugs.Contains(classID)) return $"MultiplayerPortrait01-{classID}";

        return $"MultiplayerPortrait{color}1-{classID}";
    }

    public override void Update()
    {
        if (currentPage == 1)
        {
            SmartMenuUpdateNoEscapeCheck();
            if (RWInput.CheckPauseButton(0)) MovePage(new Vector2(1500f, 0f), 0);
        }
        else if (pageFullyTransitioned) base.Update();
        else SmartMenuUpdateNoEscapeCheck();

        if (!pagesMoving) return;

        pageMovementProgress += 0.35f;
        float baseMoveSpeed = Mathf.Lerp(8f, 125f, Custom.SCurve(pageMovementProgress, 0.85f));
        for (int i = 0; i < pages.Count; i++)
        {
            float totalTravelDistance = Vector2.Distance(oldPagesPos[i], oldPagesPos[i] + newPagePos);
            float distanceToTravel = Vector2.Distance(pages[i].pos, oldPagesPos[i] + newPagePos);
            float easing = Mathf.Lerp(1f, 0.01f, Mathf.InverseLerp(totalTravelDistance, 0.1f, distanceToTravel));

            pages[i].pos = Custom.MoveTowards(pages[i].pos, oldPagesPos[i] + newPagePos, baseMoveSpeed * easing);

            if (pages[i].pos == oldPagesPos[i] + newPagePos)
            {
                pagesMoving = false;
                pageFullyTransitioned = true;
            }
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (tabContainer.activeIndex == 1)
            for (int i = 0; i < settingsDivSprites.Length; i++)
            {
                container.AddChild(settingsDivSprites[i]);
                var divSprite = settingsDivSprites[i];
                var divSpritePos = settingsDivSpritesPos[i];

                divSprite.x = mainPage.DrawX(timeStacker) + divSpritePos.x;
                divSprite.y = mainPage.DrawY(timeStacker) + divSpritePos.y;
            }
        else
            for (int i = 0; i < settingsDivSprites.Length; i++)
                container.RemoveChild(settingsDivSprites[i]);
    }

    public int GetCurrentlySelectedOfSeries(string series)
    {
        return selectedSlugcatIndex; // no need to check series (for now) since there is only one SelectOneButton in this menu
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        selectedSlugcatIndex = to; // no need to check series (for now) since there is only one SelectOneButton in this menu
    }
}

