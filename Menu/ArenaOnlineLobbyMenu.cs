using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RainMeadow.UI.Components;
using RainMeadow.UI.Pages;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI;

public class ArenaOnlineLobbyMenu : SmartMenu
{
    public static string[] PainCatNames => ["Inv", "Enot", "Paincat", "Sofanthiel", "Gorbo"]; // not using "???" cause it might cause some confusion to players who don't know Inv
    public SimplerButton playButton, slugcatSelectBackButton;
    public ArenaLevelSelector levelSelector;
    public OnlineArenaSettingsInferface arenaSettingsInterface;
    public OnlineSlugcatAbilitiesInterface? slugcatAbilitiesInterface;
    public MenuLabel slugcatNameLabel, slugcatDescriptionLabel;
    public ArenaMainLobbyPage arenaMainLobbyPage;
    public ArenaSlugcatSelectPage arenaSlugcatSelectPage;
    public Vector2 newPagePos = Vector2.zero;
    public Vector2[] oldPagesPos = [];
    public MenuIllustration competitiveTitle, competitiveShadow;
    public MenuScene.SceneID slugcatScene;
    public Page slugcatSelectPage;
    public bool pagesMoving = false, pageFullyTransitioned = true, pendingBgChange = false;
    public float pageMovementProgress = 0, desiredBgCoverAlpha = 0, lastDesiredBgCoverAlpha = 0;
    public string painCatName;
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
        if (Arena.myArenaSetup == null) manager.arenaSetup = Arena.myArenaSetup = new ArenaOnlineSetup(manager); //loading it on game mode ctor loads the base setup prob due to lobby still being null
        Futile.atlasManager.LoadAtlas("illustrations/arena_ui_elements");

        Arena.AddExternalGameModes(new Competitive(), Competitive.CompetitiveMode);

        if (Arena.currentGameMode == "" || Arena.currentGameMode == null)
            Arena.currentGameMode = Competitive.CompetitiveMode.value;

        pages.Add(slugcatSelectPage = new Page(this, null, "slugcat select", 1));
        slugcatSelectPage.pos.x += 1500f;
        ChangeScene(slugcatScene = Arena.slugcatSelectMenuScenes[Arena.arenaClientSettings.playingAs.value]);
        competitiveShadow = new MenuIllustration(this, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), true, false);
        competitiveTitle = new MenuIllustration(this, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), true, false);
        competitiveTitle.sprite.shader = manager.rainWorld.Shaders["MenuText"];

        painCatName = Arena.slugcatSelectPainCatNames[UnityEngine.Random.Range(0, Arena.slugcatSelectPainCatNames.Count)];

        arenaMainLobbyPage = new ArenaMainLobbyPage(this, mainPage, default, painCatName);
        arenaSlugcatSelectPage = new ArenaSlugcatSelectPage(this, slugcatSelectPage, default, painCatName);
        ChatLogManager.Subscribe(arenaMainLobbyPage.chatMenuBox);
        mainPage.SafeAddSubobjects(competitiveShadow, competitiveTitle, arenaMainLobbyPage);
        slugcatSelectPage.SafeAddSubobjects(arenaSlugcatSelectPage);
        
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
        slugcat = ArenaHelpers.selectableSlugcats.IndexOf(slugcat) == -1? ArenaHelpers.selectableSlugcats[0] : slugcat;
        slugcatScene = Arena.slugcatSelectMenuScenes[slugcat.value];
        Arena.arenaClientSettings.playingAs = slugcat;
        GetArenaSetup.playerClass[0] = slugcat;
        RainMeadow.Debug($"My Slugcat: {Arena.arenaClientSettings.playingAs}, in lobby list of client settings: {(ArenaHelpers.GetArenaClientSettings(OnlineManager.mePlayer)?.playingAs?.value) ?? "NULL!"}");
    }
    public override void ShutDownProcess()
    {
        arenaMainLobbyPage.chatMenuBox.chatTypingBox.DelayedUnload(0.1f);
        ChatLogManager.Unsubscribe(arenaMainLobbyPage.chatMenuBox);
        if (OnlineManager.lobby?.isOwner == true)
        {
            arenaMainLobbyPage.SaveInterfaceOptions();
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
    }
    public override string UpdateInfoText()
    {
        if (selectedObject is CheckBox checkBox)
        {
            string idString = checkBox.IDString;
            if (idString == "SPEARSHIT")
                return arenaMainLobbyPage.arenaSettingsInterface.GetGameTypeSetup.spearsHitPlayers ? Translate("Player vs player deathmatch") : Translate("Eating contest");
            if (idString == "EVILAI")
                return arenaMainLobbyPage.arenaSettingsInterface.GetGameTypeSetup.evilAI ? Translate("Creatures are vicious and aggressive") : Translate("Normal Rain World AI");
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
        Arena.arenaClientSettings.slugcatColor = this.manager.rainWorld.progression.IsCustomColorEnabled(slugcat)? ColorHelpers.HSL2RGB(ColorHelpers.RWJollyPicRange(this.manager.rainWorld.progression.GetCustomColorHSL(slugcat, 0))) : Color.black;


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
}
