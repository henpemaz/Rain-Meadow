using HarmonyLib;
using Menu;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class StoryOnlineMenu : SlugcatSelectMenu, SelectOneButton.SelectOneButtonOwner
    {
        CheckBox clientWantsToOverwriteSave;
        CheckBox friendlyFire;
        List<SimplerButton> playerButtons = new();
        SlugcatCustomization personaSettings;
        EventfulSelectOneButton[] scugButtons;
        StoryGameMode storyGameMode;
        MenuLabel onlineDifficultyLabel;
        Vector2 restartCheckboxPos;

        public StoryOnlineMenu(ProcessManager manager) : base(manager)
        {
            ID = OnlineManager.lobby.gameMode.MenuProcessId();
            storyGameMode = (StoryGameMode)OnlineManager.lobby.gameMode;

            storyGameMode.Sanitize();
            storyGameMode.currentCampaign = slugcatPages[slugcatPageIndex].slugcatNumber;

            restartCheckboxPos = restartCheckbox.pos;

            RemoveExcessStoryObjects();
            ModifyExistingMenuItems();

            if (OnlineManager.lobby.isOwner)
            {
                storyGameMode.saveToDisk = true;
                (storyGameMode.storyBoolRemixSettings, storyGameMode.storyFloatRemixSettings, storyGameMode.storyIntRemixSettings) = StoryMenuHelpers.GetHostBoolStoryRemixSettings();
            }
            else
            {
                SetupClientOptions();
                if (RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value)
                {
                    SetupSlugcatList();
                }
            }

            SetupOnlineCustomization();

            SetupOnlineMenuItems();
            UpdatePlayerList();

            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        }

        public new void StartGame(SlugcatStats.Name storyGameCharacter)
        {
            if (OnlineManager.lobby.isOwner)
            {
                personaSettings.playingAs = storyGameMode.currentCampaign = storyGameCharacter;
            }
            else
            {
                if (ModManager.MMF)
                {
                    StoryMenuHelpers.SetClientStoryRemixSettings(storyGameMode.storyBoolRemixSettings, storyGameMode.storyFloatRemixSettings, storyGameMode.storyIntRemixSettings); // Set client remix settings to Host's on StartGame()
                }
                if (!RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value) // I'm a client and I want to match the host's
                {
                    personaSettings.playingAs = storyGameMode.currentCampaign;
                }
            }

            // TODO: figure out how to reuse vanilla StartGame
            // * override singleplayer custom colours
            // * fix intro cutscenes messing with resource acquisition
            // ? how to deal with statistics screen (not supposed to continue, we should require wipe)
            manager.arenaSitting = null;
            if (restartChecked)
            {
                manager.rainWorld.progression.WipeSaveState(storyGameMode.currentCampaign);
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

            if (OnlineManager.lobby.isOwner)
            {
                storyGameMode.currentCampaign = slugcatPages[slugcatPageIndex].slugcatNumber;
                storyGameMode.region = CurrentRegion();
                if (startButton != null)
                {
                    startButton.buttonBehav.greyedOut = OnlineManager.lobby.clientSettings.Values.Any(cs => cs.inGame);
                }
            }
            else
            {
                if (startButton != null)
                {
                    startButton.buttonBehav.greyedOut = !storyGameMode.canJoinGame;
                }
                if (onlineDifficultyLabel != null)
                {
                    onlineDifficultyLabel.text = GetCurrentCampaignName() + (string.IsNullOrEmpty(storyGameMode.region) ? Translate(" - New Game") : $" - {storyGameMode.region}");
                }
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

        private void UpdatePlayerList()
        {
            playerButtons.Do(x => StoryMenuHelpers.RemoveMenuObjects(x));
            playerButtons.Clear();

            var pos = new Vector2(194, 553);

            foreach (var playerInfo in MatchmakingManager.instance.playerList)
            {
                pos -= new Vector2(0, 38);
                var btn = new SimplerButton(this, this.pages[0], playerInfo.name, pos, new(110, 30));
                btn.OnClick += (_) =>
                {
                    if (playerInfo.id != default)
                    {
                        SteamFriends.ActivateGameOverlayToWebPage($"https://steamcommunity.com/profiles/{playerInfo.id}");
                    }
                };
                playerButtons.Add(btn);
            }
            foreach (var btn in playerButtons)
            {
                this.pages[0].subObjects.Add(btn);
            }

        }

        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            UpdatePlayerList();
        }

        public int GetCurrentlySelectedOfSeries(string series) => series switch
        {
            "scugButtons" => !RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value ? -1 : slugcatPageIndex,
            _ => -1,
        };

        public void SetCurrentlySelectedOfSeries(string series, int to)
        {
            switch (series)
            {
                case "scugButtons":
                    slugcatPageIndex = to;
                    return;
            }
        }

        public string GetCurrentCampaignName() => "Current Campaign: " + SlugcatStats.getSlugcatName(storyGameMode.currentCampaign);

        private string CurrentRegion()
        {
            try
            {
                var shelterName = saveGameData[storyGameMode.currentCampaign]?.shelterName;
                if (shelterName != null && shelterName.Length > 2)
                {
                    return Region.GetRegionFullName(shelterName.Substring(0, 2), storyGameMode.currentCampaign);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Debug($"Error getting region name: {e}");
            }
            return "";
        }

        private void SetupSlugcatList()
        {
            onlineDifficultyLabel = new MenuLabel(this, pages[0], $"{GetCurrentCampaignName()}", new Vector2(startButton.pos.x - 100f, startButton.pos.y + 100f), new Vector2(200f, 30f), bigText: true);
            onlineDifficultyLabel.label.alignment = FLabelAlignment.Center;
            onlineDifficultyLabel.label.alpha = 0.5f;
            pages[0].subObjects.Add(onlineDifficultyLabel);

            var pos = new Vector2(394, 553);
            pages[0].subObjects.Add(new MenuLabel(this, pages[0], Translate("Slugcats"), pos, new(110, 30), true));

            scugButtons = new EventfulSelectOneButton[slugcatColorOrder.Count];
            for (var i = 0; i < slugcatColorOrder.Count; i++)
            {
                var scug = slugcatColorOrder[i];
                pos -= new Vector2(0, 38);
                var btn = new EventfulSelectOneButton(this, pages[0], SlugcatStats.getSlugcatName(scug), "scugButtons", pos, new Vector2(110, 30), scugButtons, i);
                pages[0].subObjects.Add(btn);

                btn.OnClick += (_) =>
                {
                    storyGameMode.avatarSettings.playingAs = scug;

                    if (colorChecked)
                    {
                        RemoveColorButtons();
                        AddColorButtons();
                    }
                };
            }
        }

        private void SetupOnlineCustomization()
        {
            personaSettings = storyGameMode.avatarSettings;
            personaSettings.playingAs = slugcatPages[slugcatPageIndex].slugcatNumber;
            personaSettings.bodyColor = RainMeadow.rainMeadowOptions.BodyColor.Value.SafeColorRange();
            personaSettings.eyeColor = RainMeadow.rainMeadowOptions.EyeColor.Value.SafeColorRange();
        }

        private void RemoveExcessStoryObjects()
        {
            if (!OnlineManager.lobby.isOwner)
            {
                StoryMenuHelpers.RemoveMenuObjects(restartCheckbox, nextButton, prevButton);

                foreach (var page in slugcatPages)
                {
                    switch (page)
                    {
                        case SlugcatPageContinue continuePage:
                            StoryMenuHelpers.RemoveMenuObjects(continuePage.regionLabel);
                            foreach (var part in continuePage.hud.parts.Where(x => x is HUD.KarmaMeter or HUD.FoodMeter).ToList())
                            {
                                part.slatedForDeletion = true;
                                part.ClearSprites();
                                continuePage.hud.parts.Remove(part);
                            }
                            break;
                        case SlugcatPageNewGame newPage:
                            StoryMenuHelpers.RemoveMenuObjects(newPage.infoLabel, newPage.difficultyLabel);
                            break;
                    }
                }
            }
        }

        private void SetupOnlineMenuItems()
        {
            // Player lobby label
            var lobbyLabel = new MenuLabel(this, pages[0], Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true);
            pages[0].subObjects.Add(lobbyLabel);

            var invite = new SimplerButton(this, pages[0], Translate("Invite Friends"), new(nextButton.pos.x + 80f, 50f), new(110, 35));
            invite.OnClick += (_) =>
            {
                SteamFriends.ActivateGameOverlay("friends");
            };
            pages[0].subObjects.Add(invite);

            var sameSpotOtherSide = restartCheckboxPos.x - startButton.pos.x;
            friendlyFire = new CheckBox(this, pages[0], this, new Vector2(startButton.pos.x - sameSpotOtherSide, restartCheckboxPos.y + 30), 70f, Translate("Friendly Fire"), "ONLINEFRIENDLYFIRE", false);
            if (!OnlineManager.lobby.isOwner) friendlyFire.buttonBehav.greyedOut = true;
            pages[0].subObjects.Add(friendlyFire);
        }

        private void ModifyExistingMenuItems()
        {
            foreach (var obj in pages[0].subObjects) // unfortunate locally declared variable.
            {
                if (obj is SimpleButton button && button.signalText == "BACK")
                {
                    button.pos = new Vector2(prevButton.pos.x - 140f, 50f);
                }
            }
        }

        private void SetupClientOptions()
        {
            clientWantsToOverwriteSave = new CheckBox(this, pages[0], this, restartCheckboxPos, 70f, Translate("Match save"), "CLIENTSAVERESET", false);
            pages[0].subObjects.Add(clientWantsToOverwriteSave);
        }
    }
}
