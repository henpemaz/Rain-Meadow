using HUD;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.GameModes;
using RWCustom;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Kittehface.Framework20;
using System.Linq;

namespace RainMeadow
{
    public class ArenaLobbyMenu : MultiplayerMenu
    {
        private ArenaCompetitiveGameMode arena => (ArenaCompetitiveGameMode)OnlineManager.lobby.gameMode;

        private ArenaClientSettings personaSettings;
        private static float num = 120f;
        private static float num2 = 0f;
        private static float num3 = num - num2;
        private List<string> sharedPlayList = new List<string>();
        private OpTinyColorPicker bodyColorPicker;
        private OpTinyColorPicker eyeColorPicker;
        public UIelementWrapper bodyColor;
        public UIelementWrapper eyeColor;
        public bool clientReadiedUp = false;


        int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // been using 1360 as ref

        public SimpleButton[] usernameButtons;
        public ArenaOnlinePlayerJoinButton meClassButton;

        public ArenaOnlinePlayerJoinButton[] classButtons;
        private bool flushArenaSittingForWaitingClients = false;

        public ArenaLobbyMenu(ProcessManager manager) : base(manager)
        {
            RainMeadow.DebugMe();

            if (OnlineManager.lobby == null) throw new InvalidOperationException("lobby is null");

            manager.arenaSetup = new ArenaSetup(manager)
            {
                currentGameType = ArenaSetup.GameTypeID.Competitive,
                savFilePath = null,


            };
            OverrideMultiplayerMenu();
            BindSettings();
            BuildLayout();
            ArenaHelpers.ResetReadyUpLogic(arena, this);

            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
            //SetupCharacterCustomization();


        }


        void RemoveExcessArenaObjects()
        {
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
            this.personaSettings = (ArenaClientSettings)OnlineManager.lobby.gameMode.clientSettings;
            personaSettings.bodyColor = RainMeadow.rainMeadowOptions.BodyColor.Value;
            personaSettings.eyeColor = RainMeadow.rainMeadowOptions.EyeColor.Value;
            personaSettings.playingAs = SlugcatStats.Name.White;

        }

        void BuildLayout()
        {
            BuildPlayerSlots();
            AddAbovePlayText();

            if (this.levelSelector != null)
            {

                if (this.levelSelector.levelsPlaylist.levelItems.Count > 0 && OnlineManager.lobby.isOwner)
                {
                    this.levelSelector.levelsPlaylist.clearAllCounter = 1;
                }
                // TODO: clients are finnicky with when they add or remove versus the host
                if (!OnlineManager.lobby.isOwner)
                {

                    for (int i = 0; i < this.levelSelector.levelsPlaylist.levelItems.Count; i++)
                    {

                        this.GetGameTypeSetup.playList.RemoveAt(i);
                        this.levelSelector.levelsPlaylist.RemoveLevelItem(new Menu.LevelSelector.LevelItem(this, this.levelSelector.levelsPlaylist, this.levelSelector.levelsPlaylist.levelItems[i].name));
                        this.levelSelector.levelsPlaylist.ScrollPos = this.levelSelector.levelsPlaylist.LastPossibleScroll;
                        this.levelSelector.levelsPlaylist.ConstrainScroll();


                    }
                    foreach (var level in arena.playList)
                    {
                        this.GetGameTypeSetup.playList.Add(level);
                        this.levelSelector.levelsPlaylist.AddLevelItem(new Menu.LevelSelector.LevelItem(this, this.levelSelector.levelsPlaylist, level));
                        this.levelSelector.levelsPlaylist.ScrollPos = this.levelSelector.levelsPlaylist.LastPossibleScroll;
                        this.levelSelector.levelsPlaylist.ConstrainScroll();
                    }
                }

            }
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
            AddOtherPlayerClassButtons();
            AddOtherUsernameButtons();


            this.GetArenaSetup.playersJoined[0] = true; // host should be part of game
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

            if (arena.clientsAreReadiedUp != OnlineManager.players.Count && arena.isInGame)
            {
                return;
            }


            if (!arena.allPlayersReadyLockLobby)
            {
                arena.clientsAreReadiedUp++;

                if (OnlineManager.players.Count > 1)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeRPC(ArenaRPCs.Arena_NotifyLobbyReadyUp, OnlineManager.mePlayer.id.name, arena.clientsAreReadiedUp);

                        }
                    }
                    this.playButton.menuLabel.text = "Waiting for others...";
                    this.playButton.inactive = true;
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

            if (this.playButton != null)
            {

                if (OnlineManager.players.Count == 1)
                {
                    arena.allPlayersReadyLockLobby = true;
                    this.playButton.menuLabel.text = "ENTER";
                    this.playButton.inactive = false;
                    this.meClassButton.buttonBehav.greyedOut = false;

                }

                if (arena.clientsAreReadiedUp == OnlineManager.players.Count && OnlineManager.players.Count > 1)
                {
                    arena.allPlayersReadyLockLobby = true;
                    this.playButton.menuLabel.text = "ENTER";

                    if (OnlineManager.lobby.isOwner)
                    {
                        this.playButton.inactive = false;
                    }

                    if (!OnlineManager.lobby.isOwner)
                    {
                        if (!arena.isInGame)
                        {
                            this.playButton.inactive = true;
                        }
                        else
                        {
                            this.playButton.inactive = false;
                        }

                    }

                }



                if (arena.clientsAreReadiedUp != OnlineManager.players.Count && arena.isInGame)
                {
                    this.playButton.inactive = true;
                    this.playButton.menuLabel.text = "GAME IN SESSION";
                }

                if (arena.returnToLobby && !flushArenaSittingForWaitingClients)
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this);
                    flushArenaSittingForWaitingClients = true;
                }
                if (!OnlineManager.lobby.isOwner)
                {
                    if (this.GetGameTypeSetup.playList.Count * this.GetGameTypeSetup.levelRepeats >= 0)
                    {
                        if (this.abovePlayButtonLabel != null)
                        {
                            this.abovePlayButtonLabel.RemoveSprites();
                            this.pages[0].RemoveSubObject(this.abovePlayButtonLabel);
                        }

                        this.playButton.buttonBehav.greyedOut = false;
                    }


                }
                else
                {

                    if (this.GetGameTypeSetup.playList.Count * this.GetGameTypeSetup.levelRepeats > 0)
                    {
                        this.playButton.buttonBehav.greyedOut = false;
                    }
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
                infoWindow.label.text = "Welcome to Arena Online! All players must ready up in the lobby before combat can begin.";
            }
        }



        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            if (RainMeadow.isArenaMode(out var _))
            {
                RainMeadow.Debug(players);
                for (int i = usernameButtons.Length - 1; i >= 1; i--)
                {
                    var playerbtn = usernameButtons[i];
                    playerbtn.RemoveSprites();
                    //mainPage.RemoveSubObject(playerbtn);
                }

                for (int i = classButtons.Length - 1; i >= 1; i--)
                {
                    var playerbtn = classButtons[i];
                    playerbtn.RemoveSprites();
                    //mainPage.RemoveSubObject(playerbtn);
                }
                if (OnlineManager.players.Count > 1)
                {
                    AddOtherPlayerClassButtons();
                    AddOtherUsernameButtons();
                }

                if (this != null)
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this);
                }

            }

        }


        private void AddMeClassButton() // doing unique stuff with player 0 so less annoying this way
        {

            meClassButton = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f), 40f), 0);
            meClassButton.buttonBehav.greyedOut = false;
            meClassButton.readyForCombat = true;
            var currentColorIndex = 0;
            meClassButton.OnClick += (_) =>
            {

                currentColorIndex = (currentColorIndex + 1) % this.GetArenaSetup.playerClass.Length;

                this.GetArenaSetup.playerClass[currentColorIndex] = this.GetArenaSetup.playerClass[currentColorIndex];

                if (currentColorIndex > 3 && ModManager.MSC)
                {
                    meClassButton.portrait.fileName = "MultiplayerPortrait" + "41-" + this.GetArenaSetup.playerClass[currentColorIndex];

                }
                else
                {
                    meClassButton.portrait.fileName = "MultiplayerPortrait" + currentColorIndex + "1";
                }


                meClassButton.portrait.LoadFile();
                meClassButton.portrait.sprite.SetElementByName(meClassButton.portrait.fileName);
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

                personaSettings.playingAs = this.GetArenaSetup.playerClass[currentColorIndex];

                if (OnlineManager.players.Count > 1)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeRPC(ArenaRPCs.Arena_NotifyClassChange, OnlineManager.mePlayer.id.name, currentColorIndex);

                        }
                    }
                }



            };
            pages[0].subObjects.Add(meClassButton);
        }

        private void AddMeUsername()
        {

            var SlugList = ArenaHelpers.AllSlugcats();
            this.GetArenaSetup.playerClass = SlugList.ToArray();

            var myUsernameButton = new SimplerButton[1];


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

            myUsernameButton[0] = new SimplerButton(this, pages[0], name, new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * myUsernameButton.Length, 40f), new Vector2(num - 20f, 30f));
            (myUsernameButton[0] as SimplerButton).OnClick += (_) =>
            {
                string url = $"https://steamcommunity.com/profiles/{playerId}";
                SteamFriends.ActivateGameOverlayToWebPage(url);
            };

            myUsernameButton[0].buttonBehav.greyedOut = false;


            pages[0].subObjects.Add(myUsernameButton[0]);



        }
        private void AddOtherPlayerClassButtons()
        {


            classButtons = new ArenaOnlinePlayerJoinButton[OnlineManager.players.Count];
            if (OnlineManager.players.Count > 1)
            {
                for (int l = 1; l < classButtons.Length; l++)
                {

                    classButtons[l] = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + l * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f) * classButtons.Length, 40f), l);
                    classButtons[l].buttonBehav.greyedOut = true;
                    classButtons[l].portraitBlack = Custom.LerpAndTick(classButtons[l].portraitBlack, 1f, 0.06f, 0.05f);
                    classButtons[l].portrait.fileName = "MultiplayerPortrait" + "01";
                    classButtons[l].portrait.LoadFile();
                    classButtons[l].portrait.sprite.SetElementByName(classButtons[l].portrait.fileName);

                    pages[0].subObjects.Add(classButtons[l]);
                }
            }
        }

        public void AddOtherUsernameButtons()
        {
            var SlugList = ArenaHelpers.AllSlugcats();
            this.GetArenaSetup.playerClass = SlugList.ToArray();

            usernameButtons = new SimplerButton[OnlineManager.players.Count];

            if (OnlineManager.players.Count > 1)
            {
                for (int k = 1; k < usernameButtons.Length; k++)
                {

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

            bodyColorPicker = new OpTinyColorPicker(this.pages[0].menu, new Vector2(705, 353), "FFFFFF");
            var tabWrapper = new MenuTabWrapper(this, this.pages[0]);
            bodyColor = new UIelementWrapper(tabWrapper, bodyColorPicker);
            bodyColorPicker.OnValueChangedEvent += ColorPicker_OnValueChangedEvent;

            eyeColorPicker = new OpTinyColorPicker(this.pages[0].menu, new Vector2(810, 353), "000000");
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

        /// TODO: Share level selection visibly with client
        /*        public void ClearAndRefreshLevelSelect()
                {

                    for (int i = 0; i < manager.arenaSitting.levelPlaylist.Count; i++)
                    {
                        this.levelSelector.LevelFromPlayList(i);

                    }

                    foreach (var level in manager.arenaSitting.levelPlaylist)
                    {

                        this.levelSelector.LevelToPlaylist(level);


                    }

                }*/


    }
}
