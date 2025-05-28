using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI;

public class ArenaOnlineLobbyMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner
{
    public static string[] PainCatNames => ["Inv", "Enot", "Paincat", "Sofanthiel", "Gorbo"]; // not using "???" cause it might cause some confusion to players who don't know Inv
    public List<SlugcatStats.Name> allSlugcats = ArenaHelpers.allSlugcats;
    public SimplerButton playButton, slugcatSelectBackButton;
    public OnlineArenaSettingsInferface arenaSettingsInterface;
    public OnlineSlugcatAbilitiesInterface? slugcatAbilitiesInterface;
    public MenuLabel slugcatNameLabel, slugcatDescriptionLabel;
    public Vector2 newPagePos = Vector2.zero;
    public EventfulSelectOneButton[] slugcatSelectButtons;
    public FSprite[] slugcatDescriptionGradients;
    public Vector2[] slugcatDescriptionGradientsPos, oldPagesPos = [];
    public TabContainer tabContainer;
    public PlayerDisplayer? playerDisplayer;
    public Dialog? slugcatDialog;
    public MenuIllustration competitiveTitle, competitiveShadow;
    public MenuScene.SceneID slugcatScene;
    public Page slugcatSelectPage;
    public bool pagesMoving = false, pageFullyTransitioned = true, pendingBgChange = false;
    public float pageMovementProgress = 0, desiredBgCoverAlpha = 0, lastDesiredBgCoverAlpha = 0;
    public string? painCatName, painCatDescription;
    public int selectedSlugcatIndex = 0;
    public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;
    public ArenaSetup GetArenaSetup => manager.arenaSetup;
    public ArenaSetup.GameTypeID CurrentGameType { get => GetArenaSetup.currentGameType; set => GetArenaSetup.currentGameType = value; }
    public ArenaSetup.GameTypeSetup GetGameTypeSetup => GetArenaSetup.GetOrInitiateGameTypeSetup(CurrentGameType);
    private ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
    public ArenaOnlineLobbyMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ArenaLobbyMenu)
    {
        RainMeadow.DebugMe();

        if (OnlineManager.lobby == null)
            throw new InvalidOperationException("lobby is null");

        backTarget = RainMeadow.Ext_ProcessID.LobbySelectMenu;
        if (Arena.myArenaSetup == null) manager.arenaSetup = Arena.myArenaSetup = new(manager); //loading it on game mode ctor loads the base setup prob due to lobby still being null
        Futile.atlasManager.LoadAtlas("illustrations/arena_ui_elements");



        Arena.AddExternalGameModes(new Competitive(), Competitive.CompetitiveMode);

        if (Arena.currentGameMode == "" || Arena.currentGameMode == null)
            Arena.currentGameMode = Competitive.CompetitiveMode.value;

        pages.Add(slugcatSelectPage = new Page(this, null, "slugcat select", 1));
        slugcatSelectPage.pos.x += 1500f;
        ChangeScene(slugcatScene = Arena.slugcatSelectMenuScenes[Arena.arenaClientSettings.playingAs.value]);
        competitiveShadow = new(this, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), true, false);
        competitiveTitle = new(this, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), true, false);
        competitiveTitle.sprite.shader = manager.rainWorld.Shaders["MenuText"];

        playButton = new(this, mainPage, Utils.Translate("READY?"), new Vector2(1056f, 50f), new Vector2(110f, 30f));

        tabContainer = new(this, mainPage, new Vector2(470f, 125f), new Vector2(450, 475));

        mainPage.SafeAddSubobjects(competitiveShadow, competitiveTitle, playButton, tabContainer);

        TabContainer.Tab playListTab = tabContainer.AddTab("Arena Playlist"),
            matchSettingsTab = tabContainer.AddTab("Match Settings");

        var x = new VerticalScrollSelector(this, playListTab, new Vector2(100, 100), new Vector2(200, 50), 3);
        x.AddSideButton("Menu_Symbol_Show_Thumbs", "test");
        x.AddSideButton("Menu_Symbol_Show_List", "test 2");
        x.AddScrollElements(
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50)),
            new ButtonScroller.ScrollerButton(this, x, "Hi", default, new Vector2(200, 50))
            );
        playListTab.AddObjects(x);

        arenaSettingsInterface = new(this, matchSettingsTab, new(120, 205), Arena.currentGameMode, [.. Arena.registeredGameModes.Values.Select(v => new ListItem(v))]);
        arenaSettingsInterface.CallForSync();
        matchSettingsTab.AddObjects(arenaSettingsInterface);

        if (ModManager.MSC)
        {
            painCatName = PainCatNames[UnityEngine.Random.Range(0, PainCatNames.Length)];
            TabContainer.Tab slugabilitiesTab = tabContainer.AddTab("Slugcat Abilities");
            slugcatAbilitiesInterface = new(this, slugabilitiesTab, new(360, 380), new(0, 50), painCatName);
            slugcatAbilitiesInterface.CallForSync();
            slugabilitiesTab.AddObjects(slugcatAbilitiesInterface);
        }

        slugcatSelectBackButton = new SimplerButton(this, slugcatSelectPage, "Back To Lobby", new Vector2(200f, 50f), new Vector2(110f, 30f), description: "Go back to main lobby");
        slugcatSelectBackButton.OnClick += _ => MovePage(new Vector2(1500f, 0f), 0);
        slugcatSelectPage.subObjects.Add(slugcatSelectBackButton);

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

            Vector2 pos = i < buttonsInTopRow ? new Vector2(topRowStartingXPos + 110f * i, 450f) : new Vector2(bottomRowStartingXPos + 110f * (i - buttonsInTopRow), 340f);
            EventfulSelectOneButton btn = new(this, slugcatSelectPage, "", "scug select", pos, new Vector2(100f, 100f), slugcatSelectButtons, i);
            btn.OnClick += _ => SwitchSelectedSlugcat(allSlugcats[index]);

            MenuIllustration portrait = new(this, btn, "", SlugcatColorableButton.GetFileForSlugcat(allSlugcats[i], false), btn.size / 2, true, true);
            btn.subObjects.Add(portrait);

            if (allSlugcats[i] == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                painCatButton = btn;
                painCatPortrait = portrait;
                painCatDescription = Arena.slugcatSelectPainCatDescriptions
                [
                    UnityEngine.Random.Range(0, 3) == 1 ? // 33% chance for portrait specific description, 66% for general quotes
                    int.Parse(Regex.Match(painCatPortrait.fileName, @"\d+").Value[0].ToString()) :
                    UnityEngine.Random.Range(5, Arena.slugcatSelectPainCatDescriptions.Count)
                ].Replace("<USERNAME>", OnlineManager.mePlayer.id.name);
            }

            slugcatSelectPage.subObjects.Add(btn);
            slugcatSelectButtons[i] = btn;
        }

        SimplerButton randomizePainCat = new(this, slugcatSelectPage, $"Randomize {painCatName} select data", new Vector2(1056, 50), new Vector2(300, 30));
        randomizePainCat.OnClick += _ =>
        {
            painCatPortrait.RemoveSprites();
            slugcatSelectPage.RemoveSubObject(painCatPortrait);
            slugcatSelectPage.SafeAddSubobjects(painCatPortrait = new(this, painCatButton, "", SlugcatColorableButton.GetFileForSlugcat(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel, false), painCatButton.size / 2, true, true));
            bool usePortraitDescription = UnityEngine.Random.Range(0, 3) == 1;
            int portraitIndex = int.Parse(Regex.Match(painCatPortrait.fileName, @"\d+").Value[0].ToString());
            int descriptionIndex = usePortraitDescription ? UnityEngine.Random.Range(5, Arena.slugcatSelectPainCatDescriptions.Count) : portraitIndex;
            RainMeadow.Debug($"description index: {descriptionIndex}; portrait index: {portraitIndex}; random: {usePortraitDescription}; portrait name: {painCatPortrait.fileName}");

            painCatDescription = Arena.slugcatSelectPainCatDescriptions[descriptionIndex].Replace("<USERNAME>", OnlineManager.mePlayer.id.name);
            SwitchSelectedSlugcat(MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel);
        };
        slugcatSelectPage.subObjects.Add(randomizePainCat);

        MenuLabel chooseYourSlugcatLabel = new(this, slugcatSelectPage, "Choose Your Slugcat", new Vector2(680f, 575f), default, true);
        chooseYourSlugcatLabel.label.color = new Color(0.5f, 0.5f, 0.5f);
        slugcatSelectPage.subObjects.Add(chooseYourSlugcatLabel);

        slugcatNameLabel = new MenuLabel(this, slugcatSelectPage, Arena.slugcatSelectDisplayNames[Arena.arenaClientSettings.playingAs.value], new Vector2(680f, 310f), default, true);
        slugcatSelectPage.subObjects.Add(slugcatNameLabel);

        slugcatDescriptionLabel = new MenuLabel(this, slugcatSelectPage, Arena.slugcatSelectDescriptions[Arena.arenaClientSettings.playingAs.value], new Vector2(680f, 210f), default, bigText: true);
        slugcatDescriptionLabel.label.color = new Color(0.8f, 0.8f, 0.8f);
        slugcatSelectPage.subObjects.Add(slugcatDescriptionLabel);

        slugcatDescriptionGradients = new FSprite[4];
        slugcatDescriptionGradientsPos = new Vector2[4];

        for (int i = 0; i < slugcatDescriptionGradients.Length; i++)
        {
            slugcatDescriptionGradients[i] = new FSprite("LinearGradient200")
            {
                rotation = i % 2 == 0 ? 270f : 90f,
                scaleY = 2f,
                anchorX = 0.6f,
                anchorY = 0f,
            };
            slugcatDescriptionGradientsPos[i] = new Vector2(680f, i > 1 ? 280f : 125f);
            container.AddChild(slugcatDescriptionGradients[i]);
        }

        BuildPlayerDisplay();
        MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        RainMeadow.Debug(GetArenaSetup.playerClass[0]?.value ?? "NULL");
        SwitchSelectedSlugcat(GetArenaSetup.playerClass[0]);
    }

    public void ChangeScene(MenuScene.SceneID sceneID)
    {
        slugcatScene = sceneID;
        pendingBgChange = false;

        if (scene.sceneID == sceneID) return;

        scene.ClearMenuObject(scene);
        scene = new InteractiveMenuScene(this, pages[0], sceneID);
        mainPage.subObjects.Add(scene);
        if (scene.depthIllustrations != null && scene.depthIllustrations.Count > 0)
        {
            int count = scene.depthIllustrations.Count;
            while (count-- > 0)
                scene.depthIllustrations[count].sprite.MoveToBack();
        }
        else
        {
            int count2 = scene.flatIllustrations.Count;
            while (count2-- > 0)
                scene.flatIllustrations[count2].sprite.MoveToBack();
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

    public void SwitchSelectedSlugcat(SlugcatStats.Name slugcat)
    {
        if (!RainMeadow.isArenaMode(out _))
        {
            RainMeadow.Error("arena is null, slugcat wont be changed!");
            return;
        }
        slugcat = allSlugcats.IndexOf(slugcat) == -1? allSlugcats[0] : slugcat;
        slugcatScene = Arena.slugcatSelectMenuScenes[slugcat.value];
        Arena.arenaClientSettings.playingAs = slugcat;
        GetArenaSetup.playerClass[0] = slugcat;
        selectedSlugcatIndex = allSlugcats.IndexOf(slugcat);
        RainMeadow.Debug($"My Slugcat: {Arena.arenaClientSettings.playingAs}, in lobby list of client settings: {(ArenaHelpers.GetArenaClientSettings(OnlineManager.mePlayer)?.playingAs?.value) ?? "NULL!"}");
        if (slugcat == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
        {
            slugcatDescriptionLabel.text = painCatDescription;
            slugcatNameLabel.text = painCatName;
            return;
        }

        slugcatDescriptionLabel.text = Arena.slugcatSelectDescriptions[slugcat.value];
        slugcatNameLabel.text = Arena.slugcatSelectDisplayNames[slugcat.value];
    }
    public override void ShutDownProcess()
    {
        if (OnlineManager.lobby?.isOwner == true)
        {
            SaveInterfaceOptions();
            GetArenaSetup.SaveToFile();
            RainMeadow.rainMeadowOptions._SaveConfigFile();
        }
        else (GetArenaSetup as ArenaOnlineSetup)?.SaveNonSessionToFile();
        manager.rainWorld.progression.SaveProgression(true, true);
        if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
        {
            OnlineManager.LeaveLobby();
            manager.arenaSetup = null;
        }
        base.ShutDownProcess();
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

        foreach (SelectOneButton button in slugcatSelectButtons)
            button.buttonBehav.greyedOut = pendingBgChange;

        lastDesiredBgCoverAlpha = desiredBgCoverAlpha;
        desiredBgCoverAlpha = Mathf.Clamp(desiredBgCoverAlpha + (pendingBgChange ? 0.01f : -0.01f), 0.8f, 1.1f);
        pendingBgChange = pendingBgChange || slugcatScene != scene.sceneID;

        if (pendingBgChange && menuDarkSprite.darkSprite.alpha >= 1) ChangeScene(slugcatScene);
        if (pagesMoving) UpdateMovingPage();
        UpdateOnlineUI();
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        menuDarkSprite.darkSprite.alpha = Mathf.Clamp(Mathf.Lerp(lastDesiredBgCoverAlpha, desiredBgCoverAlpha, timeStacker), 0.8f, 1.1f);
        for (int i = 0; i < slugcatDescriptionGradients.Length; i++)
        {
            FSprite gradientSprite = slugcatDescriptionGradients[i];
            Vector2 gradientSpritePos = slugcatDescriptionGradientsPos[i];

            gradientSprite.x = slugcatSelectPage.DrawX(timeStacker) + gradientSpritePos.x;
            gradientSprite.y = slugcatSelectPage.DrawY(timeStacker) + gradientSpritePos.y;
        }
    }
    public override string UpdateInfoText()
    {
        if (selectedObject is CheckBox checkBox)
        {
            string idString = checkBox.IDString;
            if (idString == "SPEARSHIT")
                return arenaSettingsInterface.GetGameTypeSetup.spearsHitPlayers ? Translate("Player vs player deathmatch") : Translate("Eating contest");
            if (idString == "EVILAI")
                return arenaSettingsInterface.GetGameTypeSetup.evilAI ? Translate("Creatures are vicious and aggressive") : Translate("Normal Rain World AI");
        }
        if (selectedObject is MultipleChoiceArray.MultipleChoiceButton arrayBtn)
        {
            string idString = arrayBtn.multipleChoiceArray.IDString;
            int index = arrayBtn.index;
            if (idString == "ROOMREPEAT")
            {
                string numberText = index == 0 ? "once" : index == 1 ? "twice" : index == 2 ? "three times" : index == 3 ? "four times" : "five times";
                return Translate($"Play each level {numberText}");
            }
            if (idString == "SESSIONLENGTH")
            {
                return Translate(index < 0 || index >= ArenaSetup.GameTypeSetup.SessionTimesInMinutesArray.Length ? "No rain" : $"{ArenaSetup.GameTypeSetup.SessionTimesInMinutesArray[index]} minute{(index == 1 ? "" : "s")} until rain");
            }
            if (idString == "WILDLIFE")
            {
                ArenaSetup.GameTypeSetup.WildLifeSetting settingFromBtn = new(ExtEnum<ArenaSetup.GameTypeSetup.WildLifeSetting>.values.GetEntry(index), false);
                string value = settingFromBtn == ArenaSetup.GameTypeSetup.WildLifeSetting.Off ? "No" : settingFromBtn.value;
                return Translate($"{value} wildlife");
            }
        }
        return base.UpdateInfoText();
    }
    public void UpdateOnlineUI() //for future online ui stuff
    {
        if (!RainMeadow.isArenaMode(out _)) return;
        SlugcatStats.Name slugcat = Arena.arenaClientSettings.playingAs;
        Arena.arenaClientSettings.selectingSlugcat = currentPage == 1;
        Arena.arenaClientSettings.slugcatColor = this.IsCustomColorEnabled(slugcat)? ColorHelpers.HSL2RGB(ColorHelpers.RWJollyPicRange(this.GetMenuHSL(slugcat, 0))) : Color.black;
        if (playerDisplayer != null)
        {
            foreach (ButtonScroller.IPartOfButtonScroller button in playerDisplayer.buttons)
            {
                if (button is ArenaPlayerBox playerBox)
                {
                    ArenaClientSettings? clientSettings = ArenaHelpers.GetArenaClientSettings(playerBox.profileIdentifier);
                    playerBox.slugcatButton.LoadNewSlugcat(clientSettings?.playingAs, clientSettings != null && clientSettings.slugcatColor != Color.black, false);
                    playerBox.isSelectingSlugcat = clientSettings?.selectingSlugcat ?? false;

                    if (playerBox.slugcatButton.isColored) playerBox.slugcatButton.portraitColor = (clientSettings?.slugcatColor ?? Color.white);
                    else playerBox.slugcatButton.portraitColor = Color.white;
                }
                if (button is ArenaPlayerSmallBox smallPlayerBox)
                    smallPlayerBox.slugcatButton.slug = ArenaHelpers.GetArenaClientSettings(smallPlayerBox.profileIdentifier)?.playingAs;
            }
        }
    }
    public void UpdateMovingPage()
    {
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
    public void SaveInterfaceOptions()
    {
        RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value = arenaSettingsInterface.countdownTimerTextBox.valueInt;
        if (slugcatAbilitiesInterface != null)
        {
            RainMeadow.rainMeadowOptions.BlockMaul.Value = slugcatAbilitiesInterface.blockMaulCheckBox.Checked;
            RainMeadow.rainMeadowOptions.BlockArtiStun.Value = slugcatAbilitiesInterface.blockArtiStunCheckBox.Checked;
            RainMeadow.rainMeadowOptions.ArenaSAINOT.Value = slugcatAbilitiesInterface.sainotCheckBox.Checked;
            RainMeadow.rainMeadowOptions.PainCatEgg.Value = slugcatAbilitiesInterface.painCatEggCheckBox.Checked;
            RainMeadow.rainMeadowOptions.PainCatThrows.Value = slugcatAbilitiesInterface.painCatThrowsCheckBox.Checked;
            RainMeadow.rainMeadowOptions.PainCatLizard.Value = slugcatAbilitiesInterface.painCatLizardCheckBox.Checked;
            RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value = slugcatAbilitiesInterface.saintAscendDurationTimerTextBox.valueInt;
        }
    }
    public void BuildPlayerDisplay()
    {
        if (playerDisplayer != null) return;

        playerDisplayer = new(this, mainPage, new(960, 130), OnlineManager.players, GetPlayerButton, 4, ArenaPlayerBox.DefaultSize.x + 30, ArenaPlayerBox.DefaultSize.y, 0, ArenaPlayerSmallBox.DefaultSize.y, 10);
        mainPage.subObjects.Add(playerDisplayer);
        playerDisplayer.CallForRefresh();
    }
    public void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
    {
        if (!RainMeadow.isArenaMode(out _)) return;

        RainMeadow.DebugMe();
        BuildPlayerDisplay();
        playerDisplayer?.UpdatePlayerList(OnlineManager.players);
    }
    public ButtonScroller.IPartOfButtonScroller GetPlayerButton(PlayerDisplayer playerDisplay, bool isLargeDisplay, OnlinePlayer player, Vector2 pos)
    {
        if (isLargeDisplay)
        {
            ArenaPlayerBox playerBox = new(this, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos); //buttons init prevents kick button if isMe
            if (player.isMe)
            {
                playerBox.slugcatButton.OnClick += (_) =>
                {
                    MovePage(new Vector2(-1500f, 0f), 1);
                };
                playerBox.colorInfoButton.OnClick += (_) =>
                {
                    OpenColorConfig(playerBox.slugcatButton.slugcat);
                };
            }
            playerBox.slugcatButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
            return playerBox;
        }

        ArenaPlayerSmallBox playerSmallBox = new(this, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos);

        if (player.isMe)
        {
            playerSmallBox.slugcatButton.OnClick += _ => MovePage(new Vector2(-1500f, 0f), 1);
            playerSmallBox.colorKickButton!.OnClick += _ => OpenColorConfig(playerSmallBox.slugcatButton.slug);
        }

        playerSmallBox.playerButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
        return playerSmallBox;
    }
    public void OpenColorConfig(SlugcatStats.Name? slugcat)
    {
        if (!ModManager.MMF)
        {
            PlaySound(SoundID.MENU_Checkbox_Uncheck);
            slugcatDialog = new DialogNotify("You cant color without Remix on!", new(500, 200), manager, () => { });
            manager.ShowDialog(slugcatDialog);
            return;
        }
        PlaySound(SoundID.MENU_Checkbox_Check);
        slugcatDialog = new ColorMultipleSlugcatsDialog(manager, () => { }, allSlugcats, slugcat);
        manager.ShowDialog(slugcatDialog);
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
