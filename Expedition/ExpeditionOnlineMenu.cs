using Menu;
using UnityEngine;
using Expedition;
using System.Collections.Generic;
using Steamworks;
using System.Linq;

namespace RainMeadow
{
    public partial class ExpeditionOnlineMenu : ExpeditionMenu
    {
        private ExpeditionGameMode expeditionGameMode;        
        private MenuLabel? lobbyLabel, slugcatLabel;
        private bool isCurrentCampaignInitiallyNull = false;
        private bool updatingStats, onlineFirstTimeLoad;

        public ExpeditionOnlineMenu(ProcessManager manager) : base(manager)
        {
            playerSelectedSlugcats = new SlugcatStats.Name[4];
            SetupSelectableSlugcats();
            ID = OnlineManager.lobby.gameMode.MenuProcessId();
            expeditionGameMode = (ExpeditionGameMode)OnlineManager.lobby.gameMode;
            expeditionGameMode.Sanitize();

            if (OnlineManager.lobby.isOwner)
            {
                expeditionGameMode.currentCampaign = ExpeditionData.slugcatPlayer;
                expeditionGameMode.saveToDisk = true;
                expeditionGameMode.slugcatCampaingSelected = currentSelection;

                if (MatchmakingManager.currentInstance is SteamMatchmakingManager steamMatchmakingManager)
                    SteamMatchmaking.SetLobbyData(steamMatchmakingManager.lobbyID, MatchmakingManager.CAMPAIGN_KEY, "");
            }
            else
            {
                ExpeditionData.validateQuests = false;
                currentSelection = expeditionGameMode.slugcatCampaingSelected;
                expeditionGameMode.needSlugUpdate = true;
            }

            RMOverlayHUD.GetOverlay()?.DestroyChatHUD();
            textAnchor = RainMeadow.rainMeadowOptions.ChatTextDownscroll.Value
                ? ButtonScroller.TextAnchor.Bottom
                : ButtonScroller.TextAnchor.Top;

            Futile.atlasManager.LoadAtlas("illustrations/arena_ui_elements");

            MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
            ChatTextBox.OnShutDownRequest += ResetChatInput;

            expeditionGameMode.needUpdateeChallengePreview = true;
        }

        public override void Update()
        {
            if (nullLobbyError != null)
            {
                base.Update();
                return;
            }

            if (OnlineManager.lobby == null && nullLobbyError == null)
            {
                float x = manager.rainWorld.options.ScreenSize.x;
                float y = manager.rainWorld.options.ScreenSize.y;
                float w = 480;
                float h = 320;
                nullLobbyError = new NullLobbyError(this, pages[_currentPage], new Vector2((x - w) / 2, (y - h) / 2), new Vector2(w, h), Utils.Translate("Story lobby is null! Exiting..."), false);
                pages[_currentPage].subObjects.Add(nullLobbyError);
                return;
            }

            if (ChatTextBox.blockInput)
            {
                ChatTextBox.blockInput = false;
                if ((RWInput.CheckPauseButton(0) || Input.GetKeyDown(KeyCode.Escape)) && !lastPauseButton)
                {
                    PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    ToggleChat(false);
                    lastPauseButton = true;
                }
                ChatTextBox.blockInput = true;
            }

            if (OnlineManager.lobby is not null)
            {
                if (characterSelect is not null && characterSelect.waitForSaveData)
                {
                    updatingStats = true;
                }
                else if (updatingStats)
                {                 
                    updatingStats = false;
                    if (chatMenuBox is not null) ResetChatInput();
                }
            }

            base.Update();

            if (!onlineFirstTimeLoad && firstTimeLoad)
            {
                onlineFirstTimeLoad = true;
                _currentPage = currentPage;
                SetupOnlineMenuItens();
                UpdatePlayerList();
            }


            UpdateUI();

            if (OnlineManager.lobby is null) return;

            if (OnlineManager.lobby.isOwner)
            {
                SetCampaign(ExpeditionData.slugcatPlayer);
                expeditionGameMode.slugcatCampaingSelected = currentSelection;

                PreviewChallengesInMenu();

                if (characterSelect is not null)
                {
                    characterSelect.abandonButton.bumpBehav.greyedOut = false;
                    if (characterSelect.confirmExpedition?.buttonBehav is not null)
                        characterSelect.confirmExpedition.buttonBehav.greyedOut = false;
                }
            }
            else
            {
                currentSelection = expeditionGameMode.slugcatCampaingSelected;

                if (characterSelect is not null)
                {
                    characterSelect.abandonButton.bumpBehav.greyedOut = true;
                    if (characterSelect.confirmExpedition is not null)
                    {
                        characterSelect.confirmExpedition.buttonBehav.greyedOut = !expeditionGameMode.canJoinGame;
                    }

                    if (expeditionGameMode.needUpdateeChallengePreview)
                    {

                        expeditionGameMode.needUpdateeChallengePreview = false;
                        characterSelect.UpdateChallengePreview();                        
                    }
                }
            }

            if (expeditionGameMode.needMenuSaveUpdate)
            {
                expeditionGameMode.currentCampaign = ExpeditionData.slugcatPlayer;
                characterSelect?.UpdateSelectedSlugcat((characterSelect.menu as ExpeditionMenu).currentSelection);
                expeditionGameMode.needMenuSaveUpdate = false;
            }

            if (characterSelect is not null && expeditionGameMode.needSlugUpdate)
            {
                expeditionGameMode.needSlugUpdate = false;
                characterSelect.UpdateSelectedSlugcat(expeditionGameMode.slugcatCampaingSelected);

                if (!OnlineManager.lobby.isOwner)
                {
                    for (int i = 0; i < characterSelect.slugcatButtons.Length; i++)
                    {
                        characterSelect.slugcatButtons[i].buttonBehav.greyedOut = expeditionGameMode.slugcatCampaingSelected != i;
                    }
                }
                else
                {
                    for (int i = 0; i < characterSelect.slugcatButtons.Length; i++)
                    {
                        bool greyout = !ExpeditionGame.unlockedExpeditionSlugcats.Contains(ExpeditionGame.playableCharacters[i]);
                        characterSelect.slugcatButtons[i].buttonBehav.greyedOut = greyout;
                    }
                }
            }

            if (expeditionGameMode.requireCampaignSlugcat)
            {
                RemoveSlugcatList();
                for (int i = 0; i < playerSelectedSlugcats.Length; i++)
                {
                    if (ModManager.JollyCoop && i < manager.rainWorld.options.jollyPlayerOptionsArray.Length)
                    {
                        manager.rainWorld.options.jollyPlayerOptionsArray[i].playerClass = expeditionGameMode.currentCampaign;
                    }

                    SetSelectedSlugcat(i, expeditionGameMode.currentCampaign);
                }
            }
            else if(onlineFirstTimeLoad)
            {
                SetupSlugcatList();
            }

            if (slugcatSelector != null)
            {
                slugcatSelector.Slug = PlayerSelectedSlugcat;
            }

        }

        private void PreviewChallengesInMenu()
        {
            if (isCurrentCampaignInitiallyNull)
            {
                if (currentPage != 1)
                {
                    if (expeditionGameMode.menuSaveState is null)
                    {
                        expeditionGameMode.menuSaveState = new StoryLobbyData.MenuSaveStateState()
                        {
                            karma = 1,
                            rippleLevel = 0,
                            food = 0,
                            cycle = 0,
                            karmaReinforced = false,
                            hasGlow = false,
                            hasMark = false,
                            ascended = false,
                            altEnd = false,
                            shelterName = "",
                            gameTimeAlive = 0,
                            gameTimeDead = 0,
                        };
                        expeditionGameMode.hasSaveState = true;
                    }
                }
                else
                {
                    expeditionGameMode.menuSaveState = null;
                    expeditionGameMode.hasSaveState = false;
                }
            }
        }

        public void pre_start()
        {
            if (OnlineManager.lobby != null)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    expeditionGameMode.currentCampaign = Expedition.ExpeditionData.slugcatPlayer;
                }

                var jollyallowed = ModManager.JollyCoop;
                
                expeditionGameMode.avatarCount = jollyallowed ? manager.rainWorld.options.JollyPlayerCount : 1;
                if (jollyallowed) PlayerGraphics.PopulateJollyColorArray(PlayerSelectedSlugcat);

                for (int i = 0; i < expeditionGameMode.avatarSettings.Length; i++)
                {
                    expeditionGameMode.avatarSettings[i].playingAs = expeditionGameMode.currentCampaign;
                    if (!expeditionGameMode.requireCampaignSlugcat && (playerSelectedSlugcats[i] is SlugcatStats.Name name))
                    {
                        expeditionGameMode.avatarSettings[i].playingAs = name;
                    }
                    //expeditionGameMode.avatarSettings[i].playingAs = expeditionGameMode.currentCampaign;             
                    expeditionGameMode.avatarSettings[i].currentColors = [.. PlayerGraphics.DefaultBodyPartColorHex(expeditionGameMode.avatarSettings[i].playingAs).Select(RWCustom.Custom.hexToColor)];

                    if (jollyallowed)
                    {
                        if (manager.rainWorld.options.jollyColorMode == Options.JollyColorMode.CUSTOM)
                        {
                            expeditionGameMode.avatarSettings[i].currentColors = new List<Color>
                            {
                                manager.rainWorld.options.jollyPlayerOptionsArray[i].GetBodyColor(),
                                manager.rainWorld.options.jollyPlayerOptionsArray[i].GetFaceColor(),
                                manager.rainWorld.options.jollyPlayerOptionsArray[i].GetUniqueColor()
                            };
                        }
                        else if (manager.rainWorld.options.jollyColorMode == Options.JollyColorMode.AUTO)
                        {
                            if (i == 0)
                            {
                                expeditionGameMode.avatarSettings[i].currentColors = [.. PlayerGraphics.DefaultBodyPartColorHex(expeditionGameMode.avatarSettings[i].playingAs).Select(RWCustom.Custom.hexToColor)];
                            }
                            else
                            {
                                expeditionGameMode.avatarSettings[i].currentColors = new List<Color>
                                {
                                    PlayerGraphics.JollyColor(i, 0),
                                    PlayerGraphics.JollyColor(i, 1),
                                    PlayerGraphics.JollyColor(i, 2)
                                };
                            }
                        }
                        else
                        {
                            expeditionGameMode.avatarSettings[i].currentColors = [.. PlayerGraphics.DefaultBodyPartColorHex(expeditionGameMode.avatarSettings[i].playingAs).Select(RWCustom.Custom.hexToColor)];
                        }
                        expeditionGameMode.avatarSettings[i].fakePup = manager.rainWorld.options.jollyPlayerOptionsArray[i].isPup;
                    }
                    else
                    {
                        expeditionGameMode.avatarSettings[i].currentColors = manager.rainWorld.progression.GetCustomColors(expeditionGameMode.avatarSettings[i].playingAs);
                        expeditionGameMode.avatarSettings[i].fakePup = false;
                    }
                }


                if (colorConfigDialog != null)
                {
                    manager.StopSideProcess(colorConfigDialog); //force getting rid of dialog
                }

                if (MatchmakingManager.currentInstance is SteamMatchmakingManager steamMatchmakingManager)
                    SteamMatchmaking.SetLobbyData(steamMatchmakingManager.lobbyID, MatchmakingManager.CAMPAIGN_KEY, expeditionGameMode.currentCampaign.value);

                if (ModManager.CoopAvailable)
                {
                    for (int i = 1; i < expeditionGameMode.avatarCount; i++)
                    {
                        manager.rainWorld.ActivatePlayer(i);
                    }
                    for (int j = expeditionGameMode.avatarCount; j < 4; j++)
                    {
                        manager.rainWorld.DeactivatePlayer(j);
                    }
                }
            }
        }

        bool firstTimeCampaingSet;
        public void SetCampaign(SlugcatStats.Name campaign)
        {
            if (expeditionGameMode.currentCampaign == campaign && firstTimeCampaingSet) return;
            if (manager.rainWorld.progression.loadInProgress) return;
                
            firstTimeCampaingSet = true;
            updatingStats = true;

            expeditionGameMode.currentCampaign = campaign;            
            RainMeadow.Debug($"{campaign} selected");

            SlugcatSelectMenu.SaveGameData sgd = SlugcatSelectMenu.MineForSaveData(RWCustom.Custom.rainWorld.processManager, expeditionGameMode.currentCampaign);
            
            if (sgd is not null)
            {
                expeditionGameMode.menuSaveState = new StoryLobbyData.MenuSaveStateState(sgd);
                expeditionGameMode.hasSaveState = true;
                isCurrentCampaignInitiallyNull = false;
            }
            else
            {
                expeditionGameMode.menuSaveState = null;
                expeditionGameMode.hasSaveState = false;
                isCurrentCampaignInitiallyNull = true;
            }

        }

        public override void ShutDownProcess()
        {
            isChatToggled = false;
            ResetChatInput();
            ChatTextBox.OnShutDownRequest -= ResetChatInput;

            RainMeadow.DebugMe();
            var up = manager.upcomingProcess;
            if (up != ProcessManager.ProcessID.Game && up != RainMeadow.Ext_ProcessID.ExpeditionMenu && up != ExpeditionEnums.ProcessID.ExpeditionJukebox && up != ProcessManager.ProcessID.InputOptions)
            {
                OnlineManager.LeaveLobby();
            }
            base.ShutDownProcess();
        }

        public override void Singal(MenuObject sender, string message)
        {
            if (message == "EXIT")
            {
                PlaySound(SoundID.MENU_Switch_Page_Out);
                Expedition.Expedition.coreFile.Save(runEnded: false);
                manager.musicPlayer?.FadeOutAllSongs(100f);
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                return;
            }

            base.Singal(sender, message);

            if (message == "COLOR_SLUGCAT")
            {
                OpenColorConfig(PlayerSelectedSlugcat);
            }
        }
    }
}
