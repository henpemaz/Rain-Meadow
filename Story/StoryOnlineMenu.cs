using Menu;
using Menu.Remix;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HUD;
namespace RainMeadow
{
    public class StoryOnlineMenu : SlugcatSelectMenu, SelectOneButton.SelectOneButtonOwner
    {
        int selectOneBtnIndex;
        internal CheckBox clientWantsToOverwriteSave;
        internal CheckBox friendlyFire;
        internal EventfulSelectOneButton[] playerButtons = new EventfulSelectOneButton[0];
        internal EventfulHoldButton hostStartButton;
        internal EventfulHoldButton clientWaitingButton;
        internal SlugcatCustomization personaSettings;
        private PlayerInfo[] players;
        internal bool resetSave;
        private bool updateDefaultColors;
        internal StoryGameMode storyGameMode;
        internal MenuLabel campaignContainer;
        internal MenuLabel onlineDifficultyLabel;
        internal SimplerSymbolButton iHateCheckboxes;


        internal SlugcatStats.Name customSelectedSlugcat = SlugcatStats.Name.White;

        public StoryOnlineMenu(ProcessManager manager) : base(manager)
        {
            storyGameMode = (StoryGameMode)OnlineManager.lobby.gameMode;

            storyGameMode.Sanitize();

            storyGameMode.currentCampaign = slugcatPages[slugcatPageIndex].slugcatNumber;

            StoryMenuHelpers.RemoveExcessStoryObjects(this, storyGameMode);
            StoryMenuHelpers.ModifyExistingMenuItems(this);

            if (OnlineManager.lobby.isOwner)
            {
                StoryMenuHelpers.SetupHostMenu(this, storyGameMode);
                var hostSettings = StoryMenuHelpers.GetHostBoolStoryRemixSettings();
                storyGameMode.storyBoolRemixSettings = hostSettings.hostBoolSettings;
                storyGameMode.storyFloatRemixSettings = hostSettings.hostFloatSettings;
                storyGameMode.storyIntRemixSettings = hostSettings.hostIntSettings;
            }
            else
            {
                StoryMenuHelpers.SetupClientMenu(this, storyGameMode);
                if (RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value)
                {
                    StoryMenuHelpers.CustomSlugcatSetup(this, storyGameMode);
                }

            }
            SteamSetup();
            UpdatePlayerList();
            StoryMenuHelpers.SetupOnlineMenuItems(this, storyGameMode);

            StoryMenuHelpers.SetupOnlineCustomization(this);

            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;

        }

        public void StartGame()
        {

            RainMeadow.DebugMe();
            if (!OnlineManager.lobby.isOwner) // I'm a client
            {
                if (ModManager.MMF)
                {
                    StoryMenuHelpers.SetClientStoryRemixSettings(storyGameMode.storyBoolRemixSettings, storyGameMode.storyFloatRemixSettings, storyGameMode.storyIntRemixSettings); // Set client remix settings to Host's on StartGame()
                }
                if (!RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value) // I'm a client and I want to match the hosts
                {
                    personaSettings.playingAs = storyGameMode.currentCampaign;
                }
            }
            else //I'm the host
            {


                RainMeadow.Debug("CURRENT CAMPAIGN: " + StoryMenuHelpers.GetCurrentCampaignName(storyGameMode));
                personaSettings.playingAs = storyGameMode.currentCampaign;
            }

            manager.arenaSitting = null;
            if (restartChecked)
            {
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
            }
            else
            {
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
            }
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }

        public override void Update()
        {
            base.Update();

            if (OnlineManager.lobby.isOwner && hostStartButton != null)
            {
                hostStartButton.buttonBehav.greyedOut = OnlineManager.lobby.clientSettings.Values.Any(cs => cs.inGame);
            }
            else
            {
                clientWaitingButton.buttonBehav.greyedOut = !(storyGameMode.isInGame && !storyGameMode.changedRegions);
                StoryMenuHelpers.GetRegionAndCampaignNameForClient(this, storyGameMode);

            }


            if (Mathf.Abs(this.lastScroll) > 0.5f && Mathf.Abs(this.scroll) <= 0.5f)
            {
                UpdatePlayerList();
            }
        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game) // if join on sleep/deathscreen this needs to be added here as well
            {
                OnlineManager.LeaveLobby();
            }
            RainMeadow.rainMeadowOptions._SaveConfigFile(); // save colors
            base.ShutDownProcess();
        }


        private void SteamSetup()
        {

            List<PlayerInfo> players = new List<PlayerInfo>();
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                CSteamID playerId;
                if (player.id is LocalMatchmakingManager.LocalPlayerId)
                {
                    playerId = default;
                }
                else
                {
                    playerId = (player.id as SteamMatchmakingManager.SteamPlayerId).steamID;
                }
                players.Add(new PlayerInfo(playerId, player.id.name));
            }
            this.players = players.ToArray();
        }

        private void UpdatePlayerList()
        {
            for (int i = 0; i < playerButtons.Length; i++)
            {
                var playerbtn = playerButtons[i];
                playerbtn.RemoveSprites();
                this.pages[0].RemoveSubObject(playerbtn);
            }

            playerButtons = new EventfulSelectOneButton[players.Length];

            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var btn = new EventfulSelectOneButton(this, this.pages[0], player.name, "playerButtons", new Vector2(194, 515) - i * new Vector2(0, 38), new(110, 30), playerButtons, i);
                this.pages[0].subObjects.Add(btn);
                playerButtons[i] = btn;
                btn.OnClick += (_) =>
                {
                    string url = $"https://steamcommunity.com/profiles/{player.id}";
                    SteamFriends.ActivateGameOverlayToWebPage(url);
                };
            }
        }

        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            this.players = players;
            UpdatePlayerList();
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);

            if (message == "PREV")
            {

                var index = slugcatPageIndex - 1 < 0 ? slugcatPages.Count - 1 : slugcatPageIndex - 1;
                StoryMenuHelpers.TryGetRegion(this, storyGameMode, index);
            }
            if (message == "NEXT")
            {
                var index = slugcatPageIndex + 1 >= slugcatPages.Count ? 0 : slugcatPageIndex + 1;
                StoryMenuHelpers.TryGetRegion(this, storyGameMode, index);

            }

        }

        public int GetCurrentlySelectedOfSeries(string series)
        {
            return selectOneBtnIndex;
        }

        public void SetCurrentlySelectedOfSeries(string series, int to)
        {
            selectOneBtnIndex = to;
        }

    }




}
