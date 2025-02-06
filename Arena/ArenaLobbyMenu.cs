using Kittehface.Framework20;
using Menu;
using Menu.Remix;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
namespace RainMeadow
{
    public class ArenaLobbyMenu : MultiplayerMenu
    {
        private ArenaOnlineGameMode arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;

        private static float num = 120f;
        private static float num2 = 0f;
        private static float num3 = num - num2;
        public bool clientReadiedUp = false;
        public MenuLabel totalClientsReadiedUpOnPage;
        public MenuLabel currentLevelProgression;

        public MenuLabel displayCurrentGameMode;

        private SimplerSymbolButton viewNextPlayer;
        private SimplerSymbolButton viewPrevPlayer;

        private int holdPlayerPosition;
        private int currentPlayerPosition;
        private bool initiatedStartGameForClient;
        public List<SlugcatStats.Name> allSlugs;
        public Dictionary<string, bool> playersReadiedUp = new Dictionary<string, bool>();


        int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // been using 1360 as ref

        public SimpleButton[] usernameButtons;
        public bool meUsernameButtonCreated = false;
        public bool meClassButtonCreated = false;

        public ArenaOnlinePlayerJoinButton[] classButtons;
        private bool flushArenaSittingForWaitingClients = false;


        public ArenaLobbyMenu(ProcessManager manager) : base(manager)
        {
            ID = OnlineManager.lobby.gameMode.MenuProcessId();
            RainMeadow.DebugMe();

            if (OnlineManager.lobby == null) throw new InvalidOperationException("lobby is null");

            if (OnlineManager.lobby.isOwner)
            {
                arena.arenaSittingOnlineOrder = new List<ushort>();
                arena.ResetGameTimer();
                arena.clientsAreReadiedUp = 0;
                arena.currentLevel = 0;
            }

            allSlugs = ArenaHelpers.AllSlugcats();
            holdPlayerPosition = 3; // the position we want to use for changing as we navigate
            ArenaHelpers.ResetReadyUpLogic(arena, this);

            OverrideMultiplayerMenu();
            BindSettings();
            BuildLayout();



            MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
            initiatedStartGameForClient = false;
            if (arena.currentGameMode == "" || arena.currentGameMode is null)
            {
                arena.currentGameMode = Competitive.CompetitiveMode.value;
            }

        }


        void RemoveExcessArenaObjects()
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


        void OverrideMultiplayerMenu()
        {

            RemoveExcessArenaObjects();

            this.currentGameType = this.nextGameType = ArenaSetup.GameTypeID.Competitive;
            this.nextButton.signalText = "NEXTONLINEGAME";
            this.prevButton.signalText = "PREVONLINEGAME";

            this.backButton.signalText = "BACKTOLOBBY";
            this.playButton.signalText = "STARTARENAONLINEGAME";
        }

        private void BindSettings()
        {
            arena.avatarSettings.eyeColor = RainMeadow.rainMeadowOptions.EyeColor.Value;
            arena.avatarSettings.bodyColor = RainMeadow.rainMeadowOptions.BodyColor.Value;
            arena.avatarSettings.playingAs = SlugcatStats.Name.White;
        }

        void BuildLayout()
        {
            BuildPlayerSlots();
            AddAbovePlayText();

            if (this.levelSelector != null && this.levelSelector.levelsPlaylist != null)
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


            this.displayCurrentGameMode = new MenuLabel(this, pages[0], this.Translate($"Current Mode: {arena.currentGameMode}"), new Vector2(this.usernameButtons[0].pos.x, usernameButtons[0].pos.y + 200f), new Vector2(10f, 10f), true);
            this.displayCurrentGameMode.label.alignment = FLabelAlignment.Left;
            this.pages[0].subObjects.Add(displayCurrentGameMode);
            // Ready up label
            this.totalClientsReadiedUpOnPage = new MenuLabel(this, pages[0], this.Translate($"Ready: {arena.clientsAreReadiedUp} / {OnlineManager.players.Count}"), new Vector2(displayCurrentGameMode.pos.x, usernameButtons[0].pos.y + 170f), new Vector2(10f, 10f), false);
            this.totalClientsReadiedUpOnPage.label.alignment = FLabelAlignment.Left;

            this.pages[0].subObjects.Add(totalClientsReadiedUpOnPage);


            this.currentLevelProgression = new MenuLabel(this, pages[0], this.Translate($"Playlist Progress: {arena.currentLevel} / {arena.totalLevelCount}"), new Vector2(displayCurrentGameMode.pos.x, usernameButtons[0].pos.y + 150f), new Vector2(10f, 10f), false);
            this.currentLevelProgression.label.alignment = FLabelAlignment.Left;
            this.pages[0].subObjects.Add(currentLevelProgression);


        }

        SimplerButton CreateButton(string text, Vector2 pos, Vector2 size, Action<SimplerButton>? clicked = null, Page? page = null)
        {
            page ??= pages[0];
            var b = new SimplerButton(this, page, text, pos, size);
            if (clicked != null) b.OnClick += clicked;
            page.subObjects.Add(b);
            return b;
        }




        void BuildPlayerSlots()
        {

            AddMeClassButton();
            AddMeUsername();
            //AddOtherUsernameButtons();
            //AddOtherPlayerClassButtons();


            this.GetArenaSetup.playersJoined[0] = true; // host should be part of game

            if (OnlineManager.players.Count > 4)
            {
                HandleLobbyProfileOverflow();

            }

        }

        void AddAbovePlayText()
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

        private void StartGame()
        {
            RainMeadow.DebugMe();

            if (arena.isInGame && !clientReadiedUp)
            {
                return;
            }

            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

            if (OnlineManager.lobby.isOwner && this.GetGameTypeSetup.playList != null && this.GetGameTypeSetup.playList.Count == 0)
            {
                return; // don't be foolish
            }



            if (arena.clientsAreReadiedUp != OnlineManager.players.Count && arena.isInGame)
            {
                return;
            }


            if (!arena.allPlayersReadyLockLobby)
            {
                if (!clientReadiedUp)
                {
                    arena.clientsAreReadiedUp++;

                }
                if (OnlineManager.lobby.isOwner)
                {
                    if (!arena.playersReadiedUp.Contains(OnlineManager.mePlayer.inLobbyId))
                    {
                        arena.playersReadiedUp.Add(OnlineManager.mePlayer.inLobbyId);
                    }
                }

                if (OnlineManager.players.Count > 1 && !clientReadiedUp)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeRPC(ArenaRPCs.Arena_NotifyLobbyReadyUp, OnlineManager.mePlayer, arena.clientsAreReadiedUp);

                        }
                    }
                    this.playButton.menuLabel.text = this.Translate("Waiting for others...");
                    this.playButton.inactive = true;
                    this.playButton.buttonBehav.greyedOut = true;
                }
                clientReadiedUp = true;
                return;
            }

            if (!OnlineManager.lobby.isOwner && !arena.isInGame)
            {
                return;
            }
            InitializeNewOnlineSitting();
            ArenaHelpers.SetupOnlineArenaStting(arena, this.manager);
            this.manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            // temp
            UserInput.SetUserCount(OnlineManager.players.Count);
            UserInput.SetForceDisconnectControllers(forceDisconnect: false);
            this.PlaySound(SoundID.MENU_Start_New_Game);
            this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);

        }

        public override void Update()
        {
            base.Update();


            if (this.totalClientsReadiedUpOnPage != null)
            {
                UpdateReadyUpLabel();
            }

            if (this.currentLevelProgression != null)
            {
                UpdateLevelCounter();
            }

            if (this.displayCurrentGameMode != null)
            {
                UpdateGameModeLabel();

            }

            if (arena.allPlayersReadyLockLobby && arena.isInGame && arena.arenaSittingOnlineOrder.Contains(OnlineManager.mePlayer.inLobbyId) && !OnlineManager.lobby.isOwner && !initiatedStartGameForClient && clientReadiedUp)  // time to go
            {
                this.StartGame();
                initiatedStartGameForClient = true;


            }



            if (this.playButton != null)
            {

                if (arena.clientsAreReadiedUp == 0 && arena.returnToLobby)
                {
                    this.playButton.menuLabel.text = "READY?";
                    this.playButton.inactive = false;
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
                        this.playButton.buttonBehav.greyedOut = false;
                    }
                }


                if (clientReadiedUp && OnlineManager.players.Count > 1)
                {
                    this.playButton.inactive = true;
                }

                if (arena.clientsAreReadiedUp == OnlineManager.players.Count)
                {
                    arena.allPlayersReadyLockLobby = true;

                    if (OnlineManager.players.Count == 1)
                    {
                        this.playButton.menuLabel.text = this.Translate("LOBBY WILL LOCK"); // are you sure you want to enter host? Nobody can join you

                    }
                    else
                    {
                        this.playButton.menuLabel.text = this.Translate("ENTER");

                    }

                    if (OnlineManager.lobby.isOwner) // go in, host
                    {
                        this.playButton.inactive = false;
                    }

                    if (!OnlineManager.lobby.isOwner)
                    {
                        if (!arena.isInGame) // wait for host to establish session
                        {
                            this.playButton.inactive = true;
                        }
                        else
                        {
                            this.playButton.inactive = false;
                        }

                    }

                }

                if (arena.isInGame)
                {
                    this.playButton.inactive = true;
                    if (arena.isInGame && !clientReadiedUp) // you're late
                    {
                        this.playButton.menuLabel.text = this.Translate("GAME IN SESSION");

                    }
                }

                if (arena.returnToLobby && !flushArenaSittingForWaitingClients) // coming back to lobby, reset everything
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this);
                    flushArenaSittingForWaitingClients = true;
                }



            }


        }


        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                OnlineManager.LeaveLobby();
            }
            base.ShutDownProcess();
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



        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            if (RainMeadow.isArenaMode(out var _))
            {
                RainMeadow.Debug(players);
                if (usernameButtons != null)
                {
                    for (int i = usernameButtons.Length - 1; i >= 0; i--)
                    {
                        if (usernameButtons[i] != null)
                        {
                            var playerbtn = usernameButtons[i];
                            playerbtn.RemoveSprites();
                            this.pages[0].RemoveSubObject(playerbtn);
                            meUsernameButtonCreated = false;

                        }
                    }
                }

                if (classButtons != null)
                {
                    for (int i = classButtons.Length - 1; i >= 0; i--)
                    {
                        if (classButtons[i] != null)
                        {
                            if (OnlineManager.lobby.isOwner) // kickbutton null check
                            {
                                if (classButtons[i].kickButton != null)
                                {
                                    classButtons[i].kickButton.RemoveSprites();
                                    this.pages[0].RemoveSubObject(classButtons[i].kickButton);
                                }
                            }
                            classButtons[i].RemoveSprites();
                            this.pages[0].RemoveSubObject(classButtons[i]);
                            meClassButtonCreated = false;
                        }

                    }
                }

                if (arena.playersReadiedUp.Count > OnlineManager.players.Count) // someone readied up then left
                {
                    RainMeadow.Debug("readyUpDictionary is greater than the number of players. Somebody left who was ready!");
                    List<ushort> keysToRemove = new List<ushort>();

                    for (int i = 0; i < arena.playersReadiedUp.Count; i++)
                    {
                        foreach (var player in arena.playersReadiedUp)
                        {
                            if (!OnlineManager.players.Any(player => player.inLobbyId.Equals(player)))
                            {
                                RainMeadow.Debug("Removing player who left from readyUpDictionary");
                                keysToRemove.Add(player);

                            }
                        }
                    }

                    for (int j = 0; j < keysToRemove.Count; j++)
                    {
                        arena.playersReadiedUp.Remove(keysToRemove[j]);
                    }
                    arena.clientsAreReadiedUp = arena.playersReadiedUp.Count;
                }
                foreach (var player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        if (arena.playersInLobbyChoosingSlugs.TryGetValue(player.id.name, out var existingValue))
                        {
                            RainMeadow.Debug("Player already exists in slug dictionary");
                        }
                        else
                        {
                            // Key does not exist, you can add it if needed
                            arena.playersInLobbyChoosingSlugs.Add(player.id.name, 0);
                        }

                        if (arena.playersReadiedUp.Contains(player.inLobbyId))
                        {
                            RainMeadow.Debug($"Player {player.id.name} already exists in readiedUp dictionary");
                        }
                        else
                        {
                            // Key does not exist, you can add it if needed
                            //arena.playersReadiedUp.Add(player.inLobbyId);
                        }
                    }
                }
                //AddOtherUsernameButtons();
                //AddOtherPlayerClassButtons();
                AddMeUsername();
                AddMeClassButton();

                HandleLobbyProfileOverflow();

                if (this != null)
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this);
                }


            }

        }


        private void AddMeClassButton()
        {
            classButtons = new ArenaOnlinePlayerJoinButton[OnlineManager.players.Count];
            int meIndex = -1;
            for (int i = 0; i < OnlineManager.players.Count; i++)
            {
                if (OnlineManager.players[i].isMe)
                {
                    meIndex = i;
                    break;
                }
            }

            if (!meClassButtonCreated)
            {
                if (meIndex != -1) // If 'isMe' player found
                {
                    classButtons[0] = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f), 40f), 0);
                    classButtons[0].buttonBehav.greyedOut = false;
                    classButtons[0].readyForCombat = true;
                    int currentColorIndex;
                    if (arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.mePlayer.id.name, out var existingValue))
                    {
                        currentColorIndex = arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.name];
                        RainMeadow.Debug("Player already exists in dictionary");
                        RainMeadow.Debug("Current index" + currentColorIndex);
                        classButtons[0].portrait.fileName = ArenaImage(allSlugs[currentColorIndex], currentColorIndex);
                        classButtons[0].portrait.LoadFile();
                        classButtons[0].portrait.sprite.SetElementByName(classButtons[0].portrait.fileName);
                    }
                    else
                    {
                        RainMeadow.Debug("Player did NOT exist in dictionary");
                        currentColorIndex = 0;
                        arena.playersInLobbyChoosingSlugs.Add(OnlineManager.mePlayer.id.name, currentColorIndex);
                    }
                    classButtons[0].OnClick += (_) =>
                    {
                        currentColorIndex = (currentColorIndex + 1) % allSlugs.Count;
                        allSlugs[currentColorIndex] = allSlugs[currentColorIndex];
                        classButtons[0].portrait.fileName = ArenaImage(allSlugs[currentColorIndex], currentColorIndex);
                        classButtons[0].portrait.LoadFile();
                        classButtons[0].portrait.sprite.SetElementByName(classButtons[0].portrait.fileName);
                        PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

                        arena.avatarSettings.playingAs = allSlugs[currentColorIndex];
                        arena.arenaClientSettings.playingAs = arena.avatarSettings.playingAs;

                        if (OnlineManager.players.Count > 1)
                        {
                            foreach (var player in OnlineManager.players)
                            {
                                if (!player.isMe)
                                {
                                    player.InvokeRPC(ArenaRPCs.Arena_NotifyClassChange, OnlineManager.mePlayer, currentColorIndex);

                                }
                            }
                        }

                        arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.name] = currentColorIndex;
                    };
                    pages[0].subObjects.Add(classButtons[0]);
                    arena.avatarSettings.playingAs = allSlugs[currentColorIndex];
                    arena.arenaClientSettings.playingAs = arena.avatarSettings.playingAs;
                    meClassButtonCreated = true;
                }
            }
            int localIndex = 1; // Start placing players from index 1
            for (int i = 0; i < OnlineManager.players.Count; i++)
            {
                if (OnlineManager.players[i].isMe) continue;
                {

                    if (localIndex > 3)
                    {
                        break;
                    }

                    classButtons[localIndex] = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + localIndex * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f) * classButtons.Length, 40f), localIndex);
                    classButtons[localIndex].buttonBehav.greyedOut = true;
                    classButtons[localIndex].readyForCombat = arena.playersReadiedUp.Contains(OnlineManager.players[i].inLobbyId);
                    classButtons[localIndex].portraitBlack = Custom.LerpAndTick(classButtons[localIndex].portraitBlack, 1f, 0.06f, 0.05f);
                    if (!arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.players[i].id.name, out var currentColorIndex))
                    {
                        currentColorIndex = 0;
                    }
                    classButtons[localIndex].portrait.fileName = ArenaImage(allSlugs[currentColorIndex], currentColorIndex);
                    classButtons[localIndex].portrait.LoadFile();
                    classButtons[localIndex].portrait.sprite.SetElementByName(classButtons[localIndex].portrait.fileName);
                    pages[0].subObjects.Add(classButtons[localIndex]);

                    if (OnlineManager.lobby.isOwner)
                    {
                        classButtons[localIndex].kickButton = new SimplerSymbolButton(this, this.pages[0], "Menu_Symbol_Clear_All", "KICKPLAYER", new Vector2(classButtons[localIndex].pos.x + 40f, classButtons[localIndex].pos.y + 110f));

                        classButtons[localIndex].kickButton.OnClick += (_) =>
                        {
                            RainMeadow.Debug("Kicked User: " + OnlineManager.players[localIndex]);
                            BanHammer.BanUser(OnlineManager.players[localIndex]);
                        };
                        this.pages[0].subObjects.Add(classButtons[localIndex].kickButton);
                    }
                    localIndex++; // Increment to the next available index
                }

            }
        }

        private void AddMeUsername()
        {
            usernameButtons = new SimplerButton[OnlineManager.players.Count];
            int meIndex = -1;
            for (int i = 0; i < OnlineManager.players.Count; i++)
            {
                if (OnlineManager.players[i].isMe)
                {
                    meIndex = i;
                    break;
                }
            }

            if (meIndex != -1)
            {
                // Assign 'isMe' player to index 0
                usernameButtons[0] = new SimplerButton(this, pages[0], OnlineManager.mePlayer.id.name, new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * usernameButtons.Length, 40f), new Vector2(num - 20f, 30f));
                (usernameButtons[0] as SimplerButton).OnClick += (_) =>
                {
                    OnlineManager.mePlayer.id.OpenProfileLink(); // Open profile for 'isMe' player
                };
                usernameButtons[0].buttonBehav.greyedOut = false;

                pages[0].subObjects.Add(usernameButtons[0]);
            }


            int buttonIndex = 1; // Start placing players from index 1
            for (int i = 0; i < OnlineManager.players.Count; i++)
            {
                if (OnlineManager.players[i].isMe) continue;
                {
                    // Use buttonIndex to assign non-'isMe' players to the next available indices
                    // Create button for other players
                    usernameButtons[buttonIndex] = new SimplerButton(this, pages[0], OnlineManager.players[i].id.name, new Vector2(600f + buttonIndex + 1 * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * usernameButtons.Length, 40f), new Vector2(num - 20f, 30f));
                    (usernameButtons[buttonIndex] as SimplerButton).OnClick += (_) =>
                    {
                        OnlineManager.players[buttonIndex].id.OpenProfileLink(); // Open profile for other players
                    };

                    usernameButtons[buttonIndex].buttonBehav.greyedOut = false;

                    pages[0].subObjects.Add(usernameButtons[buttonIndex]);
                    buttonIndex++;
                }
            }




        }
        private void AddOtherPlayerClassButtons()
        {
            classButtons = new ArenaOnlinePlayerJoinButton[OnlineManager.players.Count];
            if (OnlineManager.players.Count > 1)
            {
                for (int l = 1; l < OnlineManager.players.Count; l++)
                {

                    if (l > 3)
                    {
                        break;
                    }

                    int localIndex = l;

                    classButtons[l] = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + l * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f) * classButtons.Length, 40f), l);


                    classButtons[l].buttonBehav.greyedOut = true;

                    classButtons[l].readyForCombat = arena.playersReadiedUp.Contains(OnlineManager.players[l].inLobbyId);

                    classButtons[l].portraitBlack = Custom.LerpAndTick(classButtons[l].portraitBlack, 1f, 0.06f, 0.05f);
                    if (!arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.players[l].id.name, out var currentColorIndex))
                    {
                        currentColorIndex = 0;
                    }

                    classButtons[l].portrait.fileName = ArenaImage(allSlugs[currentColorIndex], currentColorIndex);

                    classButtons[l].portrait.LoadFile();
                    classButtons[l].portrait.sprite.SetElementByName(classButtons[l].portrait.fileName);

                    pages[0].subObjects.Add(classButtons[l]);

                    if (OnlineManager.lobby.isOwner)
                    {

                        classButtons[localIndex].kickButton = new SimplerSymbolButton(this, this.pages[0], "Menu_Symbol_Clear_All", "KICKPLAYER", new Vector2(classButtons[localIndex].pos.x + 40f, classButtons[localIndex].pos.y + 110f));

                        classButtons[localIndex].kickButton.OnClick += (_) =>
                        {
                            RainMeadow.Debug("Kicked User: " + OnlineManager.players[localIndex]);
                            BanHammer.BanUser(OnlineManager.players[localIndex]);
                        };

                        this.pages[0].subObjects.Add(classButtons[localIndex].kickButton);
                    }
                }
            }
        }

        public void AddOtherUsernameButtons()
        {

            usernameButtons = new SimplerButton[OnlineManager.players.Count];

            if (OnlineManager.players.Count > 1)
            {
                for (int k = 0; k < usernameButtons.Length; k++)
                {
                    if (k > 3)
                    {
                        break;
                    }

                    string name = OnlineManager.players[k].id.name;



                    usernameButtons[k] = new SimplerButton(this, pages[0], name, new Vector2(600f + k + 1 * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * usernameButtons.Length, 40f), new Vector2(num - 20f, 30f));

                    (usernameButtons[k] as SimplerButton).OnClick += (_) =>
                    {
                        OnlineManager.players[k].id.OpenProfileLink();
                    };

                    usernameButtons[k].buttonBehav.greyedOut = false;


                    pages[0].subObjects.Add(usernameButtons[k]);
                }
            }
        }

        private void UpdateReadyUpLabel()
        {
            this.totalClientsReadiedUpOnPage.text = this.Translate($"Ready: {arena.clientsAreReadiedUp} / {OnlineManager.players.Count}");

        }

        private void UpdateLevelCounter()
        {
            this.currentLevelProgression.text = this.Translate($"Playlist Progress: {arena.currentLevel} / {arena.totalLevelCount}");

        }
        private void UpdateGameModeLabel()
        {
            this.displayCurrentGameMode.text = $"Current Mode: {arena.currentGameMode}";

        }

        private void HandleLobbyProfileOverflow()
        {

            if (viewNextPlayer != null)
            {
                viewNextPlayer.RemoveSprites();
                pages[0].RemoveSubObject(viewNextPlayer);
            }

            if (viewPrevPlayer != null)
            {

                viewPrevPlayer.RemoveSprites();
                pages[0].RemoveSubObject(viewPrevPlayer);
            }

            if (OnlineManager.players.Count <= 4)
            {
                return;
            }

            currentPlayerPosition = holdPlayerPosition;

            viewNextPlayer = new SimplerSymbolButton(this, pages[0], "Menu_Symbol_Arrow", "VIEWNEXT", new Vector2(classButtons[holdPlayerPosition].pos.x + 120f, classButtons[holdPlayerPosition].pos.y + 60));
            viewNextPlayer.symbolSprite.rotation = 90;
            if (currentPlayerPosition == OnlineManager.players.Count - 1)
            {
                viewNextPlayer.buttonBehav.greyedOut = true;
            }


            viewNextPlayer.OnClick += (_) =>
            {

                currentPlayerPosition++;

                if (viewPrevPlayer != null && viewPrevPlayer.buttonBehav.greyedOut)
                {
                    viewPrevPlayer.buttonBehav.greyedOut = false;
                }

                usernameButtons[holdPlayerPosition].menuLabel.text = OnlineManager.players[currentPlayerPosition].id.name; // current becomes next


                int localIndex = currentPlayerPosition;
                if (OnlineManager.lobby.isOwner)
                {
                    classButtons[holdPlayerPosition].kickButton.ResetSubscriptions();
                    classButtons[holdPlayerPosition].kickButton.OnClick += (_) =>
                    {
                        RainMeadow.Debug($"Kicking player {OnlineManager.players[localIndex]} at index {localIndex}");
                        BanHammer.BanUser(OnlineManager.players[localIndex]);
                    };
                }

                classButtons[holdPlayerPosition].portrait.fileName = ArenaImage(allSlugs[arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition].id.name]], arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition].id.name]);
                classButtons[holdPlayerPosition].portrait.LoadFile();
                classButtons[holdPlayerPosition].portrait.sprite.SetElementByName(classButtons[holdPlayerPosition].portrait.fileName);
                try
                {
                    classButtons[holdPlayerPosition].readyForCombat = arena.playersReadiedUp.Contains(OnlineManager.players[holdPlayerPosition].inLobbyId);
                }
                catch
                {
                    classButtons[holdPlayerPosition].readyForCombat = false;
                }

                if (currentPlayerPosition + 1 >= OnlineManager.players.Count)
                {
                    RainMeadow.Debug("End of extended list: " + currentPlayerPosition);
                    viewNextPlayer.buttonBehav.greyedOut = true;
                    return;
                }

                else
                {
                    return;
                }

            };

            viewPrevPlayer = new SimplerSymbolButton(this, pages[0], "Menu_Symbol_Arrow", "VIEWPREV", new Vector2(classButtons[holdPlayerPosition].pos.x + 120f, classButtons[holdPlayerPosition].pos.y + 20));
            viewPrevPlayer.symbolSprite.rotation = 270;
            if (currentPlayerPosition <= 3)
            {
                viewPrevPlayer.buttonBehav.greyedOut = true;
            }
            viewPrevPlayer.OnClick += (_) =>
            {

                currentPlayerPosition--;
                if (viewNextPlayer != null && viewNextPlayer.buttonBehav.greyedOut)
                {
                    viewNextPlayer.buttonBehav.greyedOut = false;
                }

                usernameButtons[holdPlayerPosition].menuLabel.text = OnlineManager.players[currentPlayerPosition].id.name; // current becomes previous


                int localIndex = currentPlayerPosition;
                if (OnlineManager.lobby.isOwner)
                {
                    classButtons[holdPlayerPosition].kickButton.ResetSubscriptions();
                    classButtons[holdPlayerPosition].kickButton.OnClick += (_) =>
                    {
                        RainMeadow.Debug($"Kicking player {OnlineManager.players[localIndex]} at index {localIndex}");
                        BanHammer.BanUser(OnlineManager.players[localIndex]);
                    };
                }

                classButtons[holdPlayerPosition].portrait.fileName = ArenaImage(allSlugs[arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition].id.name]], arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition].id.name]);
                classButtons[holdPlayerPosition].portrait.LoadFile();
                classButtons[holdPlayerPosition].portrait.sprite.SetElementByName(classButtons[holdPlayerPosition].portrait.fileName);
                try
                {
                    classButtons[holdPlayerPosition].readyForCombat = arena.playersReadiedUp.Contains(OnlineManager.players[holdPlayerPosition].inLobbyId);
                }
                catch
                {
                    classButtons[holdPlayerPosition].readyForCombat = false;
                }

                if (currentPlayerPosition - 1 <= 3)
                {
                    RainMeadow.Debug("Beginning of extended list: " + currentPlayerPosition);
                    viewPrevPlayer.buttonBehav.greyedOut = true;
                    return;
                }

                else
                {
                    return;
                }
            };
            this.pages[0].subObjects.Add(viewNextPlayer);
            this.pages[0].subObjects.Add(viewPrevPlayer);
        }

    }
}
