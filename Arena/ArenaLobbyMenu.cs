using Kittehface.Framework20;
using Menu;
using Menu.Remix;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
namespace RainMeadow
{
    public class ArenaLobbyMenu : MultiplayerMenu
    {

        private MenuLabel totalClientsReadiedUpOnPage, currentLevelProgression, displayCurrentGameMode;
        public SimplerButton forceReady;
        private SimplerSymbolButton colorConfigButton;
        private ColorSlugcatDialog colorConfigDialog;
        private ArenaOnlineSlugcatButtons slugcatButtons;
        private Dictionary<string, bool> playersReadiedUp = [];
        private int currentColorIndex = 0;
        private string forceReadyText = "FORCE READY"; // for the button text, in case we need to reset it for any reason
        private bool flushArenaSittingForWaitingClients = false;
        private bool pushClientIntoGame = false;

        private ArenaOnlineGameMode arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
        private int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // been using 1360 as ref
        private int CurrentColorIndex
        {
            get
            {
                return currentColorIndex;
            }
            set
            {
                if (currentColorIndex != value)
                {
                    currentColorIndex = value;
                    CallLobbyDataChange(ArenaRPCs.Arena_NotifyClassChange, OnlineManager.mePlayer, currentColorIndex);
                }
            }
        }
        private SlugcatStats.Name SlugcatFromIndex => ArenaHelpers.selectableSlugcats[CurrentColorIndex];
        private List<OnlinePlayer> OtherOnlinePlayers => [.. OnlineManager.players?.Where(x => !(x?.isMe ?? false)) ?? []];
        public ArenaLobbyMenu(ProcessManager manager) : base(manager)
        {
            ID = OnlineManager.lobby.gameMode.MenuProcessId();
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null) throw new InvalidOperationException("lobby is null");
            if (OnlineManager.lobby.isOwner)
            {
                ArenaHelpers.ResetOnReturnToMenu(arena, this);
                arena.ResetForceReadyCountDown();
            }
            ArenaHelpers.ResetReadyUpLogic(arena, this);
            OverrideMultiplayerMenu();
            BindSettings();
            BuildLayout();

            MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
            if (arena.currentGameMode == "" || arena.currentGameMode is null)
            {
                arena.currentGameMode = Competitive.CompetitiveMode.value;
            }

        }
        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            manager.rainWorld.progression.SaveProgression(true, true);
            if (OnlineManager.lobby?.isOwner == true)
            {
                GetArenaSetup?.SaveToFile();
            }
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                OnlineManager.LeaveLobby();
            }
            base.ShutDownProcess();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (colorConfigButton != null)
            {
                if (SlugcatFromIndex != null) {
                    colorConfigButton.symbolSprite.alpha = this.manager.rainWorld.progression.IsCustomColorEnabled(SlugcatFromIndex) ? 1 : 0.2f;
                } else {
                    colorConfigButton.symbolSprite.alpha = 0.2f;
                }
                
            }
        }
        public override void Update()
        {
            base.Update();

            if (OnlineManager.lobby == null)
            {
                return;
            }
            if (OnlineManager.lobby.isOwner)
            {
                if (forceReady != null)
                {
                    if (arena.forceReadyCountdownTimer > 0)
                    {
                        forceReady.buttonBehav.greyedOut = true;
                        forceReady.menuLabel.text = forceReadyText + $" ({arena.forceReadyCountdownTimer})";
                    }
                    else
                    {
                        forceReady.menuLabel.text = forceReadyText;
                    }
                    if (arena.playersReadiedUp?.list != null && arena.forceReadyCountdownTimer <= 0)
                    {
                        forceReady.buttonBehav.greyedOut = OnlineManager.players.Count == arena.playersReadiedUp.list.Count;
                    }
                }
            }
            else
            {
                if (arena.isInGame && arena.arenaSittingOnlineOrder.Count == arena.playersReadiedUp.list.Count && !pushClientIntoGame && !arena.clientWantsToLeaveGame && !arena.playersLateWaitingInLobbyForNextRound.Contains(OnlineManager.mePlayer.inLobbyId) && arena.arenaSittingOnlineOrder.Contains(OnlineManager.mePlayer.inLobbyId))
                {
                    pushClientIntoGame = true;
                    this.StartGame();
                }
                if (arena.isInGame && !pushClientIntoGame && !arena.clientWantsToLeaveGame && arena.hasPermissionToRejoin)
                {
                    RainMeadow.Debug("Client was late but given permission to rejoin!");
                    pushClientIntoGame = true;
                    this.StartGame();
                }

            }
            UpdateSlugcatButtons();
            UpdateReadyUpLabel();
            UpdateLevelCounter();
            UpdateGameModeLabel();
            UpdatePlayButton();
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "BACKTOLOBBY")
            {
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                PlaySound(SoundID.MENU_Switch_Page_Out);
            }
            if (message == "STARTARENAONLINEGAME")
            {
                StartGame();
            }

            if (message == "INFO" && infoWindow != null)
            {
                infoWindow.label.text = Regex.Replace(Translate("Welcome to Arena Online!<LINE>All players must ready up to begin."), "<LINE>", "\r\n");
            }
            if (OnlineManager.lobby.isOwner)
            {
                if (message == "NEXTONLINEGAME")
                {
                    var gameModesList = arena.registeredGameModes.ToList();

                    // Find the current game mode entry
                    var currentModeIndex = gameModesList.FindIndex(kvp => kvp.Value == arena.currentGameMode);

                    // Get the next mode in the list, or wrap around to the first mode if at the end
                    var nextModeIndex = (currentModeIndex + 1) % gameModesList.Count;

                    // Update the current game mode
                    arena.onlineArenaGameMode = gameModesList[nextModeIndex].Key;
                    arena.currentGameMode = gameModesList[nextModeIndex].Value;

                }

                if (message == "PREVONLINEGAME")
                {
                    var gameModesList = arena.registeredGameModes.ToList();

                    // Find the current game mode entry
                    int currentModeIndex = gameModesList.FindIndex(kvp => kvp.Value == arena.currentGameMode);

                    // Handle the case when we're at the beginning of the list
                    if (currentModeIndex > 0)
                    {
                        // Get the previous mode in the list
                        int prevModeIndex = currentModeIndex - 1;
                        arena.onlineArenaGameMode = gameModesList[prevModeIndex].Key;
                        arena.currentGameMode = gameModesList[prevModeIndex].Value;

                        // Initialize the custom game type
                    }
                    else
                    {
                        // Handle the case when we're already at the beginning
                        // You might want to wrap around to the last mode here
                        arena.onlineArenaGameMode = gameModesList[gameModesList.Count - 1].Key;
                        arena.currentGameMode = gameModesList[gameModesList.Count - 1].Value;

                        // Initialize the custom game type
                    }

                }

            }
        }
        private void RemoveExcessArenaObjects()
        {
            if (OnlineManager.lobby.isOwner)
            {
                arena.playList.Clear();
            }
            pages[0].ClearMenuObjectIList(playerClassButtons);
            pages[0].ClearMenuObjectIList(playerJoinButtons);
            pages[0].ClearMenuObject(resumeButton);
            if (levelSelector != null && levelSelector.levelsPlaylist != null)
            {
                if (!OnlineManager.lobby.isOwner)
                {
                    for (int i = levelSelector.levelsPlaylist.levelItems.Count - 1; i >= 0; i--)
                    {
                        GetGameTypeSetup.playList.RemoveAt(GetGameTypeSetup.playList.Count - 1);
                        levelSelector.levelsPlaylist.RemoveLevelItem(new LevelSelector.LevelItem(this, levelSelector.levelsPlaylist, this.levelSelector.levelsPlaylist.levelItems[i].name));
                        levelSelector.levelsPlaylist.ScrollPos = levelSelector.levelsPlaylist.LastPossibleScroll;
                        levelSelector.levelsPlaylist.ConstrainScroll();
                    }
                }
                else
                {
                    arena.playList = GetGameTypeSetup.playList;
                }
            }
        }
        private void OverrideMultiplayerMenu()
        {

            RemoveExcessArenaObjects();

            this.currentGameType = this.nextGameType = ArenaSetup.GameTypeID.Competitive;
            this.nextButton.signalText = "NEXTONLINEGAME";
            this.prevButton.signalText = "PREVONLINEGAME";

            this.backButton.signalText = "BACKTOLOBBY";
            this.playButton.signalText = "STARTARENAONLINEGAME";
        }
        private void BuildLayout()
        {
            BuildPlayerButtons();
            AddAbovePlayText();
            if (OnlineManager.lobby.isOwner)
            {
                AddForceReadyUp();
            }
            if (levelSelector != null && levelSelector.levelsPlaylist != null)
            {

                if (!OnlineManager.lobby.isOwner)
                {
                    foreach (var level in arena.playList)
                    {
                        this.GetGameTypeSetup.playList.Add(level);
                        this.levelSelector.levelsPlaylist.AddLevelItem(new Menu.LevelSelector.LevelItem(this, this.levelSelector.levelsPlaylist, level));
                        this.levelSelector.levelsPlaylist.ScrollPos = this.levelSelector.levelsPlaylist.LastPossibleScroll;
                        this.levelSelector.levelsPlaylist.ConstrainScroll();
                    }
                }

            }

            if (slugcatButtons.meButton != null) //shouldnt be null
            {
                displayCurrentGameMode = new MenuLabel(this, pages[0], Translate($"Current Mode:") + " " + Utils.Translate(arena.currentGameMode ?? ""), slugcatButtons.pos + slugcatButtons.meButton.pos + new Vector2(0, 150), new Vector2(10f, 10f), true);
                displayCurrentGameMode.label.alignment = FLabelAlignment.Left;
                // Ready up label
                totalClientsReadiedUpOnPage = new MenuLabel(this, pages[0], Translate($"Ready:") + " " + arena.playersReadiedUp.list.Count + "/" + OnlineManager.players.Count, new Vector2(displayCurrentGameMode.pos.x, displayCurrentGameMode.pos.y - 25), new Vector2(10f, 10f), false);
                totalClientsReadiedUpOnPage.label.alignment = FLabelAlignment.Left;

                currentLevelProgression = new MenuLabel(this, pages[0], Translate($"Playlist Progress:") + " " + arena.currentLevel + "/" + arena.totalLevelCount, new Vector2(displayCurrentGameMode.pos.x, displayCurrentGameMode.pos.y - 40), new Vector2(10f, 10f), false);
                currentLevelProgression.label.alignment = FLabelAlignment.Left;

                pages[0].subObjects.AddRange([displayCurrentGameMode, totalClientsReadiedUpOnPage, currentLevelProgression]);

                if (ModManager.MMF)
                {
                    colorConfigButton = new(this, pages[0], "Kill_Slugcat", "", slugcatButtons.pos + slugcatButtons.meButton.usernameButton.pos + new Vector2(-44, 0f));
                    colorConfigButton.OnClick += (_) =>
                    {
                        if (SlugcatFromIndex == null) return;
                        colorConfigDialog = new(manager, SlugcatFromIndex, () => { });
                        manager.ShowDialog(colorConfigDialog);
                    };
                    pages[0].subObjects.Add(colorConfigButton);
                    MutualHorizontalButtonBind(colorConfigButton, slugcatButtons.meButton.usernameButton);
                }
            }

        }
        private void BuildPlayerButtons()
        {
            if (slugcatButtons == null)
            {
                slugcatButtons = new(this, pages[0], new(706, 440), OtherOnlinePlayers, UpdateProfilePlayerButtons);
                pages[0].subObjects.Add(slugcatButtons);
                if (arena.playersInLobbyChoosingSlugs?.TryGetValue(OnlineManager.mePlayer.GetUniqueID(), out int existingValue) == true)
                {
                    RainMeadow.Debug("Player already exists in dictionary");
                    RainMeadow.Debug("Current index" + existingValue.ToString());
                    CurrentColorIndex = existingValue;
                    slugcatButtons.meButton!.SetNewSlugcat(SlugcatFromIndex, existingValue, ArenaImage);
                    (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs = SlugcatFromIndex;
                }
                else
                {
                    RainMeadow.Debug("Player did NOT exist in dictionary");
                    CurrentColorIndex = 0;

                }
                slugcatButtons.meButton!.OnClick += (arenaSlugcatButton) =>
                {
                    CurrentColorIndex = (CurrentColorIndex + 1) % ArenaHelpers.selectableSlugcats.Count;
                    slugcatButtons.meButton!.SetNewSlugcat(SlugcatFromIndex, CurrentColorIndex, ArenaImage);
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    RainMeadow.Debug($"My ID: {OnlineManager.mePlayer.GetUniqueID()}");
                    (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs = SlugcatFromIndex;
                    RainMeadow.Debug($"My Slugcat: {OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>().playingAs}");


                };
            }
        }
        private void UpdateProfilePlayerButtons(ArenaOnlineSlugcatButtons slugcatPlayerButtons)
        {
            for (int i = 0; i < slugcatPlayerButtons.otherArenaPlayerButtons?.Length; i++)
            {
                ArenaOnlinePlayerJoinButton playerButton = slugcatPlayerButtons.otherArenaPlayerButtons[i];
                playerButton.portraitBlack = Custom.LerpAndTick(playerButton.portraitBlack, 1f, 0.06f, 0.05f);

            }
        }
        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            if (RainMeadow.isArenaMode(out var arena)) //apparently null lobby can happen, so need!
            {
                BuildPlayerButtons();
                List<OnlinePlayer> otherOnlinePlayers = OtherOnlinePlayers;
                RainMeadow.Debug($"Player count without me: {otherOnlinePlayers.Count}");
                slugcatButtons.otherOnlinePlayers = otherOnlinePlayers;
                slugcatButtons.PopulatePage(slugcatButtons.CurrentOffset);
                if (OnlineManager.lobby?.isOwner == true)
                {
                    arena.ResetForceReadyCountDownShort();
                    if (arena.playersReadiedUp?.list != null)
                    {
                        var onlinePlayerIds = OnlineManager.players.Select(x => x.id).ToHashSet();

                        arena.playersReadiedUp.list.RemoveAll(player => !onlinePlayerIds.Contains(player));
                    }

                }
                if (this != null)
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this);
                }
            }


        }
        private void BindSettings()
        {
            arena.avatarSettings.eyeColor = RainMeadow.rainMeadowOptions.EyeColor.Value;
            arena.avatarSettings.bodyColor = RainMeadow.rainMeadowOptions.BodyColor.Value;
            arena.avatarSettings.playingAs = RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat;
        }
        private SimplerButton CreateButton(string text, Vector2 pos, Vector2 size, Action<SimplerButton>? clicked = null, Page? page = null)
        {
            page ??= pages[0];
            var b = new SimplerButton(this, page, text, pos, size);
            if (clicked != null) b.OnClick += clicked;
            page.subObjects.Add(b);
            return b;
        }
        private void AddAbovePlayText()
        {
            this.abovePlayButtonLabel = new MenuLabel(this, pages[0], "", this.playButton.pos + new Vector2((0f - this.playButton.size.x) / 2f + 0.01f, 50.01f), new Vector2(this.playButton.size.x, 20f), bigText: false);
            this.abovePlayButtonLabel.label.alignment = FLabelAlignment.Left;
            this.abovePlayButtonLabel.label.color = MenuRGB(MenuColors.DarkGrey);
            pages[0].subObjects.Add(this.abovePlayButtonLabel);
            if (manager.rainWorld.options.ScreenSize.x < 1280f)
            {
                this.abovePlayButtonLabel.label.alignment = FLabelAlignment.Right;
                this.abovePlayButtonLabel.pos.x = this.playButton.pos.x + 55f;
            }
        }
        private void InitializeNewOnlineSitting()
        {
            manager.arenaSitting = new ArenaSitting(this.GetGameTypeSetup, this.multiplayerUnlocks);

            manager.arenaSitting.levelPlaylist = new List<string>();

            if (this.GetGameTypeSetup.shufflePlaylist)
            {
                List<string> list2 = new List<string>();
                for (int l = 0; l < this.GetGameTypeSetup.playList.Count; l++)
                {
                    list2.Add(this.GetGameTypeSetup.playList[l]);
                }

                while (list2.Count > 0)
                {
                    int index2 = UnityEngine.Random.Range(0, list2.Count);
                    for (int m = 0; m < this.GetGameTypeSetup.levelRepeats; m++)
                    {
                        manager.arenaSitting.levelPlaylist.Add(list2[index2]);
                    }

                    list2.RemoveAt(index2);
                }
            }
            else
            {
                for (int n = 0; n < this.GetGameTypeSetup.playList.Count; n++)
                {
                    for (int num = 0; num < this.GetGameTypeSetup.levelRepeats; num++)
                    {
                        manager.arenaSitting.levelPlaylist.Add(this.GetGameTypeSetup.playList[n]);
                    }
                }
            }

            // Host dictates playlist

            if (OnlineManager.lobby.isOwner)
            {
                arena.playList = manager.arenaSitting.levelPlaylist;


                for (int i = 0; i < OnlineManager.players.Count; i++)
                {
                    if (!arena.arenaSittingOnlineOrder.Contains(OnlineManager.players[i].inLobbyId))
                    {
                        arena.arenaSittingOnlineOrder.Add(OnlineManager.players[i].inLobbyId);
                    }

                }
                arena.totalLevelCount = manager.arenaSitting.levelPlaylist.Count;

            }

            // Client retrieves playlist
            else
            {
                manager.arenaSitting.levelPlaylist = arena.playList;
                manager.arenaSitting.currentLevel = arena.currentLevel;

            }

            ArenaHelpers.SetProfileColor(arena);
            if (arena.registeredGameModes.Values.Contains(arena.currentGameMode))
            {
                arena.onlineArenaGameMode = arena.registeredGameModes.FirstOrDefault(kvp => kvp.Value == arena.currentGameMode).Key;
                RainMeadow.Debug("Playing GameMode: " + arena.onlineArenaGameMode);
            }
            else
            {
                RainMeadow.Error("Could not find gamemode in list! Setting to Competitive as a fallback");
                arena.onlineArenaGameMode = arena.registeredGameModes.FirstOrDefault(kvp => kvp.Value == Competitive.CompetitiveMode.value).Key;
            }
            arena.onlineArenaGameMode.InitAsCustomGameType(this.GetGameTypeSetup);

        }
        public void StartGame()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive)
            {
                return;
            }


            if (OnlineManager.lobby.isOwner && this.GetGameTypeSetup.playList != null && this.GetGameTypeSetup.playList.Count == 0)
            {
                return; // don't be foolish
            }


            // Players Who Arrive On Time 
            if (!arena.allPlayersReadyLockLobby && !arena.isInGame)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    if (!arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))
                    {
                        arena.playersReadiedUp.list.Add(OnlineManager.mePlayer.id);
                    }
                }
                else
                {

                    if (OnlineManager.players.Count > 1 && !arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))
                    {
                        RainMeadow.Debug("Arena: Notifying host I'm ready to go");

                        OnlineManager.lobby.owner.InvokeRPC(ArenaRPCs.Arena_NotifyLobbyReadyUp, OnlineManager.mePlayer);
                        this.playButton.menuLabel.text = this.Translate("Waiting for others...");
                        this.playButton.inactive = true;
                        this.playButton.buttonBehav.greyedOut = true;
                    }
                }
                return;
            }

            // Players Who Didn't
            if (!OnlineManager.lobby.isOwner)
            {
                if (!arena.isInGame)
                {
                    RainMeadow.Debug("Host is not in game");
                    return;
                }
                else
                {

                    if (!arena.playersLateWaitingInLobbyForNextRound.Contains(OnlineManager.mePlayer.inLobbyId)) // lobby's locked up, you don't have permission to rejoin, you haven't asked to be queued
                    {
                        if (arena.arenaSittingOnlineOrder.Contains(OnlineManager.mePlayer.inLobbyId)) // normal. You should be removed when you quit back to lobby or on next level if you're missing
                        {
                            // noop
                        }
                        else
                        {
                            RainMeadow.Debug("Arena: Notifying host I'm late");
                            OnlineManager.lobby.owner.InvokeRPC(ArenaRPCs.Arena_AddPlayerWaiting, OnlineManager.mePlayer);
                            this.playButton.inactive = true;
                            this.playButton.buttonBehav.greyedOut = true;
                            return;
                        }
                    }
                    //if (arena.playersLateWaitingInLobbyForNextRound.Contains(OnlineManager.mePlayer.inLobbyId) && !arena.arenaSittingOnlineOrder.Contains(OnlineManager.mePlayer.inLobbyId))
                    //{
                    //    RainMeadow.Debug("Arena: You've let the host know you're ready, but they're not ready for you");
                    //    return;
                    //}

                    // there's still a chance someone is queued and they're not ready by host yet
                    if (!arena.hasPermissionToRejoin && arena.playersLateWaitingInLobbyForNextRound.Contains(OnlineManager.mePlayer.inLobbyId))
                    {
                        RainMeadow.Debug("Arena: You've let the host know you're ready, they've acknowled the request, but the time is not right");
                        return;
                    }


                    arena.clientWantsToLeaveGame = false;
                }
            }
            if (colorConfigDialog != null)
            {
                manager.StopSideProcess(colorConfigDialog); //force getting rid of dialog
            }
            arena.arenaClientSettings.playingAs = SlugcatFromIndex;
            arena.InitializeSlugcat();
            InitializeNewOnlineSitting();
            ArenaHelpers.SetupOnlineArenaStting(arena, this.manager);
            this.manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            // temp
            UserInput.SetUserCount(OnlineManager.players.Count);
            UserInput.SetForceDisconnectControllers(forceDisconnect: false);
            this.PlaySound(SoundID.MENU_Start_New_Game);
            this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }
        private void UpdateReadyUpLabel()
        {
            if (totalClientsReadiedUpOnPage != null)
            {
                totalClientsReadiedUpOnPage.text = Translate("Ready:") + " " + arena.playersReadiedUp.list.Count + "/" + OnlineManager.players.Count;
            }
        }
        private void UpdateLevelCounter()
        {
            if (currentLevelProgression != null)
            {
                currentLevelProgression.text = Translate("Playlist Progress:") + " " + arena.currentLevel + "/" + arena.totalLevelCount;
            }
        }
        private void UpdateGameModeLabel()
        {
            if (displayCurrentGameMode != null)
            {
                displayCurrentGameMode.text = Translate("Current Mode:") + " " + Translate(arena.currentGameMode);
            }

        }
        private void UpdatePlayButton()
        {
            if (playButton == null)
            {
                return;
            }
            if (arena.playersReadiedUp?.list?.Count == 0 && arena.returnToLobby)
            {
                playButton.menuLabel.text = Translate("READY?");
                playButton.inactive = false;
            }
            if (OnlineManager.players.Count == 1)
            {
                playButton.menuLabel.text = Translate("WAIT FOR OTHERS");

            }
            if (GetGameTypeSetup.playList.Count == 0 && OnlineManager.lobby.isOwner)
            {
                playButton.buttonBehav.greyedOut = true;
            }
            if (GetGameTypeSetup.playList.Count * GetGameTypeSetup.levelRepeats >= 0)
            {
                pages[0].ClearMenuObject(abovePlayButtonLabel);
                if (!OnlineManager.lobby.isOwner) // let clients ready up when no map is selected
                {
                    playButton.buttonBehav.greyedOut = false;
                }
            }
            if (arena.playersReadiedUp?.list?.Contains(OnlineManager.mePlayer.id) == true && OnlineManager.players.Count > 1)
            {
                playButton.inactive = true;
            }
            if (arena.playersReadiedUp?.list?.Count == OnlineManager.players.Count)
            {
                arena.allPlayersReadyLockLobby = true;
                if (OnlineManager.players.Count == 1)
                {
                    playButton.menuLabel.text = Translate("LOBBY WILL LOCK"); // are you sure you want to enter host? Nobody can join you

                }
                else if (OnlineManager.lobby.isOwner)
                {
                    playButton.menuLabel.text = Translate("ENTER");
                    playButton.inactive = false;
                }
            }
            // clients
            if (!OnlineManager.lobby.isOwner)
            {
                if (arena.isInGame)
                {
                    if ((arena.playersLateWaitingInLobbyForNextRound.Contains(OnlineManager.mePlayer.inLobbyId)))
                    {
                        playButton.menuLabel.text = Translate("QUEUED TO JOIN");
                        playButton.inactive = true;
                    }
                    else
                    {
                        playButton.menuLabel.text = Translate("JOIN?");
                        playButton.inactive = false;
                    }

                }
                else
                {
                    if (!(arena.playersReadiedUp?.list?.Contains(OnlineManager.mePlayer.id) == true))
                    {
                        playButton.menuLabel.text = Translate("READY?");
                        playButton.inactive = false;
                    }
                    if (arena.playersReadiedUp?.list?.Contains(OnlineManager.mePlayer.id) == true && arena.playersReadiedUp.list.Count != OnlineManager.players.Count)
                    {
                        playButton.menuLabel.text = Translate("Waiting for others...");
                        playButton.inactive = true;
                    }

                    if (arena.playersReadiedUp?.list?.Contains(OnlineManager.mePlayer.id) == true && arena.playersReadiedUp.list.Count == OnlineManager.players.Count)
                    {
                        playButton.menuLabel.text = Translate("Waiting for host...");
                        playButton.inactive = true;
                    }
                }
            }
            if (arena.returnToLobby && !flushArenaSittingForWaitingClients) // coming back to lobby, reset everything
            {
                ArenaHelpers.ResetReadyUpLogic(arena, this);
                flushArenaSittingForWaitingClients = true;
            }
        }
        private void UpdateSlugcatButtons() //called under update, so lobby null check isnt needed
        {
            if (slugcatButtons?.otherArenaPlayerButtons != null)
            {
                foreach (ArenaOnlinePlayerJoinButton playerButton in slugcatButtons.otherArenaPlayerButtons)
                {
                    playerButton.readyForCombat = arena.playersReadiedUp?.list?.Contains(playerButton.profileIdentifier.id) == true;
                    playerButton.buttonBehav.greyedOut = !(arena.reigningChamps?.list?.Contains(playerButton.profileIdentifier.id) == true);
                    int colorIndex = arena.playersInLobbyChoosingSlugs?.TryGetValue(playerButton.profileIdentifier.GetUniqueID(), out int result) == true ? result : 0;
                    playerButton.SetNewSlugcat(ArenaHelpers.selectableSlugcats[colorIndex], colorIndex, ArenaImage);
                }
            }
        }
        private void AddForceReadyUp()
        {

            Action<SimplerButton> forceReadyClick = (_) =>
            {
                for (int i = 0; i < OnlineManager.players.Count; i++)
                {
                    var player = OnlineManager.players[i];
                    if (player.isMe)
                    {
                        this.playButton.Clicked();
                        continue;
                    }
                    if (!arena.playersReadiedUp.list.Contains(player.id))
                    {
                        player.InvokeOnceRPC(ArenaRPCs.Arena_ForceReadyUp);

                    }
                }
                arena.ResetForceReadyCountDownShort();
            };
            this.forceReady = CreateButton(this.Translate(forceReadyText), new Vector2(this.playButton.pos.x - 130f, this.playButton.pos.y), this.playButton.size, forceReadyClick);
        }
        public void CallLobbyDataChange(Delegate del, params object[] args) //prevent unnecessary rpc is called as owner, else call rpc
        {
            if (OnlineManager.lobby == null)
            {
                return;
            }
            if (OnlineManager.lobby.isOwner)
            {
                del.DynamicInvoke(args);
                return;
            }
            OnlineManager.lobby.owner.InvokeOnceRPC(del, args);
        }

    }
}
