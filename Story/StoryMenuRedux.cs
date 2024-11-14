using Menu;
using Menu.Remix;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ExtraExtentions;
namespace RainMeadow
{
    public class StoryMenuRedux : SlugcatSelectMenu, SelectOneButton.SelectOneButtonOwner
    {
        private PlayerInfo[] players;
        internal CheckBox resetSaveCheckbox;
        internal CheckBox clientWantsToOverwriteSave;
        internal EventfulSelectOneButton[] playerButtons = new EventfulSelectOneButton[0];
        int selectOneBtnIndex;
        internal SlugcatCustomization personaSettings;
        private StoryGameMode story;
        internal EventfulHoldButton hostStartButton;
        internal EventfulHoldButton clientWaitingButton;
        internal bool resetSave;
        internal StoryGameMode storyModeOnline;
        internal MenuLabel campaignContainer;
        private bool updateDefaultColors;


        internal SlugcatStats.Name customSelectedSlugcat = SlugcatStats.Name.White;

        public StoryMenuRedux(ProcessManager manager) : base(manager)
        {

            storyModeOnline = OnlineManager.lobby.gameMode as StoryGameMode;
            storyModeOnline.currentCampaign = slugcatPages[slugcatPageIndex].slugcatNumber;

            StoryMenuHelpers.RemoveExcessStoryObjects(this);


            if (OnlineManager.lobby.isOwner)
            {
                StoryMenuHelpers.SetupHostMenu(this, storyModeOnline);
            }
            else
            {
                StoryMenuHelpers.SetupClientMenu(this, storyModeOnline);
                if (RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value)
                {
                    StoryMenuHelpers.CustomSlugcatSetup(this, customSelectedSlugcat);
                }

            }
            StoryMenuHelpers.SetupOnlineCustomization(this);
            SteamSetup();
            UpdatePlayerList();
            StoryMenuHelpers.SetupOnlineMenuItems(this);

            StoryMenuHelpers.SanitizeStoryClientSettings(storyModeOnline.storyClientData);
            StoryMenuHelpers.SanitizeStoryGameMode(storyModeOnline);

            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;

        }

        // TODO:
        // 1. IownAcheckbox is competing with story menu built in stuff
        // 2. Need to figure out color change event to save choice

        public void StartGame()
        {
            RainMeadow.DebugMe();
            if (!OnlineManager.lobby.isOwner) // I'm a client
            {
                if (ModManager.MMF)
                {
                    StoryMenuHelpers.SetClientStoryRemixSettings(story.storyBoolRemixSettings, story.storyFloatRemixSettings, story.storyIntRemixSettings); // Set client remix settings to Host's on StartGame()
                }
                if (!RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value) // I'm a client and I want to match the hosts
                {
                    personaSettings.playingAs = story.currentCampaign;
                }
                else // I'm a client and I want my own Slugcat
                {
                    personaSettings.playingAs = customSelectedSlugcat;

                }
            }
            else //I'm the host
            {
                personaSettings.playingAs = slugcatPages[slugcatPageIndex].slugcatNumber;
                RainMeadow.Debug("CURRENT CAMPAIGN: " + StoryMenuHelpers.GetCurrentCampaignName(storyModeOnline));
            }

            manager.arenaSitting = null;
            if (resetSave)
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
                campaignContainer.text = $"Current Campaign: The {StoryMenuHelpers.GetCurrentCampaignName(storyModeOnline)}";
                clientWaitingButton.buttonBehav.greyedOut = !(storyModeOnline.isInGame && !storyModeOnline.changedRegions);
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
        public bool GetChecked(CheckBox box)
        {
            string idstring = box.IDString;
            if (idstring != null)
            {
                if (idstring == "RESETSAVE")
                {
                    return resetSave;
                }
                if (idstring == "OVERWRITECLIENTSAVE")
                {
                    return storyModeOnline.saveToDisk;
                }
            }
            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            string idstring = box.IDString;
            if (idstring != null)
            {
                if (idstring == "RESETSAVE")
                {
                    resetSave = !resetSave;
                }

                if (idstring == "OVERWRITECLIENTSAVE")
                {
                    storyModeOnline.saveToDisk = !storyModeOnline.saveToDisk;
                }
            }
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "PREV")
            {

                var index = slugcatPageIndex - 1 < 0 ? slugcatPages.Count - 1 : slugcatPageIndex - 1;
                storyModeOnline.currentCampaign  = slugcatPages[index].slugcatNumber;
            }
            if (message == "NEXT")
            {
                var index = slugcatPageIndex + 1 >= slugcatPages.Count ? 0 : slugcatPageIndex + 1;
                storyModeOnline.currentCampaign = slugcatPages[index].slugcatNumber;

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
