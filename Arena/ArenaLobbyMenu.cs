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
        public SimpleButton meUsernameButton;
        public ArenaOnlinePlayerJoinButton meClassButton;

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
            }

            allSlugs = ArenaHelpers.AllSlugcats();
            holdPlayerPosition = 3; // the position we want to use for changing as we navigate

            OverrideMultiplayerMenu();
            BindSettings();
            BuildLayout();

            ArenaHelpers.ResetReadyUpLogic(arena, this);


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

            // Ready up label
            this.totalClientsReadiedUpOnPage = new MenuLabel(this, pages[0], this.Translate($"Ready: {arena.clientsAreReadiedUp} / {OnlineManager.players.Count}"), new Vector2(meUsernameButton.pos.x + 30f, meUsernameButton.pos.y + 150f), new Vector2(10f, 10f), false);
            this.pages[0].subObjects.Add(totalClientsReadiedUpOnPage);

            this.displayCurrentGameMode = new MenuLabel(this, pages[0], this.Translate($"Current Mode: {arena.currentGameMode}"), new Vector2(this.meUsernameButton.pos.x, meUsernameButton.pos.y + 200f), new Vector2(10f, 10f), true);
            this.pages[0].subObjects.Add(displayCurrentGameMode);


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
            AddOtherUsernameButtons();
            AddOtherPlayerClassButtons();


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

            }

            // Client retrieves playlist
            else
            {
                manager.arenaSitting.levelPlaylist = arena.playList;

            }

            ArenaHelpers.SetProfileColor(arena);
            arena.returnToLobby = false;

            //

            //
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

                if (OnlineManager.players.Count > 1 && !clientReadiedUp)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeRPC(ArenaRPCs.Arena_NotifyLobbyReadyUp, OnlineManager.mePlayer.id.name, arena.clientsAreReadiedUp);

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
                for (int i = usernameButtons.Length - 1; i >= 1; i--)
                {
                    if (usernameButtons[i] != null)
                    {
                        var playerbtn = usernameButtons[i];
                        playerbtn.RemoveSprites();
                        this.pages[0].RemoveSubObject(playerbtn);

                    }
                }

                for (int i = classButtons.Length - 1; i >= 1; i--)
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
                    }

                }


                if (OnlineManager.players.Count > 1)
                {
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

                            if (arena.playersReadiedUp.TryGetValue(player.id.name, out var alreadyReady))
                            {
                                RainMeadow.Debug("Player already exists in readiedUp dictionary");
                            }
                            else
                            {
                                // Key does not exist, you can add it if needed
                                arena.playersReadiedUp.Add(player.id.name, false);
                            }
                        }
                    }
                    AddOtherUsernameButtons();
                    AddOtherPlayerClassButtons();
                    HandleLobbyProfileOverflow();

                    if (this != null)
                    {
                        ArenaHelpers.ResetReadyUpLogic(arena, this);
                    }

                }
            }

        }


        private void AddMeClassButton() // doing unique stuff with player 0 so less annoying this way
        {

            meClassButton = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f), 40f), 0);
            meClassButton.buttonBehav.greyedOut = false;
            meClassButton.readyForCombat = true;
            int currentColorIndex;
            if (arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.mePlayer.id.name, out var existingValue))
            {
                currentColorIndex = arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.name];
                RainMeadow.Debug("Player already exists in dictionary");
                RainMeadow.Debug("Current index" + currentColorIndex);
                meClassButton.portrait.fileName = ArenaImage(allSlugs[currentColorIndex], currentColorIndex);
                meClassButton.portrait.LoadFile();
                meClassButton.portrait.sprite.SetElementByName(meClassButton.portrait.fileName);
            }
            else
            {
                RainMeadow.Debug("Player did NOT exist in dictionary");
                currentColorIndex = 0;
                arena.playersInLobbyChoosingSlugs.Add(OnlineManager.mePlayer.id.name, currentColorIndex);
            }
            meClassButton.OnClick += (_) =>
            {
                currentColorIndex = (currentColorIndex + 1) % allSlugs.Count;
                allSlugs[currentColorIndex] = allSlugs[currentColorIndex];
                meClassButton.portrait.fileName = ArenaImage(allSlugs[currentColorIndex], currentColorIndex);
                meClassButton.portrait.LoadFile();
                meClassButton.portrait.sprite.SetElementByName(meClassButton.portrait.fileName);
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);



                arena.avatarSettings.playingAs = allSlugs[currentColorIndex];
                arena.arenaClientSettings.playingAs = arena.avatarSettings.playingAs;

                if (OnlineManager.players.Count > 1 && !arena.allPlayersReadyLockLobby) // stop unnecessary RPCs
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeRPC(ArenaRPCs.Arena_NotifyClassChange, OnlineManager.mePlayer.id.name, currentColorIndex);

                        }
                    }
                }

                arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.name] = currentColorIndex;



            };

            pages[0].subObjects.Add(meClassButton);
            arena.avatarSettings.playingAs = allSlugs[currentColorIndex];
            arena.arenaClientSettings.playingAs = arena.avatarSettings.playingAs;

        }

        private void AddMeUsername()
        {

            string name = OnlineManager.mePlayer.id.name;
            meUsernameButton = new SimplerButton(this, pages[0], name, new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f), 40f), new Vector2(num - 20f, 30f));
            (meUsernameButton as SimplerButton).OnClick += (_) =>
            {
                OnlineManager.mePlayer.id.OpenProfileLink();
            };

            meUsernameButton.buttonBehav.greyedOut = false;


            pages[0].subObjects.Add(meUsernameButton);



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
                        if (OnlineManager.players.Count <= 4)
                        {
                            classButtons[localIndex].kickButton.OnClick += (_) =>
                            {
                                RainMeadow.Debug("Kicked User: " + OnlineManager.players[localIndex]);
                                BanHammer.BanUser(OnlineManager.players[localIndex]);
                            };
                        }
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
                for (int k = 1; k < usernameButtons.Length; k++)
                {
                    if (k > 3)
                    {
                        break;
                    }

                    string name = OnlineManager.players[k].id.name;
                    usernameButtons[k] = new SimplerButton(this, pages[0], name, new Vector2(600f + k * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * usernameButtons.Length, 40f), new Vector2(num - 20f, 30f));
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
            this.totalClientsReadiedUpOnPage.text = $"Ready: {arena.clientsAreReadiedUp} / {OnlineManager.players.Count}";

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


                if (viewPrevPlayer != null && viewPrevPlayer.buttonBehav.greyedOut)
                {
                    viewPrevPlayer.buttonBehav.greyedOut = false;
                }

                usernameButtons[holdPlayerPosition].menuLabel.text = OnlineManager.players[currentPlayerPosition + 1].id.name; // current becomes next
                int localIndex = currentPlayerPosition;
                if (OnlineManager.lobby.isOwner)
                {
                    classButtons[holdPlayerPosition].kickButton.OnClick += (_) =>
                    {
                        BanHammer.BanUser(OnlineManager.players[localIndex + 1]);
                    };
                }

                classButtons[holdPlayerPosition].portrait.fileName = ArenaImage(allSlugs[arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition + 1].id.name]], arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition + 1].id.name]);
                classButtons[holdPlayerPosition].portrait.LoadFile();
                classButtons[holdPlayerPosition].portrait.sprite.SetElementByName(classButtons[holdPlayerPosition].portrait.fileName);
                try
                {
                    classButtons[holdPlayerPosition].readyForCombat = arena.playersReadiedUp[OnlineManager.players[currentPlayerPosition + 1].id.name];
                }
                catch
                {
                    classButtons[holdPlayerPosition].readyForCombat = false;
                }

                currentPlayerPosition++;

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


                if (viewNextPlayer != null && viewNextPlayer.buttonBehav.greyedOut)
                {
                    viewNextPlayer.buttonBehav.greyedOut = false;
                }

                usernameButtons[holdPlayerPosition].menuLabel.text = OnlineManager.players[currentPlayerPosition - 1].id.name; // current becomes previous
                int localIndex = currentPlayerPosition;
                if (OnlineManager.lobby.isOwner)
                {
                    classButtons[holdPlayerPosition].kickButton.OnClick += (_) =>
                    {
                        BanHammer.BanUser(OnlineManager.players[localIndex - 1]);
                    };
                }

                classButtons[holdPlayerPosition].portrait.fileName = ArenaImage(allSlugs[arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition - 1].id.name]], arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition - 1].id.name]);
                classButtons[holdPlayerPosition].portrait.LoadFile();
                classButtons[holdPlayerPosition].portrait.sprite.SetElementByName(classButtons[holdPlayerPosition].portrait.fileName);
                try
                {
                    classButtons[holdPlayerPosition].readyForCombat = arena.playersReadiedUp[OnlineManager.players[currentPlayerPosition - 1].id.name];
                }
                catch
                {
                    classButtons[holdPlayerPosition].readyForCombat = false;
                }

                currentPlayerPosition--;
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
