using Kittehface.Framework20;
using Menu;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RainMeadow
{
    public class ArenaLobbyMenu : SmartMenu
    {
        public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;
        public ArenaLobbyMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ArenaLobbyMenu)
        {
            RainMeadow.DebugMe();

            OnlineManager.lobby.OnLobbyAvailable += OnLobbyAvailable;

            manager.arenaSetup = new ArenaSetup(manager)
            {
                currentGameType = ArenaSetup.GameTypeID.Competitive
            };

            FakeInitializeMultiplayerMenu();

            BuildLayout();
        }

        MultiplayerMenu mm;
        void FakeInitializeMultiplayerMenu()
        {
            mm = (MultiplayerMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(MultiplayerMenu));
            mm.ID = ProcessManager.ProcessID.MultiplayerMenu;
            mm.manager = manager;
            mm.currentGameType = ArenaSetup.GameTypeID.Competitive;

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
        struct SimpleButtonCreateOptions
        {
            public string text;
            public Vector2 pos;
            public Vector2 size;
            public Action<SimplerButton>? clicked;
            public bool waitLobbyAvailability;
            public Page? page;
        }

        SimplerButton CreateButton(SimpleButtonCreateOptions options)
        {
            var page = options.page ?? pages[0];

            var b = new SimplerButton(this, page, options.text, options.pos, options.size);

            if (options.waitLobbyAvailability && !OnlineManager.lobby.isAvailable)
            {
                b.buttonBehav.greyedOut = true;
                enableOnLobbyAvailable.Add(b);
            }

            if (options.clicked != null) b.OnClick += options.clicked;

            page.subObjects.Add(b);

            return b;
        }

        struct PlayerButtonCreateOptions
        {
            public int playerIndex;
            public Vector2 pos;
            public Page? page;
            public float? width;
        }

        #endregion

        int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // been using 1360 as ref
        void BuildLayout()
        {
            scene.AddIllustration(new MenuIllustration(this, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
            scene.AddIllustration(new MenuIllustration(this, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
            scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.shader = manager.rainWorld.Shaders["MenuText"];

            CreateButton(new SimpleButtonCreateOptions()
            {
                text = "START",
                pos = new Vector2(ScreenWidth - 304, 50),
                size = new Vector2(110, 30),
                waitLobbyAvailability = true,
                clicked = self => StartGame()
            });

            infoButton = new SymbolButton(this, pages[0], "Menu_InfoI", "INFO", new Vector2(1142f, 624f));
            pages[0].subObjects.Add(infoButton);

            BuildPlayerSlots();
            //BuildLevelLists();
        }

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

            playerClassButtons = new SimpleButton[4];
            for (int k = 0; k < playerClassButtons.Length; k++)
            {
                string name = "Survivor";

                if (ModManager.MSC)
                {
                    name = Translate(SlugcatStats.getSlugcatName(manager.arenaSetup.playerClass[k]));
                }

                playerClassButtons[k] = new SimpleButton(this, pages[0], name, "CLASSCHANGE" + k, new Vector2(600f + k * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * playerClassButtons.Length, 40f), new Vector2(num - 20f, 30f));

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

                if (ModManager.MSC)
                {
                    playerJoinButtons[l].portrait.fileName = ArenaImage(manager.arenaSetup.playerClass[l], l);
                    playerJoinButtons[l].portrait.LoadFile();
                    playerJoinButtons[l].portrait.sprite.SetElementByName(playerJoinButtons[l].portrait.fileName);
                    MutualVerticalButtonBind(playerClassButtons[l], playerJoinButtons[l]);
                }

                pages[0].subObjects.Add(playerJoinButtons[l]);
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
            UserInput.SetUserCount(1);
            UserInput.SetForceDisconnectControllers(forceDisconnect: false);

            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void Update()
        {
            base.Update();

            //mm.Update();
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

        public string ArenaImage(SlugcatStats.Name classID, int color)
        {
            if (classID == null)
            {
                return "MultiplayerPortrait" + color + "2";
            }

            return "MultiplayerPortrait" + color + "1-" + classID.ToString();
        }
    }
}
