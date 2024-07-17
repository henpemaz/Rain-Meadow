using HUD;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.GameModes;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow
{
    public class ArenaLobbyMenu : SmartMenu
    {
        public SymbolButton infoButton;

        private ArenaClientSettings personaSettings;
        private static float num = 120f;
        private static float num2 = 0f;
        private static float num3 = num - num2;
        private List<string> sharedPlayList = new List<string>();

        OpSliderTick playerCountSlider;
        UIelementWrapper playerCountWrapper;

        int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // been using 1360 as ref

        SimpleButton[] playerClassButtons;
        PlayerJoinButton[] playerJoinButtons;
        MultiplayerMenu mm;

        public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;

        public ArenaLobbyMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ArenaLobbyMenu)
        {
            RainMeadow.DebugMe();

            if (OnlineManager.lobby == null) throw new InvalidOperationException("lobby is null");

            manager.arenaSetup = new ArenaSetup(manager)
            {
                currentGameType = ArenaSetup.GameTypeID.Competitive
            };

            FakeInitializeMultiplayerMenu();

            UninitializeInheritedScene();

            BuildLayout();

            BindSettings();

            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;


        }

        void UninitializeInheritedScene()
        {
            mainPage.subObjects.Remove(this.backObject);

            mainPage.subObjects.Add(this.backObject = new SimplerButton(mm, pages[0], "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
            (backObject as SimplerButton).OnClick += (btn) =>
            {
                manager.RequestMainProcessSwitch(this.backTarget);
            };
        }


        void FakeInitializeMultiplayerMenu()
        {
            mm = (MultiplayerMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(MultiplayerMenu));
            mm.ID = ProcessManager.ProcessID.MultiplayerMenu;
            mm.manager = manager;
            mm.currentGameType = mm.nextGameType = ArenaSetup.GameTypeID.Competitive;
            mm.pages = pages;
            mm.mySoundLoopName = mySoundLoopName;
            mm.mySoundLoopID = mySoundLoopID;

            UnlockAndLoadLevels();


            // very convenient
            mm.arenaSettingsInterface = new ArenaSettingsInterface(mm, pages[0]);
            mm.arenaSettingsInterface.pos += Vector2.down * 40;
            pages[0].subObjects.Add(mm.arenaSettingsInterface);
            

            mm.levelSelector = new LevelSelector(mm, pages[0], false);
            pages[0].subObjects.Add(mm.levelSelector);
            mm.init = true;




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

        void BuildLayout()
        {
            scene.AddIllustration(new MenuIllustration(mm, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
            scene.AddIllustration(new MenuIllustration(mm, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
            scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.shader = manager.rainWorld.Shaders["MenuText"];

            mm.playButton = CreateButton("START", new Vector2(ScreenWidth - 304, 50), new Vector2(110, 30), self => StartGame());

            infoButton = new SymbolButton(mm, pages[0], "Menu_InfoI", "INFO", new Vector2(1142f, 624f));
            pages[0].subObjects.Add(infoButton);

            BuildPlayerSlots();
            AddAbovePlayText();
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

            AddPlayerButtons();

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

        public void InitializeSitting(List<string> hostPlaylist)
        {
            manager.arenaSitting = new ArenaSitting(mm.GetGameTypeSetup, mm.multiplayerUnlocks);

            manager.arenaSitting.AddPlayer(0); // placeholder add player
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
                sharedPlayList = manager.arenaSitting.levelPlaylist;
                RainMeadow.Debug("COUNT OF HOST MAPS:" + sharedPlayList.Count);

            }

            // Client retrieves playlist
            else
            {
                manager.arenaSitting.levelPlaylist = hostPlaylist;
                RainMeadow.Debug("COUNT OF MAPS CLIENT:" + hostPlaylist.Count);




            }



        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

            if (!OnlineManager.lobby.isOwner) return;



            foreach (OnlinePlayer player in OnlineManager.players)
            {

                if (OnlineManager.lobby.isOwner)
                {
                    // Give the owner a head start
                    RPCs.StartArena(sharedPlayList);

                    if (!player.isMe)
                    {
                        player.InvokeRPC(RPCs.StartArena, sharedPlayList);
                    }
                }


            }

        }

        public override void Update()
        {

            mm.Update();

            if (mm.GetGameTypeSetup.playList.Count * mm.GetGameTypeSetup.levelRepeats > 0)
            {
                mm.playButton.buttonBehav.greyedOut = false;
            }
            else
            {
                mm.playButton.buttonBehav.greyedOut = OnlineManager.lobby.isAvailable;

            }

        }


        public override void Singal(MenuObject sender, string message)
        {
            if (mm.requestingControllerConnections)
            {
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                if (manager.rainWorld.GetPlayerSigningIn(i))
                {
                    return;
                }
            }

            switch (message)
            {
                case "INFO":
                    if (mm.infoWindow != null)
                    {
                        mm.infoWindow.wantToGoAway = true;
                        PlaySound(SoundID.MENU_Remove_Level);
                    }
                    else
                    {
                        mm.infoWindow = new InfoWindow(mm, sender, new Vector2(0f, 0f));
                        sender.subObjects.Add(mm.infoWindow);
                        PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    }
                    break;
            }

            if (!ModManager.MSC)
            {
                return;
            }

            for (int num6 = 0; num6 < playerClassButtons.Length; num6++)
            {
                if (message == "CLASSCHANGE" + num6)
                {
                    mm.GetArenaSetup.playerClass[num6] = mm.NextClass(mm.GetArenaSetup.playerClass[0]);
                    playerClassButtons[num6].menuLabel.text = Translate(SlugcatStats.getSlugcatName(mm.GetArenaSetup.playerClass[0]));
                    playerJoinButtons[num6].portrait.fileName = mm.ArenaImage(mm.GetArenaSetup.playerClass[0], 0);
                    playerJoinButtons[num6].portrait.LoadFile();
                    playerJoinButtons[num6].portrait.sprite.SetElementByName(playerJoinButtons[0].portrait.fileName);
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
            }

            base.Singal(sender, message);
        }

        public override string UpdateInfoText()
        {
            //mm.UpdateInfoText();

            return base.UpdateInfoText();
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
            personaSettings.bodyColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);

            personaSettings.eyeColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);

        }

        private void AddPlayerButtons()
        {
            mm.GetArenaSetup.playerClass = new SlugcatStats.Name[4];

            for (int i = 0; i < mm.GetArenaSetup.playerClass.Length; i++)
            {
                mm.GetArenaSetup.playerClass[i] = SlugcatStats.Name.White;
            }

            playerClassButtons = new SimplerButton[OnlineManager.players.Count];

            for (int k = 0; k < playerClassButtons.Length; k++)
            {
                string name = OnlineManager.players[k].id.name;

                if (ModManager.MSC)
                {
                    name = OnlineManager.players[k].id.name;
                }

                playerClassButtons[k] = new SimplerButton(mm, pages[0], name, new Vector2(600f + k * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * playerClassButtons.Length, 40f), new Vector2(num - 20f, 30f));

                (playerClassButtons[k] as SimplerButton).OnClick += (_) =>
                {

                    mm.GetArenaSetup.playerClass[k] = mm.GetArenaSetup.playerClass[0];
                    //mm.NextClass(mm.GetArenaSetup.playerClass[0]);
                    playerClassButtons[k].menuLabel.text = OnlineManager.players[0].id.name;

                    playerJoinButtons[k].portrait.fileName = playerJoinButtons[0].portrait.fileName;
                    mm.ArenaImage(mm.GetArenaSetup.playerClass[0], 0);
                    // playerJoinButtons[0].portrait.LoadFile();
                    // playerJoinButtons[0].portrait.sprite.SetElementByName(playerJoinButtons[0].portrait.fileName);
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

                };




                if (!ModManager.MSC)
                {
                    playerClassButtons[k].buttonBehav.greyedOut = false;
                }

                pages[0].subObjects.Add(playerClassButtons[k]);
            }

            playerJoinButtons = new PlayerJoinButton[OnlineManager.players.Count];
            for (int l = 0; l < playerJoinButtons.Length; l++)
            {
                playerJoinButtons[l] = new PlayerJoinButton(mm, pages[0], new Vector2(600f + l * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f) * playerJoinButtons.Length, 40f), l);
                playerJoinButtons[l].buttonBehav.greyedOut = false;

                if (ModManager.MSC)
                {
                    playerJoinButtons[l].portrait.fileName = mm.ArenaImage(manager.arenaSetup.playerClass[0], 0);
                    playerJoinButtons[l].portrait.LoadFile();
                    playerJoinButtons[l].portrait.sprite.SetElementByName(playerJoinButtons[0].portrait.fileName);
                    MutualVerticalButtonBind(playerClassButtons[l], playerJoinButtons[l]);
                }

                pages[0].subObjects.Add(playerJoinButtons[l]);
            }

        }

        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            RainMeadow.Debug(players);
            for (int i = 0; i < playerClassButtons.Length; i++)
            {
                var playerbtn = playerClassButtons[i];
                playerbtn.RemoveSprites();
                mainPage.RemoveSubObject(playerbtn);
            }

            for (int i = 0; i < playerJoinButtons.Length; i++)
            {
                var playerbtn = playerJoinButtons[i];
                playerbtn.RemoveSprites();
                mainPage.RemoveSubObject(playerbtn);
            }

            AddPlayerButtons();


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