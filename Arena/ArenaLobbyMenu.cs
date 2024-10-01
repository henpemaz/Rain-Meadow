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
using System.Runtime.InteropServices;
using System.Linq;

namespace RainMeadow
{
    public class ArenaLobbyMenu : SmartMenu
    {
        public SymbolButton infoButton;
        private ArenaCompetitiveGameMode arena => (ArenaCompetitiveGameMode)OnlineManager.lobby.gameMode;

        private ArenaClientSettings personaSettings;
        private static float num = 120f;
        private static float num2 = 0f;
        private static float num3 = num - num2;
        private List<string> sharedPlayList = new List<string>();
        private OpTinyColorPicker bodyColorPicker;
        private OpTinyColorPicker eyeColorPicker;
        OpSliderTick playerCountSlider;
        UIelementWrapper playerCountWrapper;
        public UIelementWrapper bodyColor;
        public UIelementWrapper eyeColor;
        public bool clientReadiedUp = false;


        int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // been using 1360 as ref

        public SimpleButton[] usernameButtons;
        public ArenaOnlinePlayerJoinButton meClassButton;

        public ArenaOnlinePlayerJoinButton[] classButtons;
        public MultiplayerMenu mm;
        private bool flushArenaSittingForWaitingClients = false;

        public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;

        public ArenaLobbyMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ArenaLobbyMenu)
        {
            RainMeadow.DebugMe();

            if (OnlineManager.lobby == null) throw new InvalidOperationException("lobby is null");

            manager.arenaSetup = new ArenaSetup(manager)
            {
                currentGameType = ArenaSetup.GameTypeID.Competitive,
                savFilePath = null,


            };
            FakeInitializeMultiplayerMenu();
            UninitializeInheritedScene();
            BindSettings();
            BuildLayout();
            ArenaHelpers.ResetReadyUpLogic(arena, this);

            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
            //SetupCharacterCustomization();

        }

        void UninitializeInheritedScene()
        {
            mainPage.subObjects.Remove(this.backObject);

            mainPage.subObjects.Add(this.backObject = new SimplerButton(mm, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += (btn) =>
            {
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
            };
        }


        void FakeInitializeMultiplayerMenu()
        {
            mm = (MultiplayerMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(MultiplayerMenu));
            mm.ID = ProcessManager.ProcessID.MultiplayerMenu;
            mm.manager = manager;
            mm.currentGameType = mm.nextGameType = ArenaSetup.GameTypeID.Competitive;


            mm.GetGameTypeSetup.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;
            mm.GetGameTypeSetup.rainWhenOnePlayerLeft = false; // TODO:  Hook this to update logic due to level switching if we want it
            mm.GetGameTypeSetup.savingAndLoadingSession = false;
            mm.GetGameTypeSetup.saveCreatures = false;

            mm.pages = pages;

            mm.mySoundLoopName = mySoundLoopName;
            mm.mySoundLoopID = mySoundLoopID;

            UnlockAndLoadLevels();


            // TODO: very convenient, but can't use custom colors with this impl
            mm.arenaSettingsInterface = new ArenaSettingsInterface(mm, pages[0]);
            mm.arenaSettingsInterface.pos += Vector2.down * 40;
            //mm.arenaSettingsInterface.rainTimer.CheckedButton -------- TODO: sync this

            pages[0].subObjects.Add(mm.arenaSettingsInterface);



            mm.levelSelector = new LevelSelector(mm, pages[0], false);
            pages[0].subObjects.Add(mm.levelSelector);

            mm.init = true;
        }



        void BuildLayout()
        {
            scene.AddIllustration(new MenuIllustration(mm, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
            scene.AddIllustration(new MenuIllustration(mm, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
            scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.shader = manager.rainWorld.Shaders["MenuText"];
            mm.playButton = CreateButton("READY?", new Vector2(ScreenWidth - 304, 50), new Vector2(110, 30), self => StartGame());


            BuildPlayerSlots();
            AddAbovePlayText();
        }

        void UnlockAndLoadLevels()
        {
            string[] array = AssetManager.ListDirectory("Levels");

            mm.allLevels = new List<string>();

            for (int j = 0; j < array.Length; j++)
            {
                if (array[j].Substring(array[j].Length - 4, 4) == ".txt" && array[j].Substring(array[j].Length - 13, 13) != "_settings.txt" && array[j].Substring(array[j].Length - 10, 10) != "_arena.txt" && !array[j].Contains("unlockall"))
                {
                    string[] array2 = array[j].Substring(0, array[j].Length - 4).Split(Path.DirectorySeparatorChar);
                    mm.allLevels.Add(array2[array2.Length - 1]);
                }
            }

            mm.multiplayerUnlocks = new MultiplayerUnlocks(manager.rainWorld.progression, mm.allLevels);

            mm.allLevels.Sort((A, B) => mm.multiplayerUnlocks.LevelListSortString(A).CompareTo(mm.multiplayerUnlocks.LevelListSortString(B)));
            mm.thumbsToBeLoaded = new List<string>();
            mm.loadedThumbTextures = new List<string>();
            for (int k = 0; k < mm.allLevels.Count; k++)
            {
                mm.thumbsToBeLoaded.Add(mm.allLevels[k]);
            }

        }

        SimplerButton CreateButton(string text, Vector2 pos, Vector2 size, Action<SimplerButton>? clicked = null, Page? page = null)
        {
            page ??= pages[0];
            var b = new SimplerButton(mm, page, text, pos, size);
            if (clicked != null) b.OnClick += clicked;
            page.subObjects.Add(b);
            return b;
        }




        void BuildPlayerSlots()
        {
            float num = 120f;
            float num2 = 0f;
            if (base.CurrLang == InGameTranslator.LanguageID.German)
            {
                num = 140f;
                num2 = 15f;
            }

            float num3 = num - num2;

            playerCountSlider = new OpSliderTick(new Configurable<int>(2, new ConfigAcceptableRange<int>(1, 31)), new Vector2(710, 550), 450);
            pages[0].subObjects.Add(playerCountWrapper = new UIelementWrapper(tabWrapper, playerCountSlider));

            AddMeClassButton();
            AddMeUsername();
            AddOtherPlayerClassButtons();
            AddOtherUsernameButtons();


            mm.GetArenaSetup.playersJoined[0] = true; // host should be part of game
        }

        void AddAbovePlayText()
        {
            mm.abovePlayButtonLabel = new MenuLabel(mm, pages[0], "", mm.playButton.pos + new Vector2((0f - mm.playButton.size.x) / 2f + 0.01f, 50.01f), new Vector2(mm.playButton.size.x, 20f), bigText: false);
            mm.abovePlayButtonLabel.label.alignment = FLabelAlignment.Left;
            mm.abovePlayButtonLabel.label.color = MenuRGB(MenuColors.DarkGrey);
            pages[0].subObjects.Add(mm.abovePlayButtonLabel);
            if (manager.rainWorld.options.ScreenSize.x < 1280f)
            {
                mm.abovePlayButtonLabel.label.alignment = FLabelAlignment.Right;
                mm.abovePlayButtonLabel.pos.x = mm.playButton.pos.x + 55f;
            }
        }

        public void InitializeSitting()
        {
            manager.arenaSitting = new ArenaSitting(mm.GetGameTypeSetup, mm.multiplayerUnlocks);



            manager.arenaSitting.levelPlaylist = new List<string>();

            if (mm.GetGameTypeSetup.shufflePlaylist)
            {
                List<string> list2 = new List<string>();
                for (int l = 0; l < mm.GetGameTypeSetup.playList.Count; l++)
                {
                    list2.Add(mm.GetGameTypeSetup.playList[l]);
                }

                while (list2.Count > 0)
                {
                    int index2 = UnityEngine.Random.Range(0, list2.Count);
                    for (int m = 0; m < mm.GetGameTypeSetup.levelRepeats; m++)
                    {
                        manager.arenaSitting.levelPlaylist.Add(list2[index2]);
                    }

                    list2.RemoveAt(index2);
                }
            }
            else
            {
                for (int n = 0; n < mm.GetGameTypeSetup.playList.Count; n++)
                {
                    for (int num = 0; num < mm.GetGameTypeSetup.levelRepeats; num++)
                    {
                        manager.arenaSitting.levelPlaylist.Add(mm.GetGameTypeSetup.playList[n]);
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
                            player.InvokeRPC(RPCs.Arena_NotifyLobbyReadyUp, OnlineManager.mePlayer.id.name, arena.clientsAreReadiedUp);

                        }
                    }
                    mm.playButton.menuLabel.text = "Waiting for others...";
                    mm.playButton.inactive = true;
                }

                return;
            }

            if (!OnlineManager.lobby.isOwner && !arena.isInGame)
            {
                return;
            }
            InitializeSitting();
            ArenaHelpers.SetupOnlineArenaStting(arena, mm.manager);
            mm.manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            //mm.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;

            // temp
            UserInput.SetUserCount(OnlineManager.players.Count);
            UserInput.SetForceDisconnectControllers(forceDisconnect: false);
            mm.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);

        }

        public override void Update()
        {
            // base.Update();
            mm.Update();


            if (meClassButton != null)
            {
                meClassButton.readyForCombat = true;

            }


            if (mm.playButton != null)
            {

                if (OnlineManager.players.Count == 1)
                {
                    arena.allPlayersReadyLockLobby = true;
                    mm.playButton.menuLabel.text = "ENTER";
                    mm.playButton.inactive = false;

                }

                if (arena.clientsAreReadiedUp == OnlineManager.players.Count && OnlineManager.players.Count > 1)
                {
                    arena.allPlayersReadyLockLobby = true;
                    mm.playButton.menuLabel.text = "ENTER";

                    if (OnlineManager.lobby.isOwner)
                    {
                        mm.playButton.inactive = false;
                    }

                    if (!OnlineManager.lobby.isOwner)
                    {
                        if (!arena.isInGame)
                        {
                            mm.playButton.inactive = true;
                        }
                        else
                        {
                            mm.playButton.inactive = false;
                        }

                    }

                }



                if (arena.clientsAreReadiedUp != OnlineManager.players.Count && arena.isInGame)
                {
                    mm.playButton.inactive = true;
                    mm.playButton.menuLabel.text = "GAME IN SESSION";
                }

                if (arena.returnToLobby && !flushArenaSittingForWaitingClients)
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this);
                    flushArenaSittingForWaitingClients = true;
                }


                if (mm.GetGameTypeSetup.playList.Count * mm.GetGameTypeSetup.levelRepeats > 0)
                {
                    mm.playButton.buttonBehav.greyedOut = false;
                }
                else
                {
                    mm.playButton.buttonBehav.greyedOut = OnlineManager.lobby.isAvailable;

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

        private void BindSettings()
        {
            this.personaSettings = (ArenaClientSettings)OnlineManager.lobby.gameMode.clientSettings;
            personaSettings.bodyColor = RainMeadow.rainMeadowOptions.BodyColor.Value;
            personaSettings.eyeColor = RainMeadow.rainMeadowOptions.EyeColor.Value;
            personaSettings.playingAs = SlugcatStats.Name.White;

        }


        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            RainMeadow.Debug(players);
            for (int i = usernameButtons.Length - 1; i >= 1; i--)
            {
                var playerbtn = usernameButtons[i];
                playerbtn.RemoveSprites();
                mainPage.RemoveSubObject(playerbtn);
            }

            for (int i = classButtons.Length - 1; i >= 1; i--)
            {
                var playerbtn = classButtons[i];
                playerbtn.RemoveSprites();
                mainPage.RemoveSubObject(playerbtn);
            }
            AddOtherPlayerClassButtons();
            AddOtherUsernameButtons();

            if (this != null)
            {
                ArenaHelpers.ResetReadyUpLogic(arena, this);
            }

        }


        private void AddMeClassButton() // doing unique stuff with player 0 so less annoying this way
        {

            meClassButton = new ArenaOnlinePlayerJoinButton(mm, pages[0], new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f), 40f), 0);
            meClassButton.buttonBehav.greyedOut = false;
            // TODO: Figure out what greys out the image so we can use as visual ref for readying up
            var currentColorIndex = 0;
            meClassButton.OnClick += (_) =>
            {

                currentColorIndex = (currentColorIndex + 1) % mm.GetArenaSetup.playerClass.Length;

                mm.GetArenaSetup.playerClass[currentColorIndex] = mm.GetArenaSetup.playerClass[currentColorIndex];

                if (currentColorIndex > 3 && ModManager.MSC)
                {
                    meClassButton.portrait.fileName = "MultiplayerPortrait" + "41-" + mm.GetArenaSetup.playerClass[currentColorIndex];

                }
                else
                {
                    meClassButton.portrait.fileName = "MultiplayerPortrait" + currentColorIndex + "1";
                }


                meClassButton.portrait.LoadFile();
                meClassButton.portrait.sprite.SetElementByName(meClassButton.portrait.fileName);
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

                personaSettings.playingAs = mm.GetArenaSetup.playerClass[currentColorIndex];

                if (OnlineManager.players.Count > 1)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeRPC(RPCs.Arena_NotifyClassChange, OnlineManager.mePlayer.id.name, currentColorIndex);

                        }
                    }
                }



            };
            pages[0].subObjects.Add(meClassButton);
        }

        private void AddMeUsername()
        {

            var SlugList = ArenaHelpers.AllSlugcats();
            mm.GetArenaSetup.playerClass = SlugList.ToArray();

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

            myUsernameButton[0] = new SimplerButton(mm, pages[0], name, new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * myUsernameButton.Length, 40f), new Vector2(num - 20f, 30f));
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

                    classButtons[l] = new ArenaOnlinePlayerJoinButton(mm, pages[0], new Vector2(600f + l * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f) * classButtons.Length, 40f), l);
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
            mm.GetArenaSetup.playerClass = SlugList.ToArray();

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

                    usernameButtons[k] = new SimplerButton(mm, pages[0], name, new Vector2(600f + k * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * usernameButtons.Length, 40f), new Vector2(num - 20f, 30f));
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

            bodyColorPicker = new OpTinyColorPicker(this, new Vector2(705, 353), "FFFFFF");
            bodyColor = new UIelementWrapper(tabWrapper, bodyColorPicker);
            bodyColorPicker.OnValueChangedEvent += ColorPicker_OnValueChangedEvent;

            eyeColorPicker = new OpTinyColorPicker(this, new Vector2(810, 353), "000000");
            eyeColor = new UIelementWrapper(tabWrapper, eyeColorPicker);
            eyeColorPicker.OnValueChangedEvent += ColorPicker_OnValueChangedEvent;

            this.mainPage.subObjects.Add(bodyColor);
            this.mainPage.subObjects.Add(eyeColor);


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
                        mm.levelSelector.LevelFromPlayList(i);

                    }

                    foreach (var level in manager.arenaSitting.levelPlaylist)
                    {

                        mm.levelSelector.LevelToPlaylist(level);


                    }

                }*/


    }
}
