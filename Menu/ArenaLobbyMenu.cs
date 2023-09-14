using Kittehface.Framework20;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
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
        public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;
        public ArenaLobbyMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ArenaLobbyMenu)
        {
            RainMeadow.DebugMe();

            if (OnlineManager.lobby == null) throw new InvalidOperationException("lobby is null");
            OnlineManager.lobby.OnLobbyAvailable += OnLobbyAvailable;

            manager.arenaSetup = new ArenaSetup(manager)
            {
                currentGameType = ArenaSetup.GameTypeID.Competitive
            };

            FakeInitializeMultiplayerMenu();

            UninitializeInheritedScene();

            BuildLayout();
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

        MultiplayerMenu mm;

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

            mm.levelSelector = new LevelSelector(mm, pages[0], false);

            pages[0].subObjects.Add(mm.arenaSettingsInterface);
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

        #region Menu Builders
        SimplerButton CreateButton(string text, Vector2 pos, Vector2 size, bool waitLobbyAvailability, Action<SimplerButton>? clicked = null, Page? page = null)
        {
            page = page ?? pages[0];

            var b = new SimplerButton(mm, page, text, pos, size);

            if (waitLobbyAvailability && !OnlineManager.lobby.isAvailable)
            {
                b.buttonBehav.greyedOut = true;
                enableOnLobbyAvailable.Add(b);
            }

            if (clicked != null) b.OnClick += clicked;

            page.subObjects.Add(b);

            return b;
        }

        #endregion

        int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // been using 1360 as ref
        void BuildLayout()
        {
            scene.AddIllustration(new MenuIllustration(mm, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
            scene.AddIllustration(new MenuIllustration(mm, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
            scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.shader = manager.rainWorld.Shaders["MenuText"];

            mm.playButton = CreateButton("START", new Vector2(ScreenWidth - 304, 50), new Vector2(110, 30), true, self => StartGame());

            infoButton = new SymbolButton(mm, pages[0], "Menu_InfoI", "INFO", new Vector2(1142f, 624f));
            pages[0].subObjects.Add(infoButton);

            BuildPlayerSlots();
            AddAbovePlayText();
        }

        OpSliderTick playerCountSlider;
        UIelementWrapper playerCountWrapper;

        public SymbolButton infoButton;

        SimpleButton[] playerClassButtons;
        PlayerJoinButton[] playerJoinButtons;
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

            playerCountSlider = new OpSliderTick(new Configurable<int>(2, new ConfigAcceptableRange<int>(1, 4)), new Vector2(710, 550), 450);
            pages[0].subObjects.Add(playerCountWrapper = new UIelementWrapper(tabWrapper, playerCountSlider));

            playerClassButtons = new SimpleButton[4];
            for (int k = 0; k < playerClassButtons.Length; k++)
            {
                string name = "Survivor";

                if (ModManager.MSC)
                {
                    name = Translate(SlugcatStats.getSlugcatName(manager.arenaSetup.playerClass[k]));
                }

                playerClassButtons[k] = new SimpleButton(mm, pages[0], name, "CLASSCHANGE" + k, new Vector2(600f + k * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * playerClassButtons.Length, 40f), new Vector2(num - 20f, 30f));

                if (!ModManager.MSC)
                {
                    playerClassButtons[k].buttonBehav.greyedOut = true;
                }

                pages[0].subObjects.Add(playerClassButtons[k]);
            }

            playerJoinButtons = new PlayerJoinButton[4];
            for (int l = 0; l < playerJoinButtons.Length; l++)
            {
                playerJoinButtons[l] = new PlayerJoinButton(mm, pages[0], new Vector2(600f + l * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f) * playerJoinButtons.Length, 40f), l);
                playerJoinButtons[l].buttonBehav.greyedOut = true;

                if (ModManager.MSC)
                {
                    playerJoinButtons[l].portrait.fileName = mm.ArenaImage(manager.arenaSetup.playerClass[l], l);
                    playerJoinButtons[l].portrait.LoadFile();
                    playerJoinButtons[l].portrait.sprite.SetElementByName(playerJoinButtons[l].portrait.fileName);
                    MutualVerticalButtonBind(playerClassButtons[l], playerJoinButtons[l]);
                }

                pages[0].subObjects.Add(playerJoinButtons[l]);
            }

            playerJoinButtons[0].Joined = true; // host should be part of game
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

        List<SimplerButton> enableOnLobbyAvailable = new();
        private void OnLobbyAvailable()
        {
            foreach (var b in enableOnLobbyAvailable)
            {
                b.buttonBehav.greyedOut = false;
            }
        }

        private void InitializeSitting()
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
        }

        private void StartGame()
        {
            RainMeadow.DebugMe();
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

            InitializeSitting();

            manager.rainWorld.progression.ClearOutSaveStateFromMemory();

            // temp
            UserInput.SetUserCount(OnlineManager.players.Count);
            UserInput.SetForceDisconnectControllers(forceDisconnect: false);

            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void Update()
        {
            //MultiplayerMenuUpdate();
            mm.Update();
            //base.Update();
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
                    mm.GetArenaSetup.playerClass[num6] = mm.NextClass(mm.GetArenaSetup.playerClass[num6]);
                    playerClassButtons[num6].menuLabel.text = Translate(SlugcatStats.getSlugcatName(mm.GetArenaSetup.playerClass[num6]));
                    playerJoinButtons[num6].portrait.fileName = mm.ArenaImage(mm.GetArenaSetup.playerClass[num6], num6);
                    playerJoinButtons[num6].portrait.LoadFile();
                    playerJoinButtons[num6].portrait.sprite.SetElementByName(playerJoinButtons[num6].portrait.fileName);
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

        void MultiplayerMenuUpdate()
        {
            if (!mm.requestingControllerConnections && !mm.exiting)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i != 0)
                    {
                        manager.arenaSetup.playersJoined[i] = manager.rainWorld.GetPlayerHandler(i) != null;
                        _ = manager.arenaSetup.playersJoined[i];
                        manager.rainWorld.GetPlayerSigningIn(i);
                    }
                }
            }

            base.Update();

            bool flag = RWInput.CheckPauseButton(0, manager.rainWorld);
            if (flag && !mm.lastPauseButton && manager.dialog == null)
            {
                mm.OnExit();
            }

            mm.lastPauseButton = flag;
            mm.lastBlackFade = mm.blackFade;

            var num = 0;
            if (mm.blackFade < num)
            {
                mm.blackFade = Custom.LerpAndTick(mm.blackFade, num, 0.05f, 71f / (339f * (float)Math.PI));
            }
            else
            {
                mm.blackFade = Custom.LerpAndTick(mm.blackFade, num, 0.05f, 0.125f);
            }

            bool flag2 = false;
            int num2 = 0;
            for (int j = 0; j < mm.GetArenaSetup.playersJoined.Length; j++)
            {
                if (mm.GetArenaSetup.playersJoined[j])
                {
                    num2++;
                }
            }

            if (num2 == 0)
            {
                mm.abovePlayButtonLabel.text = Translate("No players joined!");
            }
            else
            {
                if (mm.levelSelector.levelsPlaylist != null && mm.levelSelector.levelsPlaylist.mismatchCounter > 20)
                {
                    mm.abovePlayButtonLabel.text = Translate("ERROR");
                }
                else
                {
                    int num3 = mm.GetGameTypeSetup.playList.Count * mm.GetGameTypeSetup.levelRepeats;
                    if (num3 == 0)
                    {
                        mm.abovePlayButtonLabel.text = Regex.Replace(Translate("Select which levels to play<LINE>in the level selector"), "<LINE>", "\r\n");
                    }
                    else
                    {
                        int num4 = mm.ApproximatePlayTime();
                        string text;
                        text = ((num3 == 1) ? Translate("ROUND SESSION") : ((num3 < 2 && num3 > 4) ? Translate("ROUNDS SESSION") : Translate("ROUNDS SESSION-ru2")));
                        text = ((!text.Contains("#")) ? (num3 + " " + text) : text.Replace("#", num3.ToString()));
                        mm.abovePlayButtonLabel.text = text + ((num4 > 0) ? ("\r\n" + Translate("Approximately") + " " + num4 + " " + ((num4 == 1) ? Translate("minute") : Translate("minutes"))) : "");
                        flag2 = true;
                    }
                }
            }

            mm.APBLLastSin = mm.APBLSin;
            mm.APBLLastPulse = mm.APBLPulse;

            if (!flag2)
            {
                mm.APBLSin += 1f;
                mm.APBLPulse = Custom.LerpAndTick(mm.APBLPulse, 1f, 0.04f, 0.025f);
                mm.playButton.buttonBehav.greyedOut = true;
            }
            else
            {
                mm.APBLPulse = Custom.LerpAndTick(mm.APBLPulse, 0f, 0.04f, 0.025f);
                mm.playButton.buttonBehav.greyedOut = false;
            }
        }

        public override void ShutDownProcess()
        {
            // Rain Meadow
            RainMeadow.DebugMe();
            if (OnlineManager.lobby != null) OnlineManager.lobby.OnLobbyAvailable -= OnLobbyAvailable;
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                MatchmakingManager.instance.LeaveLobby();
            }
            base.ShutDownProcess();
        }
    }
}
