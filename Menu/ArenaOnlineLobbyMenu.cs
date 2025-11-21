using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using MoreSlugcats;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI.Components;
using RainMeadow.UI.Pages;
using RWCustom;
using UnityEngine;
using static RainMeadow.UI.Components.ArenaLevelSelector;

namespace RainMeadow.UI;

public class ArenaOnlineLobbyMenu : SmartMenu
{
    public ArenaMainLobbyPage arenaMainLobbyPage;
    public ArenaSlugcatSelectPage arenaSlugcatSelectPage;
    public Vector2 newPagePos = Vector2.zero;
    public Vector2[] oldPagesPos = [];
    public MenuIllustration competitiveTitle, competitiveShadow;
    public Page slugcatSelectPage;
    public MenuScene.SceneID? pendingScene;
    public bool pagesMoving = false, pushClientIntoGame, forceFlatIllu;
    public int painCatIndex, customTextDescriptionCounter;
    public float pageMovementProgress = 0, desiredBgCoverAlpha = 0, lastDesiredBgCoverAlpha = 0;
    public string painCatName, customTextDescription;
    public bool initiateStartGameAfterCountDown;
    private int lastCountdownSoundPlayed = -1;
    public bool SettingsDisabled => OnlineManager.lobby?.isOwner != true || Arena.initiateLobbyCountdown;
    public override bool CanEscExit => base.CanEscExit && currentPage == 0 && !pagesMoving;
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
        Arena.currentLobbyOwner = OnlineManager.lobby.owner;
        backTarget = RainMeadow.Ext_ProcessID.LobbySelectMenu;
        forceFlatIllu = !manager.rainWorld.flatIllustrations;
        if (backObject is SimplerButton btn) btn.description = Translate("Exit to Lobby Select");
        if (Arena.myArenaSetup == null) manager.arenaSetup = Arena.myArenaSetup = new ArenaOnlineSetup(manager); //loading it on game mode ctor loads the base setup prob due to lobby still being null
        Futile.atlasManager.LoadAtlas("illustrations/arena_ui_elements");
        Futile.atlasManager.LoadAtlas("illustrations/ui_elements");
        if (Arena.currentGameMode == "" || Arena.currentGameMode == null)
            Arena.currentGameMode = FFA.FFAMode.value;

        pages.Add(slugcatSelectPage = new Page(this, null, "slugcat select", 1));
        slugcatSelectPage.pos.x += 1500f;
        competitiveShadow = new(this, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), true, false);
        competitiveTitle = new(this, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), true, false);
        competitiveTitle.sprite.shader = manager.rainWorld.Shaders["MenuText"];

        painCatName = Arena.slugcatSelectPainCatNames.GetValueOrDefault(UnityEngine.Random.Range(0, Arena.slugcatSelectPainCatNames.Count), "")!;
        painCatIndex = UnityEngine.Random.Range(0, 5);

        arenaMainLobbyPage = new ArenaMainLobbyPage(this, mainPage, default, painCatName, painCatIndex);
        arenaSlugcatSelectPage = new ArenaSlugcatSelectPage(this, slugcatSelectPage, default, painCatName, painCatIndex);
        ChatLogManager.Subscribe(arenaMainLobbyPage.chatMenuBox);
        mainPage.SafeAddSubobjects(competitiveShadow, competitiveTitle, arenaMainLobbyPage);
        slugcatSelectPage.SafeAddSubobjects(arenaSlugcatSelectPage);
        Arena.ResetOnReturnMenu(manager);
        RemoveAndAddNewExtGameModeTab(Arena.externalArenaGameMode);
        initiateStartGameAfterCountDown = false;
        lastCountdownSoundPlayed = -1;
    }

    public void ChangeScene()
    {
        if (pendingScene == null) return;
        manager.rainWorld.flatIllustrations = manager.rainWorld.flatIllustrations || forceFlatIllu;
        mainPage.ClearMenuObject(scene);
        scene = new InteractiveMenuScene(this, pages[0], pendingScene);
        mainPage.SafeAddSubobjects(scene);
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
        pendingScene = null;
        manager.rainWorld.flatIllustrations = !forceFlatIllu;
    }
    public void MovePage(Vector2 direction, int index)
    {
        if (pagesMoving || currentPage == index) return;

        pagesMoving = true;
        pageMovementProgress = 0f;
        newPagePos = direction;
        oldPagesPos = new Vector2[pages.Count];
        for (int i = 0; i < oldPagesPos.Length; i++)
            oldPagesPos[i] = pages[i].pos;

        currentPage = index;

        if (currentPage == 1)
        {
            arenaSlugcatSelectPage.readyWarning = Arena.arenaClientSettings.ready && Arena.allowJoiningMidRound;
            Arena.arenaClientSettings.ready = Arena.arenaClientSettings.ready && !Arena.allowJoiningMidRound;
        }
        else
            Arena.arenaClientSettings.ready = Arena.arenaClientSettings.ready || arenaSlugcatSelectPage.readyWarning;

        PlaySound(SoundID.MENU_Next_Slugcat);
    }
    public void SwitchSelectedSlugcat(SlugcatStats.Name slugcat)
    {
        GetArenaSetup.playerClass[0] = slugcat;
        pendingScene = Arena.slugcatSelectMenuScenes.TryGetValue(slugcat.value, out MenuScene.SceneID newScene) ? newScene : GetScene;
    }
    public void SetTemporaryDescription(string desc, int overideDescForHowManyTicks) //how many ticks before it will no longer override UpdateInfoText
    {
        customTextDescription = desc;
        customTextDescriptionCounter = overideDescForHowManyTicks;
    }
    public void GoToChangeCharacter()
    {
        if (OnlineManager.lobby.isOwner && Arena.initiateLobbyCountdown) return;

        bool arenaMode = RainMeadow.isArenaMode(out _);
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (arenaMode && Arena.arenaClientSettings.ready)
            {
                PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                return;
            }
            var index = arenaSlugcatSelectPage.selectedSlugcatIndex;
            if (index == -1) index = 0;
            else
            {
                index += 1;
                index %= ArenaHelpers.selectableSlugcats.Count;
            }
            if (arenaMode)
            {
                int unbannedIndex = Arena.GetNewAvailableSlugcatIndex(index);
                if (unbannedIndex == arenaSlugcatSelectPage.selectedSlugcatIndex)
                {
                    PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
                    return;
                }
                index = unbannedIndex;
            }
            arenaSlugcatSelectPage.SwitchSelectedSlugcat(ArenaHelpers.selectableSlugcats[index]);
            PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            return;
        }
        MovePage(new Vector2(-1500f, 0f), 1);
        selectedObject = arenaSlugcatSelectPage.slugcatSelectButtons[0];
    }
    public void GoToSlugcatSelector()
    {
        PlaySound(SoundID.MENU_Next_Slugcat);
        SlugcatSelector selector = new(manager, [GetArenaSetup.playerClass[0], GetArenaSetup.playerClass[0], GetArenaSetup.playerClass[0]], [.. ArenaHelpers.selectableSlugcats],

            (slugcats, selector) =>
            {
                arenaSlugcatSelectPage.SwitchSelectedSlugcat(slugcats[UnityEngine.Random.Range(0, slugcats.Length)]);
                if (RainMeadow.isArenaMode(out _)) Arena.arenaClientSettings.gotSlugcat = selector.IsMatching;
            }
        );
        manager.ShowDialog(selector);
    }
    public void StartGame()
    {
        if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

        if (OnlineManager.lobby.isOwner)
        {
            if (Arena.lobbyCountDown > 0)
            {
                Arena.initiateLobbyCountdown = true;
                return;
            }
        }

        while (manager.dialog != null)
            manager.StopSideProcess(manager.dialog);
        Arena.OnStartGame(manager);
        Arena.InitializeSlugcat();
        InitializeNewOnlineSitting();
        ArenaHelpers.SetupOnlineArenaStting(Arena, manager);
        // temp
        // UserInput.SetUserCount(OnlineManager.players.Count);
        // UserInput.SetForceDisconnectControllers(forceDisconnect: false);
        PlaySound(SoundID.MENU_Start_New_Game);
        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        Arena.arenaClientSettings.ready = false;
    }
    public void SetPlaylistFromSetupToSitting()
    {
        if (GetGameTypeSetup.shufflePlaylist)
        {
            List<string> playlist = new(GetGameTypeSetup.playList);

            while (playlist.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, playlist.Count);
                for (int i = 0; i < GetGameTypeSetup.levelRepeats; i++)
                    manager.arenaSitting.levelPlaylist.Add(playlist[randomIndex]);
                playlist.RemoveAt(randomIndex);
            }
        }
        else
            for (int i = 0; i < GetGameTypeSetup.playList.Count; i++)
                for (int j = 0; j < GetGameTypeSetup.levelRepeats; j++)
                    manager.arenaSitting.levelPlaylist.Add(GetGameTypeSetup.playList[i]);
    }
    public void InitializeNewOnlineSitting()
    {
        manager.arenaSitting = new ArenaSitting(GetGameTypeSetup, new MultiplayerUnlocks(manager.rainWorld.progression, arenaMainLobbyPage.levelSelector.allLevels))
        { levelPlaylist = [] };

        // Host dictates playlist
        if (OnlineManager.lobby.isOwner)
        {
            SetPlaylistFromSetupToSitting();
            Arena.playList.Clear();
            Arena.playList.AddRange(manager.arenaSitting.levelPlaylist);
            Arena.arenaSittingOnlineOrder.AddRange(OnlineManager.players.Where(x => ArenaHelpers.GetArenaClientSettings(x)?.ready == true).Select(x => x.inLobbyId));
            Arena.totalLevelCount = manager.arenaSitting.levelPlaylist.Count;
        }
        else
        {
            manager.arenaSitting.levelPlaylist.AddRange(Arena.playList);
            manager.arenaSitting.currentLevel = Arena.currentLevel;
        }

        if (Arena.registeredGameModes.Keys.Contains(Arena.currentGameMode))
        {
            Arena.externalArenaGameMode = Arena.registeredGameModes.FirstOrDefault(kvp => kvp.Key == Arena.currentGameMode).Value;
            RainMeadow.Debug($"Playing GameMode: {Arena.externalArenaGameMode}");
        }
        else
        {
            RainMeadow.Error("Could not find game mode in list! Setting to FFA as a fallback");
            Arena.externalArenaGameMode = Arena.registeredGameModes.FirstOrDefault(kvp => kvp.Key == FFA.FFAMode.value).Value;
        }

        Arena.externalArenaGameMode.InitAsCustomGameType(GetGameTypeSetup);
    }
    public override void ShutDownProcess()
    {
        if (RainMeadow.isArenaMode(out _)) Arena.externalArenaGameMode?.OnUIShutDown(this);
        arenaMainLobbyPage.chatMenuBox.chatTypingBox.DelayedUnload(0.1f);
        ChatLogManager.Unsubscribe(arenaMainLobbyPage.chatMenuBox);
        if (OnlineManager.lobby?.isOwner == true)
        {
            GetArenaSetup.SaveToFile();
            arenaMainLobbyPage.SaveInterfaceOptions();
            RainMeadow.rainMeadowOptions._SaveConfigFile();
        }
        else (GetArenaSetup as ArenaOnlineSetup)?.SaveNonSessionToFile();
        base.ShutDownProcess();
        if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
        {
            OnlineManager.LeaveLobby();
            manager.arenaSetup = null;
        }

    }
    public override void Init()
    {
        base.Init();
        CreateAndUpdateElementBindings();
        selectedObject = arenaMainLobbyPage.readyButton;
    }
    public override void Update()
    {
        base.Update();

        if (!CanEscExit && RWInput.CheckPauseButton(0) && manager.dialog is null)
        {
            MovePage(new Vector2(1500f, 0f), 0);
            selectedObject = arenaMainLobbyPage.readyButton;
        }
        if (pendingScene == scene.sceneID) pendingScene = null;
        lastDesiredBgCoverAlpha = desiredBgCoverAlpha;
        desiredBgCoverAlpha = Mathf.Clamp(desiredBgCoverAlpha + ((pendingScene != null) ? 0.01f : -0.01f), 0.8f, 1.1f);
        if (pendingScene != null && menuDarkSprite.darkSprite.alpha >= 1) ChangeScene();
        if (pagesMoving) UpdateMovingPage();
        if (customTextDescriptionCounter <= 0) customTextDescription = "";
        else
        {
            customTextDescriptionCounter--;
            infoLabel.text = UpdateInfoText();
            if (!string.IsNullOrEmpty(infoLabel.text))
                infoLabelFade = 1;
        }
        UpdateOnlineUI();
        if (!RainMeadow.isArenaMode(out _)) return;
        if (Arena.currentLobbyOwner != OnlineManager.lobby.owner)
        {
            Arena.ResetOnReturnMenu(manager);
            Arena.currentLobbyOwner = OnlineManager.lobby.owner;
        }

        if (OnlineManager.lobby.isOwner)
        {
            if (Arena.lobbyCountDown <= 0 && !initiateStartGameAfterCountDown)
            {
                initiateStartGameAfterCountDown = true;
                StartGame();
            }
        }
        else
        {
            
            if (Arena.hasPermissionToRejoin && !initiateStartGameAfterCountDown && Arena.arenaClientSettings.ready)
            {
                initiateStartGameAfterCountDown = true;
                StartGame();
            }
        }

        if (!Arena.allowJoiningMidRound)
        {
            Arena.arenaClientSettings.ready = true;
        }

        if (Arena.initiateLobbyCountdown)
        {
            PlayStartGameCountdown();
        }
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        menuDarkSprite.darkSprite.alpha = Mathf.Clamp(Mathf.Lerp(lastDesiredBgCoverAlpha, desiredBgCoverAlpha, timeStacker), 0.8f, 1.1f);
    }
    public override string UpdateInfoText()
    {
        if (!string.IsNullOrEmpty(customTextDescription))
            return customTextDescription;
        if (selectedObject is SlugcatColorableButton colBtn)
        {
            string id = colBtn.signalText;
            if (colBtn.signalText == "CHANGE_SLUGCAT")
                return Translate("Go to Slugcat Selection Page");
        }
        if (selectedObject is CheckBox checkBox)
        {
            bool check = checkBox.Checked;
            string idString = checkBox.IDString;
            if (idString == "SPEARSHIT")
                return check ? Translate("Player vs player deathmatch") : Translate("Eating contest");
            if (idString == "EVILAI")
                return check ? Translate("Creatures are vicious and aggressive") : Translate("Normal Rain World AI");
            if (idString == "ITEMSTEAL")
                return check ? Translate("Players can steal items from each other") : Translate("Players cannot steal items from each other");
            if (idString == "MIDGAMEJOIN")
                return check ? Translate("Players can join each round") : Translate("Players can only join at the first round");
            if (idString == "WEAPONCOLLISIONFIX")
                return check ? Translate("Thrown weapons are corrected to prevent no-clips") : Translate("Thrown weapons follow vanilla behaviour");
            if (idString == "PIGGY")
                return check ? Translate("Players can piggyback each other") : Translate("Players cannot piggyback each other");
        }
        if (selectedObject is SimpleButton simpleBtn)
        {
            string id = simpleBtn.signalText;
            if (id == "CHANGE_SLUGCAT")
                return Translate("Go to Slugcat Selection Page");
            if (id == OnlineSlugcatAbilitiesInterface.BACKTOSELECT)
                return Translate("Return to Select Settings Page");
        }
        if (selectedObject is SymbolButton symbolBtn)
        {
            string id = symbolBtn.signalText;
            if (id == "MUTEPLAYER")
            {
                OnlinePlayer? profileIdentifier = (symbolBtn.owner as ArenaPlayerBox)?.profileIdentifier ?? (symbolBtn.owner as ArenaPlayerSmallBox)?.profileIdentifier;
                bool muted = profileIdentifier != null && OnlineManager.lobby?.gameMode?.mutedPlayers != null && OnlineManager.lobby.gameMode.mutedPlayers.Contains(profileIdentifier.id.name);
                return muted? Translate("Unmute player") : Translate("Mute player");
            }
            if (id == "KICKPLAYER")
                return Translate("Kick player from lobby");
            if (id == "COLOR_SLUGCAT")
                return Translate("Customize your colors");
        }
        if (selectedObject is SelectOneButton selectOneButton)
        {
            int index = selectOneButton.buttonArrayIndex;
            string idString = selectOneButton.signalText;
            if (idString == "scug select")
            {
                if (OnlineManager.lobby?.isOwner == true)
                    return Translate("Press grab to toggle active slugcats");
                else if (RainMeadow.isArenaMode(out _) && Arena.bannedSlugs.Contains(index))
                    return Translate("You aren't allowed to play as this slugcat");
            }
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
                return index < 0 || index >= ArenaSetup.GameTypeSetup.SessionTimesInMinutesArray.Length ? Translate("No rain") : ArenaSetup.GameTypeSetup.SessionTimesInMinutesArray[index] + " " + Translate($"minute{(index == 1 ? "" : "s")} until rain");
            if (idString == "WILDLIFE")
            {
                ArenaSetup.GameTypeSetup.WildLifeSetting settingFromBtn = new(ExtEnum<ArenaSetup.GameTypeSetup.WildLifeSetting>.values.GetEntry(index), false);
                string value = settingFromBtn == ArenaSetup.GameTypeSetup.WildLifeSetting.Off ? "No" : settingFromBtn.value;
                return Translate($"{value} wildlife");
            }
        }
        if (selectedObject is ButtonScroller.SideButton sideBtn)
        {
            string id = sideBtn.signalText;
            if (id == "THUMBS" && sideBtn.owner is PlaylistSelector playSelector)
                return Translate(playSelector.ShowThumbsStatus ? "Showing level thumbnails" : "Showing level names");
            if (id == "SHUFFLE" && sideBtn.owner is PlaylistHolder playHolder)
                return Translate(playHolder.ShuffleStatus ? "Playing levels in random order" : "Playing levels in selected order");
        }
        if (selectedObject is OnlineTeamBattleSettingsInterface.TeamButton teamBtn)  return Translate("Join Team \"<TEAMNAME>\"").Replace("<TEAMNAME>", teamBtn.teamName);
        if (selectedObject is OnlineSlugcatAbilitiesInterface.SelectSettingsPage.SettingsButton settingBtn)
            return Translate("Go to <SETTINGSNAME> Page").Replace("<SETTINGSNAME>", settingBtn.menuLabel.label.text); //menulabel text is already coded to be translated
        return selectedObject is IHaveADescription descObj ? descObj.Description : base.UpdateInfoText();
    }
    public void UpdateOnlineUI() //for future online ui stuff
    {
        if (!RainMeadow.isArenaMode(out _)) return;
        SlugcatStats.Name slugcat = GetArenaSetup.playerClass[0];
        Arena.arenaClientSettings.playingAs = slugcat;
        Arena.arenaClientSettings.selectingSlugcat = currentPage == 1;
        if (manager.upcomingProcess == null) Arena.arenaClientSettings.slugcatColor = manager.rainWorld.progression.IsCustomColorEnabled(slugcat) ? ColorHelpers.HSL2RGB(ColorHelpers.RWJollyPicRange(manager.rainWorld.progression.GetCustomColorHSL(slugcat, 0))) : Color.black;

        if (!(Arena.currentGameMode == Arena.externalArenaGameMode?.GetGameModeId?.value))
        {
            if (!Arena.registeredGameModes.TryGetValue(Arena.currentGameMode, out var extGameMode)) return;
            RemoveAndAddNewExtGameModeTab(extGameMode);
        }
        Arena.externalArenaGameMode?.OnUIUpdate(this);

    }
    public void UpdateMovingPage()
    {
        pageMovementProgress = Mathf.MoveTowards(pageMovementProgress, 1f, 1f / 35f);
        if (Mathf.Approximately(pageMovementProgress, 1f))
        {
            pageMovementProgress = 1f;
            pagesMoving = false;
        }

        for (int i = 0; i < pages.Count; i++)
        {
            var newpos = oldPagesPos[i] + newPagePos;
            pages[i].pos.x = Custom.LerpSinEaseInOut(oldPagesPos[i].x, newpos.x, pageMovementProgress);
        }
    }

    public void PlayStartGameCountdown()
    {
        if (Arena.lobbyCountDown != lastCountdownSoundPlayed &&
            (Arena.lobbyCountDown == 3 || Arena.lobbyCountDown == 2 || Arena.lobbyCountDown == 1))
        {
            PlaySound(SoundID.MENU_Player_Join_Game);
            lastCountdownSoundPlayed = Arena.lobbyCountDown;
        }
    }
    public void CreateAndUpdateElementBindings()
    {
        //Set up for and fix the match settings submenu. This is not exactly the cleanest-looking implementation, but it's the friendliest to modification.
        List<MenuObject> MatchSettingsRow1Elements = new List<MenuObject>() { arenaMainLobbyPage.arenaSettingsInterface.spearsHitCheckbox, arenaMainLobbyPage.arenaSettingsInterface.evilAICheckBox };
        List<MenuObject> MatchSettingsRow2Elements = arenaMainLobbyPage.arenaSettingsInterface.roomRepeatArray.buttons.Cast<MenuObject>().ToList();
        List<MenuObject> MatchSettingsRow3Elements = arenaMainLobbyPage.arenaSettingsInterface.rainTimerArray.buttons.Cast<MenuObject>().ToList();
        List<MenuObject> MatchSettingsRow4Elements = arenaMainLobbyPage.arenaSettingsInterface.wildlifeArray.buttons.Cast<MenuObject>().ToList();
        List<MenuObject> MatchSettingsRow5Elements = new List<MenuObject>() { arenaMainLobbyPage.arenaSettingsInterface.stealItemCheckBox, arenaMainLobbyPage.arenaSettingsInterface.allowMidGameJoinCheckbox };
        List<MenuObject> MatchSettingsRow6Elements = new List<MenuObject>() { arenaMainLobbyPage.arenaSettingsInterface.piggyBackCheckbox, arenaMainLobbyPage.arenaSettingsInterface.weaponCollisionCheckBox };
        List<MenuObject> MatchSettingsRow7Elements = new List<MenuObject>() { arenaMainLobbyPage.arenaSettingsInterface.countdownTimerTextBox.wrapper };
        List<MenuObject> MatchSettingsRow8Elements = new List<MenuObject>() { arenaMainLobbyPage.arenaSettingsInterface.arenaGameModeComboBox.wrapper };
        List<List<MenuObject>> MatchSettingsElementRowList = new List<List<MenuObject>>() { MatchSettingsRow1Elements, MatchSettingsRow2Elements, MatchSettingsRow3Elements, MatchSettingsRow4Elements, MatchSettingsRow5Elements, MatchSettingsRow6Elements, MatchSettingsRow7Elements, MatchSettingsRow8Elements };
        Extensions.TrySequentialParallelStitchBind(MatchSettingsElementRowList, areRows: true, loopLastIndex: true, reverseListList: true);

        UpdateElementBindings();
    }
    public void UpdateElementBindings()
    {
        //Enforce the bottom row's element order. Wow was this broken. TrySequentualMutualBind has a built-in per-entry null check, so if startButton doesn't exist, it will gracefully rebind around it.
        List<MenuObject> BottomRowElements = new List<MenuObject>() { backObject, arenaMainLobbyPage.startButton, arenaMainLobbyPage.readyButton, arenaMainLobbyPage.arenaGameStatsButton };
        Extensions.TrySequentialMutualBind(this, BottomRowElements, leftRight: true, loopLastIndex: true);
    }
    public void RemoveAndAddNewExtGameModeTab(ExternalArenaGameMode? gameMode)
    {
        if (gameMode == null) return;
        Arena.externalArenaGameMode?.OnUIDisabled(this);
        Arena.externalArenaGameMode = gameMode;
        Arena.externalArenaGameMode.OnUIEnabled(this);

    }
}
