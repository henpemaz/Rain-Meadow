using Kittehface.Framework20;
using Menu;
using Menu.Remix;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
using System.Security.Cryptography;
using HarmonyLib;
using System.Numerics;
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
        private int currentColorIndex = -1;
        private string forceReadyText = "FORCE READY"; // for the button text, in case we need to reset it for any reason
        private bool flushArenaSittingForWaitingClients = false;

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
        private SlugcatStats.Name SlugcatFromIndex => ArenaHelpers.allSlugcats[Mathf.Max(CurrentColorIndex, 0)];
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
                colorConfigButton.symbolSprite.alpha = this.IsCustomColorEnabled(SlugcatFromIndex) ? 1 : 0.2f;
            }
        }
        public override void Update()
        {
            base.Update();

            if (OnlineManager.lobby == null) return;

            if (OnlineManager.lobby.isOwner)
            {
                if (this.forceReady != null)
                {
                    if (arena.forceReadyCountdownTimer > 0)
                    {
                        this.forceReady.buttonBehav.greyedOut = true;
                        this.forceReady.menuLabel.text = forceReadyText + $" ({arena.forceReadyCountdownTimer})";
                    }
                    else
                    {
                        this.forceReady.menuLabel.text = forceReadyText;
                    }

                    if (arena.playersReadiedUp != null && arena.playersReadiedUp.list != null && arena.forceReadyCountdownTimer <= 0)
                    {
                        this.forceReady.buttonBehav.greyedOut = OnlineManager.players.Count == arena.playersReadiedUp.list.Count;
                    }
                }


            }
            if (slugcatButtons?.otherArenaPlayerButtons != null)
            {
                foreach (ArenaOnlinePlayerJoinButton playerButton in slugcatButtons.otherArenaPlayerButtons)
                {
                    int colorIndex = arena.playersInLobbyChoosingSlugs.TryGetValue(playerButton.profileIdentifier.GetUniqueID(), out int result) ? result : 0;
                    playerButton.SetNewSlugcat(ArenaHelpers.allSlugcats[colorIndex], colorIndex, ArenaImage);
                }
            }

            if (totalClientsReadiedUpOnPage != null)
            {
                UpdateReadyUpLabel();
            }

            if (currentLevelProgression != null)
            {
                UpdateLevelCounter();
            }

            if (displayCurrentGameMode != null)
            {
                UpdateGameModeLabel();

            }

            if (playButton != null)
            {

                if (arena.playersReadiedUp.list.Count == 0 && arena.returnToLobby)
                {
                    playButton.menuLabel.text = Translate("READY?");
                    playButton.inactive = false;
                }

                if (OnlineManager.players.Count == 1)
                {
                    this.playButton.menuLabel.text = this.Translate("WAIT FOR OTHERS");

                }

                if (this.GetGameTypeSetup.playList.Count == 0 && OnlineManager.lobby.isOwner)
                {
                    this.playButton.buttonBehav.greyedOut = true;
                }

                if (this.GetGameTypeSetup.playList.Count * this.GetGameTypeSetup.levelRepeats >= 0)
                {
                    if (this.abovePlayButtonLabel != null)
                    {
                        this.abovePlayButtonLabel.RemoveSprites();
                        this.pages[0].RemoveSubObject(this.abovePlayButtonLabel);
                    }
                    if (!OnlineManager.lobby.isOwner) // let clients ready up when no map is selected
                    {
                        playButton.buttonBehav.greyedOut = false;
                    }
                }


                if (arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id) && OnlineManager.players.Count > 1)
                {
                    playButton.inactive = true;
                }

                if (arena.playersReadiedUp.list.Count == OnlineManager.players.Count)
                {
                    arena.allPlayersReadyLockLobby = true;

                    if (OnlineManager.players.Count == 1)
                    {
                        playButton.menuLabel.text = Translate("LOBBY WILL LOCK"); // are you sure you want to enter host? Nobody can join you

                    }
                    else
                    {
                        if (OnlineManager.lobby.isOwner)
                        {
                            playButton.menuLabel.text = Translate("ENTER");
                            playButton.inactive = false;

                        }
                    }
                }

                // clients
                if (!OnlineManager.lobby.isOwner)
                {
                    if (arena.isInGame)
                    {
                        playButton.inactive = true;
                        if (!arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id)) // you're late
                        {
                            playButton.menuLabel.text = Translate("GAME IN SESSION");

                        }
                    }
                    if (!arena.isInGame && !arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))
                    {
                        playButton.menuLabel.text = Translate("READY?");
                        playButton.inactive = false;
                    }
                    if (!arena.isInGame && arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id) && arena.playersReadiedUp.list.Count != OnlineManager.players.Count)
                    {
                        playButton.menuLabel.text = Translate("Waiting for others...");
                        playButton.inactive = true;
                    }

                    if (!arena.isInGame && arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id) && arena.playersReadiedUp.list.Count == OnlineManager.players.Count)
                    {
                        playButton.menuLabel.text = Translate("Waiting for host...");
                        playButton.inactive = true;
                    }
                }

                if (arena.returnToLobby && !flushArenaSittingForWaitingClients) // coming back to lobby, reset everything
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this);
                    flushArenaSittingForWaitingClients = true;
                }
            }


        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "BACKTOLOBBY")
            {
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                base.PlaySound(SoundID.MENU_Switch_Page_Out);
            }
            if (message == "STARTARENAONLINEGAME")
            {
                StartGame();
            }

            if (message == "INFO" && infoWindow != null)
            {
                infoWindow.label.text = Regex.Replace(this.Translate("Welcome to Arena Online!<LINE>All players must ready up to begin."), "<LINE>", "\r\n");
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
            if (this.playerClassButtons != null && this.playerClassButtons.Length > 0)
            {
                for (int i = this.playerClassButtons.Length - 1; i >= 0; i--)
                {
                    this.playerClassButtons[i].RemoveSprites();
                    this.pages[0].RecursiveRemoveSelectables(playerClassButtons[i]);
                }
            }

            if (this.playerJoinButtons != null && this.playerJoinButtons.Length > 0)
            {
                for (int i = this.playerJoinButtons.Length - 1; i >= 0; i--)
                {
                    this.playerJoinButtons[i].RemoveSprites();
                    this.pages[0].RecursiveRemoveSelectables(playerJoinButtons[i]);


                }
            }
            if (this.resumeButton != null)
            {
                this.resumeButton.RemoveSprites();
                this.pages[0].RecursiveRemoveSelectables(this.resumeButton);
            }

            if (this.levelSelector != null && this.levelSelector.levelsPlaylist != null)
            {

                if (!OnlineManager.lobby.isOwner)
                {
                    for (int i = this.levelSelector.levelsPlaylist.levelItems.Count - 1; i >= 0; i--)
                    {
                        this.GetGameTypeSetup.playList.RemoveAt(this.GetGameTypeSetup.playList.Count - 1);
                        this.levelSelector.levelsPlaylist.RemoveLevelItem(new Menu.LevelSelector.LevelItem(this, this.levelSelector.levelsPlaylist, this.levelSelector.levelsPlaylist.levelItems[i].name));
                        this.levelSelector.levelsPlaylist.ScrollPos = this.levelSelector.levelsPlaylist.LastPossibleScroll;
                        this.levelSelector.levelsPlaylist.ConstrainScroll();
                    }

                }
                else
                {
                    arena.playList = this.GetGameTypeSetup.playList;
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
                if (arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.mePlayer.GetUniqueID(), out int existingValue))
                {
                    RainMeadow.Debug("Player already exists in dictionary");
                    RainMeadow.Debug("Current index" + existingValue.ToString());
                    CurrentColorIndex = existingValue;
                    slugcatButtons.meButton!.SetNewSlugcat(SlugcatFromIndex, existingValue, ArenaImage);
                }
                else
                {
                    RainMeadow.Debug("Player did NOT exist in dictionary");
                    CurrentColorIndex = 0;

                }
                slugcatButtons.meButton!.OnClick += (arenaSlugcatButton) =>
                {
                    CurrentColorIndex = (CurrentColorIndex + 1) % ArenaHelpers.allSlugcats.Count;
                    slugcatButtons.meButton!.SetNewSlugcat(SlugcatFromIndex, CurrentColorIndex, ArenaImage);
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    RainMeadow.Debug($"My ID: {OnlineManager.mePlayer.GetUniqueID()}");
                };
            }
        }
        private void UpdateProfilePlayerButtons(ArenaOnlineSlugcatButtons slugcatPlayerButtons)
        {
            for (int i = 0; i < slugcatPlayerButtons.otherArenaPlayerButtons?.Length; i++)
            {
                ArenaOnlinePlayerJoinButton playerButton = slugcatPlayerButtons.otherArenaPlayerButtons[i];
                playerButton.portraitBlack = Custom.LerpAndTick(playerButton.portraitBlack, 1f, 0.06f, 0.05f);
                playerButton.readyForCombat = arena != null && arena.playersReadiedUp != null && arena.playersReadiedUp.list != null && arena.playersReadiedUp.list.Contains(playerButton.profileIdentifier.id);
                playerButton.buttonBehav.greyedOut = arena.reigningChamps == null || arena.reigningChamps.list == null || !arena.reigningChamps.list.Contains(playerButton.profileIdentifier.id);
                int currentColorIndexOther = arena.playersInLobbyChoosingSlugs.TryGetValue(playerButton.profileIdentifier.GetUniqueID(), out int result) ? result : 0;
                playerButton.SetNewSlugcat(ArenaHelpers.allSlugcats[currentColorIndexOther], currentColorIndexOther, ArenaImage);

            }
        }
        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            BuildPlayerButtons();
            List<OnlinePlayer> otherOnlinePlayers = OtherOnlinePlayers;
            RainMeadow.Debug($"Player count without me: {otherOnlinePlayers.Count}");
            slugcatButtons.otherOnlinePlayers = otherOnlinePlayers;
            slugcatButtons.PopulatePage(slugcatButtons.CurrentOffset);
            if (OnlineManager.lobby?.isOwner == true)
            {
                arena.ResetForceReadyCountDownShort();
                if (arena.playersReadiedUp != null && arena.playersReadiedUp.list != null)
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
        private void BindSettings()
        {
            arena.avatarSettings.eyeColor = RainMeadow.rainMeadowOptions.EyeColor.Value;
            arena.avatarSettings.bodyColor = RainMeadow.rainMeadowOptions.BodyColor.Value;
            arena.avatarSettings.playingAs = SlugcatStats.Name.White;
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
            if (arena.isInGame && !arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))
            {
                return;
            }

            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

            if (OnlineManager.lobby.isOwner && this.GetGameTypeSetup.playList != null && this.GetGameTypeSetup.playList.Count == 0)
            {
                return; // don't be foolish
            }

            if (arena.playersReadiedUp.list.Count != OnlineManager.players.Count && arena.isInGame)
            {
                return;
            }


            if (!arena.allPlayersReadyLockLobby)
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
                        OnlineManager.lobby.owner.InvokeRPC(ArenaRPCs.Arena_NotifyLobbyReadyUp, OnlineManager.mePlayer);
                        this.playButton.menuLabel.text = this.Translate("Waiting for others...");
                        this.playButton.inactive = true;
                        this.playButton.buttonBehav.greyedOut = true;
                    }
                }
                return;
            }

            if (!OnlineManager.lobby.isOwner && !arena.isInGame)
            {
                RainMeadow.Debug("Host is not in game");
                return;
            }
            if (colorConfigDialog != null)
            {
                manager.StopSideProcess(colorConfigDialog); //force getting rid of dialog
            }
            if (OnlineManager.lobby.isOwner)
            {
                GetArenaSetup?.SaveToFile();
            }
            arena.avatarSettings.playingAs = ArenaHelpers.allSlugcats.GetValueOrDefault(CurrentColorIndex, ArenaHelpers.allSlugcats[UnityEngine.Random.Range(0, ArenaHelpers.allSlugcats.Count - 1)])!;
            arena.arenaClientSettings.playingAs = arena.avatarSettings.playingAs;
            arena.avatarSettings.currentColors = this.GetCustomColors(arena.avatarSettings.playingAs);
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
            this.totalClientsReadiedUpOnPage.text = this.Translate("Ready:") + " " + arena.playersReadiedUp.list.Count + "/" + OnlineManager.players.Count;
        }
        private void UpdateLevelCounter()
        {

            this.currentLevelProgression.text = this.Translate("Playlist Progress:") + " " + arena.currentLevel + "/" + arena.totalLevelCount;
        }
        private void UpdateGameModeLabel()
        {
            this.displayCurrentGameMode.text = Translate("Current Mode:") + " " + Utils.Translate(arena.currentGameMode);

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
