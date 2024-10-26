using Kittehface.Framework20;
using Menu;
using Menu.Remix;
using RWCustom;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow
{
    public class ArenaLobbyMenu : MultiplayerMenu
    {
        private ArenaCompetitiveGameMode arena => (ArenaCompetitiveGameMode)OnlineManager.lobby.gameMode;

        private SlugcatCustomization personaSettings;
        private static float num = 120f;
        private static float num2 = 0f;
        private static float num3 = num - num2;
        private List<string> sharedPlayList = new List<string>();
        private OpTinyColorPicker bodyColorPicker;
        private OpTinyColorPicker eyeColorPicker;
        public UIelementWrapper bodyColor;
        public UIelementWrapper eyeColor;
        public bool clientReadiedUp = false;
        public MenuLabel totalClientsReadiedUpOnPage;
        private SimplerSymbolButton viewNextPlayer;
        private SimplerSymbolButton viewPrevPlayer;

        private int holdPlayerPosition;
        private int currentPlayerPosition;
        public List<SlugcatStats.Name> allSlugs;



        int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // been using 1360 as ref

        public SimpleButton[] usernameButtons;
        public SimpleButton meUsernameButton;
        public ArenaOnlinePlayerJoinButton meClassButton;

        public ArenaOnlinePlayerJoinButton[] classButtons;
        private bool flushArenaSittingForWaitingClients = false;


        public ArenaLobbyMenu(ProcessManager manager) : base(manager)
        {
            RainMeadow.DebugMe();

            if (OnlineManager.lobby == null) throw new InvalidOperationException("lobby is null");

            if (OnlineManager.lobby.isOwner)
            {
                arena.arenaSittingOnlineOrder = new List<ushort>();
            }

            allSlugs = ArenaHelpers.AllSlugcats();
            holdPlayerPosition = 3; // the position we want to use for changing as we navigate

            OverrideMultiplayerMenu();
            BindSettings();
            BuildLayout();


            ArenaHelpers.ResetReadyUpLogic(arena, this);


            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
            //SetupCharacterCustomization();


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
                for (int i = this.levelSelector.levelsPlaylist.levelItems.Count - 1; i >= 0; i--)
                {
                    this.GetGameTypeSetup.playList.RemoveAt(this.GetGameTypeSetup.playList.Count - 1);
                    this.levelSelector.levelsPlaylist.RemoveLevelItem(new Menu.LevelSelector.LevelItem(this, this.levelSelector.levelsPlaylist, this.levelSelector.levelsPlaylist.levelItems[i].name));
                    this.levelSelector.levelsPlaylist.ScrollPos = this.levelSelector.levelsPlaylist.LastPossibleScroll;
                    this.levelSelector.levelsPlaylist.ConstrainScroll();
                }

            }

        }


        void OverrideMultiplayerMenu()
        {

            RemoveExcessArenaObjects();

            this.currentGameType = this.nextGameType = ArenaSetup.GameTypeID.Competitive;
            this.nextButton.inactive = true;
            this.nextButton.signalText = "BLACKHOLE";
            this.prevButton.inactive = true;
            this.prevButton.signalText = "BLACKHOLE";

            this.backButton.signalText = "BACKTOLOBBY";
            this.playButton.signalText = "STARTARENAONLINEGAME";
            this.GetGameTypeSetup.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;
            this.GetGameTypeSetup.rainWhenOnePlayerLeft = false; // TODO:  Hook this to update logic due to level switching if we want it
            this.GetGameTypeSetup.savingAndLoadingSession = false;
            this.GetGameTypeSetup.saveCreatures = false;
        }

        private void BindSettings()
        {
            this.personaSettings = (OnlineManager.lobby.gameMode as ArenaCompetitiveGameMode).avatarSettings;
            personaSettings.bodyColor = RainMeadow.rainMeadowOptions.BodyColor.Value;
            personaSettings.eyeColor = RainMeadow.rainMeadowOptions.EyeColor.Value;
            personaSettings.playingAs = SlugcatStats.Name.White;
            arena.arenaClientSettings.playingAs = personaSettings.playingAs;
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


        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

            if (OnlineManager.lobby.isOwner && this.GetGameTypeSetup.playList != null && this.GetGameTypeSetup.playList.Count == 0)
            {
                return; // don't be foolish
            }



            if (arena.clientsAreReadiedUp != OnlineManager.players.Count && arena.isInGame)
            {
                return;
            }
            if (!clientReadiedUp)
            {
                arena.clientsAreReadiedUp++;

            }

            if (!arena.allPlayersReadyLockLobby)
            {

                clientReadiedUp = true;
                if (OnlineManager.players.Count > 1)
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
            this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);

        }

        public override void Update()
        {
            base.Update();

            if (this.totalClientsReadiedUpOnPage != null)
            {
                UpdateReadyUpLabel();
            }

            if (this.playButton != null)
            {


                if (OnlineManager.players.Count == 1)
                {
                    this.playButton.menuLabel.text = this.Translate("WAIT FOR OTHERS");
                    //this.playButton.inactive = true;

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

                if (arena.arenaSittingOnlineOrder.Count != OnlineManager.players.Count && arena.isInGame) // you're late.
                {
                    this.playButton.inactive = true;
                    this.playButton.menuLabel.text = this.Translate("GAME IN SESSION");
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

            }
            if (message == "STARTARENAONLINEGAME")
            {
                StartGame();
            }

            if (message == "INFO" && infoWindow != null)
            {
                infoWindow.label.text = Regex.Replace(this.Translate("Welcome to Arena Online!<LINE>All players must ready up to begin."), "<LINE>", "\r\n");
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
                    }
                    //mainPage.RemoveSubObject(playerbtn);
                }

                for (int i = classButtons.Length - 1; i >= 1; i--)
                {
                    if (classButtons[i] != null)
                    {
                        var playerbtn = classButtons[i];
                        playerbtn.RemoveSprites();
                    }
                    //mainPage.RemoveSubObject(playerbtn);
                }




                if (OnlineManager.players.Count > 1)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            if (arena.playersInLobbyChoosingSlugs.TryGetValue(player.id.name, out var existingValue))
                            {
                                RainMeadow.Debug("Player already exists in online dictionary");
                            }
                            else
                            {
                                // Key does not exist, you can add it if needed
                                arena.playersInLobbyChoosingSlugs.Add(player.id.name, 0);
                            }
                        }
                    }
                    AddOtherUsernameButtons();
                    AddOtherPlayerClassButtons();
                    HandleLobbyProfileOverflow();

                    
                    clientReadiedUp = false;


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
            var currentColorIndex = 0;
            if (arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.mePlayer.id.name, out var existingValue))
            {
                currentColorIndex = arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.name];
                RainMeadow.Debug("Player already exists in dictionary");
                if (arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.name] > 3 && ModManager.MSC)
                {
                    meClassButton.portrait.fileName = "MultiplayerPortrait" + "41-" + allSlugs[currentColorIndex];

                }
                else
                {
                    meClassButton.portrait.fileName = "MultiplayerPortrait" + currentColorIndex + "1";
                }
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

                if (currentColorIndex > 3 && ModManager.MSC)
                {
                    meClassButton.portrait.fileName = "MultiplayerPortrait" + "41-" + allSlugs[currentColorIndex];

                }
                else
                {
                    meClassButton.portrait.fileName = "MultiplayerPortrait" + currentColorIndex + "1";
                }


                meClassButton.portrait.LoadFile();
                meClassButton.portrait.sprite.SetElementByName(meClassButton.portrait.fileName);
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

                personaSettings.playingAs = allSlugs[currentColorIndex];
                arena.arenaClientSettings.playingAs = personaSettings.playingAs;

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
        }

        private void AddMeUsername()
        {

            string name = OnlineManager.mePlayer.id.name;
            CSteamID playerId;
            if (OnlineManager.players[0].id is LocalMatchmakingManager.LocalPlayerId)
            {
                playerId = default;
            }
            else
            {
                playerId = (OnlineManager.mePlayer.id as SteamMatchmakingManager.SteamPlayerId).steamID;
            }

            meUsernameButton = new SimplerButton(this, pages[0], name, new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f), 40f), new Vector2(num - 20f, 30f));
            (meUsernameButton as SimplerButton).OnClick += (_) =>
            {
                string url = $"https://steamcommunity.com/profiles/{playerId}";
                SteamFriends.ActivateGameOverlayToWebPage(url);
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

                    if (this.usernameButtons[l].menuLabel.text == OnlineManager.players[l].id.name) // we have our mark

                        classButtons[l] = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + l * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f) * classButtons.Length, 40f), l);
                    classButtons[l].buttonBehav.greyedOut = true;
                    classButtons[l].portraitBlack = Custom.LerpAndTick(classButtons[l].portraitBlack, 1f, 0.06f, 0.05f);
                    var currentColorIndex = arena.playersInLobbyChoosingSlugs[OnlineManager.players[l].id.name];

                    if (arena.playersInLobbyChoosingSlugs[OnlineManager.players[l].id.name] > 3 && ModManager.MSC)
                    {
                        classButtons[l].portrait.fileName = "MultiplayerPortrait" + "41-" + allSlugs[currentColorIndex];

                    }
                    else
                    {
                        classButtons[l].portrait.fileName = "MultiplayerPortrait" + currentColorIndex + "1";
                    }
                    classButtons[l].portrait.LoadFile();
                    classButtons[l].portrait.sprite.SetElementByName(classButtons[l].portrait.fileName);

                    pages[0].subObjects.Add(classButtons[l]);
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
                    CSteamID playerId;
                    if (OnlineManager.players[k].id is LocalMatchmakingManager.LocalPlayerId)
                    {
                        playerId = default;
                    }
                    else
                    {
                        playerId = (OnlineManager.players[k].id as SteamMatchmakingManager.SteamPlayerId).steamID;
                    }

                    usernameButtons[k] = new SimplerButton(this, pages[0], name, new Vector2(600f + k * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * usernameButtons.Length, 40f), new Vector2(num - 20f, 30f));
                    (usernameButtons[k] as SimplerButton).OnClick += (_) =>
                    {
                        string url = $"https://steamcommunity.com/profiles/{playerId}";
                        SteamFriends.ActivateGameOverlayToWebPage(url);
                    };

                    usernameButtons[k].buttonBehav.greyedOut = false;


                    pages[0].subObjects.Add(usernameButtons[k]);
                }
            }
        }

        private void SetupCharacterCustomization()
        {
            var bodyLabel = new MenuLabel(this, pages[0], Translate("Body color"), new Vector2(800, 353), new(0, 30), false);
            bodyLabel.label.alignment = FLabelAlignment.Right;
            this.pages[0].subObjects.Add(bodyLabel);

            var eyeLabel = new MenuLabel(this, pages[0], Translate("Eye color"), new Vector2(900, 353), new(0, 30), false);
            eyeLabel.label.alignment = FLabelAlignment.Right;
            this.pages[0].subObjects.Add(eyeLabel);

            bodyColorPicker = new OpTinyColorPicker(this.pages[0].menu, new Vector2(705, 353), personaSettings.bodyColor);
            var tabWrapper = new MenuTabWrapper(this, this.pages[0]);
            bodyColor = new UIelementWrapper(tabWrapper, bodyColorPicker);
            bodyColorPicker.OnValueChangedEvent += ColorPicker_OnValueChangedEvent;

            eyeColorPicker = new OpTinyColorPicker(this.pages[0].menu, new Vector2(810, 353), personaSettings.eyeColor);
            eyeColor = new UIelementWrapper(tabWrapper, eyeColorPicker);
            eyeColorPicker.OnValueChangedEvent += ColorPicker_OnValueChangedEvent;

            pages[0].subObjects.Add(bodyColor);
            pages[0].subObjects.Add(eyeColor);


        }

        private void ColorPicker_OnValueChangedEvent()
        {
            if (personaSettings != null) personaSettings.bodyColor = bodyColorPicker.valuecolor;
            if (personaSettings != null) personaSettings.eyeColor = eyeColorPicker.valuecolor;
        }

        private void UpdateReadyUpLabel()
        {
            this.totalClientsReadiedUpOnPage.text = $"Ready: {arena.clientsAreReadiedUp} / {OnlineManager.players.Count}";

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
                if (arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition + 1].id.name] > 3 && ModManager.MSC)
                {
                    classButtons[holdPlayerPosition].portrait.fileName = "MultiplayerPortrait" + "41-" + allSlugs[arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition + 1].id.name]];

                }
                else
                {
                    classButtons[holdPlayerPosition].portrait.fileName = "MultiplayerPortrait" + arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition + 1].id.name] + "1";
                }
                classButtons[holdPlayerPosition].portrait.LoadFile();
                classButtons[holdPlayerPosition].portrait.sprite.SetElementByName(classButtons[holdPlayerPosition].portrait.fileName);

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
                if (arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition - 1].id.name] > 3 && ModManager.MSC)
                {
                    classButtons[holdPlayerPosition].portrait.fileName = "MultiplayerPortrait" + "41-" + allSlugs[arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition - 1].id.name]];

                }
                else
                {
                    classButtons[holdPlayerPosition].portrait.fileName = "MultiplayerPortrait" + arena.playersInLobbyChoosingSlugs[OnlineManager.players[currentPlayerPosition - 1].id.name] + "1";
                }
                classButtons[holdPlayerPosition].portrait.LoadFile();
                classButtons[holdPlayerPosition].portrait.sprite.SetElementByName(classButtons[holdPlayerPosition].portrait.fileName);

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
