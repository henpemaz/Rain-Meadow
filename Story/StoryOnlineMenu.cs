using HarmonyLib;
using Menu;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class StoryOnlineMenu : SlugcatSelectMenu
    {
        CheckBox clientWantsToOverwriteSave;
        CheckBox friendlyFire;
        CheckBox reqCampaignSlug;
        MenuLabel? lobbyLabel, slugcatLabel;
        ButtonScroller? playerScrollBox;
        StoryMenuSlugcatSelector? slugcatSelector;
        SlugcatCustomization personaSettings;
        SlugcatStats.Name[] selectableSlugcats;
        SlugcatStats.Name? currentSlugcat, playerSelectedSlugcat;
        StoryGameMode storyGameMode;
        MenuLabel onlineDifficultyLabel;
        Vector2 restartCheckboxPos;
        public SlugcatStats.Name[] SelectableSlugcats
        {
            get
            {
                SetupSelectableSlugcats();
                return selectableSlugcats;
            }
        }
        public SlugcatStats.Name PlayerSelectedSlugcat
        {
            get
            {
                return playerSelectedSlugcat ?? SelectableSlugcats[slugcatPageIndex];
            }
            set
            {
                playerSelectedSlugcat = value == slugcatColorOrder[slugcatPageIndex]? null : value;
                CurrentSlugcat = PlayerSelectedSlugcat;
            }
        }
        public SlugcatStats.Name CurrentSlugcat
        {
            get
            {
                return currentSlugcat ?? SelectableSlugcats[slugcatPageIndex];
            }
            set
            {
                if (currentSlugcat != value)
                {
                    RemoveColorButtons();
                    currentSlugcat = value;
                    UpdateUponChangingSlugcat(currentSlugcat);
                }
            }
        }
        public static int MaxVisibleOnList => 8;
        public static float ButtonSpacingOffset => 8;
        public static float ButtonSizeWithSpacing => ButtonSize + ButtonSpacingOffset;
        public static float ButtonSize => 30;


        public StoryOnlineMenu(ProcessManager manager) : base(manager)
        {
            SetupSelectableSlugcats();
            ID = OnlineManager.lobby.gameMode.MenuProcessId();
            storyGameMode = (StoryGameMode)OnlineManager.lobby.gameMode;

            storyGameMode.Sanitize();
            storyGameMode.currentCampaign = slugcatPages[slugcatPageIndex].slugcatNumber;
            restartCheckboxPos = restartCheckbox.pos;       
            RemoveExcessStoryObjects();
            ModifyExistingMenuItems();

            if (OnlineManager.lobby.isOwner)
            {
                storyGameMode.requireCampaignSlugcat = false; // Default option is in remix menu.
                storyGameMode.saveToDisk = true;
            }
            else
            {
                SetupClientOptions();
            }

            // HACK: force-register MMFEnums.SliderID because god is dead and we have killed them
            // turns out god not dead
            //MoreSlugcats.MMFEnums.SliderID.RegisterValues();

            SetupOnlineCustomization();

            SetupOnlineMenuItems();
            UpdatePlayerList();

            MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        }

        public void SetupSelectableSlugcats() {
            if (selectableSlugcats == null) {
                var SelectableSlugcatsEnumerable = slugcatColorOrder.AsEnumerable();
                if (ModManager.MSC) {
                    if (!SelectableSlugcatsEnumerable.Contains(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)) {
                        SelectableSlugcatsEnumerable = SelectableSlugcatsEnumerable.Append(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup);
                    }
                }
                selectableSlugcats = SelectableSlugcatsEnumerable.ToArray();
            }
        }   

        public new void StartGame(SlugcatStats.Name storyGameCharacter)
        {
            if (OnlineManager.lobby.isOwner)
            {
                storyGameMode.currentCampaign = storyGameCharacter;
            }
            personaSettings.playingAs = storyGameMode.requireCampaignSlugcat ? storyGameMode.currentCampaign : PlayerSelectedSlugcat; //double check just incase

            // TODO: figure out how to reuse vanilla StartGame
            // * override singleplayer custom colours
            // * fix intro cutscenes messing with resource acquisition
            // ? how to deal with statistics screen (not supposed to continue, we should require wipe)
            personaSettings.currentColors = this.GetCustomColors(personaSettings.playingAs); //abt colors, color config updates to campaign when required campaign is on. Client side, the host still needs to be in the menu to update it so they will notice the color config update
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

            if (OnlineManager.lobby == null) return;

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
                if (onlineDifficultyLabel == null)
                {
                    onlineDifficultyLabel = new MenuLabel(this, pages[0], $"{GetCurrentCampaignName()}", new Vector2(startButton.pos.x - 100f, startButton.pos.y + 100f), new Vector2(200f, 30f), bigText: true);
                    onlineDifficultyLabel.label.alignment = FLabelAlignment.Center;
                    onlineDifficultyLabel.label.alpha = 0.5f;
                    pages[0].subObjects.Add(onlineDifficultyLabel);
                }
                if (startButton != null)
                {
                    startButton.buttonBehav.greyedOut = !storyGameMode.canJoinGame;
                }
                if (onlineDifficultyLabel != null)
                {
                    onlineDifficultyLabel.text = GetCurrentCampaignName() + (string.IsNullOrEmpty(storyGameMode.region) ? Translate(" - New Game") : " - " + Translate(storyGameMode.region));
                }
            }
            if (storyGameMode.requireCampaignSlugcat)
            {
                RemoveSlugcatList();
                CurrentSlugcat = storyGameMode.currentCampaign;
            }
            else
            {
                SetupSlugcatList();
                CurrentSlugcat = PlayerSelectedSlugcat;
            }
            if (slugcatSelector != null)
            {
                slugcatSelector.Slug = CurrentSlugcat;
            }

        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game) // if join on sleep/deathscreen this needs to be added here as well
            {
                OnlineManager.LeaveLobby();
            }
            RainMeadow.rainMeadowOptions._SaveConfigFile(); //im just gonna allow this for now, since idk updates in slider set value
            base.ShutDownProcess();
        }
        public override string UpdateInfoText()
        {
            return selectedObject is IHaveADescription descObj ? descObj.Description : base.UpdateInfoText();
        }
        private void UpdatePlayerList()
        {
            playerScrollBox?.RemoveAllButtons(false);
            if (playerScrollBox == null)
            {
                playerScrollBox = new(this, pages[0], new(194, 553 - 30 - ButtonScroller.CalculateHeightBasedOnAmtOfButtons(MaxVisibleOnList, ButtonSize, ButtonSpacingOffset)), MaxVisibleOnList, 200, ButtonSize, ButtonSpacingOffset);
                pages[0].subObjects.Add(playerScrollBox);
            }
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                StoryMenuPlayerButton playerButton = new(this, playerScrollBox, player, OnlineManager.lobby.isOwner && player != OnlineManager.lobby.owner);
                playerScrollBox.AddScrollObjects(playerButton);
            }
            playerScrollBox.ConstrainScroll();

        }

        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            if (RainMeadow.isStoryMode(out var _))
            {
                UpdatePlayerList();
            }
        }

        public string GetCurrentCampaignName() => Translate("Current Campaign: ") + Translate(SlugcatStats.getSlugcatName(storyGameMode.currentCampaign));

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
            Vector2 pos = new(394, 553);
            if (slugcatLabel == null)
            {
                slugcatLabel = new(this, pages[0], Translate("Selected Slugcat"), pos, new(110, 30), true);
                pages[0].subObjects.Add(slugcatLabel);
            }
            if (slugcatSelector == null)
            {
                //first player button is 30 pos below size of list. and list top part is 30 below the title. Plus
                slugcatSelector = new(this, pages[0], new(pos.x, pos.y - (ButtonSize * 2)), MaxVisibleOnList, ButtonSpacingOffset, CurrentSlugcat, GetSlugcatSelectionButtons);
                pages[0].subObjects.Add(slugcatSelector);
            }

        }
        private void RemoveSlugcatList()
        {
            pages[0].ClearMenuObject(ref slugcatLabel);
            pages[0].ClearMenuObject(ref slugcatSelector);
        }

        private void SetupOnlineCustomization()
        {
            personaSettings = storyGameMode.avatarSettings;
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
            lobbyLabel = new MenuLabel(this, pages[0], Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true);
            pages[0].subObjects.Add(lobbyLabel);

            var invite = new SimplerButton(this, pages[0], Translate("Invite Friends"), new(nextButton.pos.x + 80f, 50f), new(110, 35));
            invite.OnClick += (_) => MatchmakingManager.currentInstance.OpenInvitationOverlay();
            pages[0].subObjects.Add(invite);

            var sameSpotOtherSide = restartCheckboxPos.x - startButton.pos.x;
            friendlyFire = new CheckBox(this, pages[0], this, new Vector2(startButton.pos.x - sameSpotOtherSide, restartCheckboxPos.y + 30), 70f, Translate("Friendly Fire"), "ONLINEFRIENDLYFIRE", false);
            reqCampaignSlug = new CheckBox(this, pages[0], this, new Vector2(startButton.pos.x - sameSpotOtherSide, restartCheckboxPos.y), 150f, Translate("Require Campaign Slugcat"), "CAMPAIGNSLUGONLY", false);
            if (!OnlineManager.lobby.isOwner)
            {
                friendlyFire.buttonBehav.greyedOut = true;
                reqCampaignSlug.buttonBehav.greyedOut = true;
            }
            pages[0].subObjects.Add(friendlyFire);
            pages[0].subObjects.Add(reqCampaignSlug);
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
        public StoryMenuSlugcatButton[] GetSlugcatSelectionButtons(StoryMenuSlugcatSelector slugcatSelector, ButtonScroller buttonScroller)
        {
            List<StoryMenuSlugcatButton> slugcatButtons = [];
            for (int i = 0; i < SelectableSlugcats.Length; i++)
            {
                if (SelectableSlugcats[i] != slugcatSelector.Slug)
                {
                    StoryMenuSlugcatButton storyMenuSlugcatButton = new(this, buttonScroller, SelectableSlugcats[i], (scug) =>
                    {
                        PlayerSelectedSlugcat = scug;
                        slugcatSelector.OpenCloseList(false, true, true);
                    });
                    slugcatButtons.Add(storyMenuSlugcatButton);
                }
            }
            return [.. slugcatButtons];
        }
        private void UpdateUponChangingSlugcat(SlugcatStats.Name scug)
        {
            if (colorsCheckbox != null)
            {
                colorsCheckbox.Checked = colorChecked; //this.IsCustomColorEnabled(scug); //automatically opens color interface if enabled
            }
        }
    }
}
