using HarmonyLib;
using Menu;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class StoryOnlineMenu : SlugcatSelectMenu, SelectOneButton.SelectOneButtonOwner
    {
        CheckBox clientWantsToOverwriteSave;
        CheckBox friendlyFire;
        CheckBox reqCampaignSlug;
        List<SimplerButton> playerButtons = new();
        List<SimplerSymbolButton> kicButtons = new();
        MenuLabel lobbyLabel;
        MenuLabel slugcatLabel;

        SlugcatCustomization personaSettings;
        EventfulSelectOneButton[] scugButtons;
        public SlugcatStats.Name[] selectableSlugcats { get; private set; }

        StoryGameMode storyGameMode;
        MenuLabel onlineDifficultyLabel;
        Vector2 restartCheckboxPos;
        public int actualSelectedIndex = -1;
        public int SelectedIndex
        {
            get => (actualSelectedIndex < 0 || (OnlineManager.lobby?.isOwner ?? false)) ? slugcatPageIndex : actualSelectedIndex; private set
            {
                actualSelectedIndex = value;
            }
        }

        public StoryOnlineMenu(ProcessManager manager) : base(manager)
        {
            ID = OnlineManager.lobby.gameMode.MenuProcessId();
            storyGameMode = (StoryGameMode)OnlineManager.lobby.gameMode;

            SelectedIndex = slugcatPageIndex;
            storyGameMode.Sanitize();
            storyGameMode.currentCampaign = slugcatPages[slugcatPageIndex].slugcatNumber;

            restartCheckboxPos = restartCheckbox.pos;


            SetupSelectableSlugcats();
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
            MoreSlugcats.MMFEnums.SliderID.RegisterValues();

            SetupOnlineCustomization();

            SetupOnlineMenuItems();
            UpdatePlayerList();

            MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        }

        public void SetupSelectableSlugcats()
        {
            if (selectableSlugcats == null)
            {
                var SelectableSlugcatsEnumerable = slugcatColorOrder.AsEnumerable();
                if (ModManager.MSC)
                {
                    if (!SelectableSlugcatsEnumerable.Contains(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup))
                    {
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
                personaSettings.playingAs = storyGameMode.currentCampaign = storyGameCharacter;
            }
            else
            {
                if (storyGameMode.requireCampaignSlugcat) // I'm a client and I want to match the host's
                {
                    personaSettings.playingAs = storyGameMode.currentCampaign;
                }
            }

            // TODO: figure out how to reuse vanilla StartGame
            // * override singleplayer custom colours
            // * fix intro cutscenes messing with resource acquisition
            // ? how to deal with statistics screen (not supposed to continue, we should require wipe)

            if (this.colorChecked)
            {
                List<Color> val = new();
                for (int i = 0; i < manager.rainWorld.progression.miscProgressionData.colorChoices[selectableSlugcats[SelectedIndex].value].Count; i++)
                {
                    Vector3 vector = new Vector3(1f, 1f, 1f);
                    if (manager.rainWorld.progression.miscProgressionData.colorChoices[selectableSlugcats[SelectedIndex].value][i].Contains(","))
                    {
                        string[] array = manager.rainWorld.progression.miscProgressionData.colorChoices[selectableSlugcats[SelectedIndex].value][i].Split(new char[1] { ',' });
                        vector = new Vector3(float.Parse(array[0], (NumberStyles)511, (IFormatProvider)(object)CultureInfo.InvariantCulture), float.Parse(array[1], (NumberStyles)511, (IFormatProvider)(object)CultureInfo.InvariantCulture), float.Parse(array[2], (NumberStyles)511, (IFormatProvider)(object)CultureInfo.InvariantCulture));
                    }
                    val.Add(RWCustom.Custom.HSL2RGB(vector[0], vector[1], vector[2]));
                }

                personaSettings.currentColors = val;
            }
            else
            {
                // Use the default colors for this slugcat when the checkbox is unchecked
                personaSettings.currentColors = PlayerGraphics.DefaultBodyPartColorHex(selectableSlugcats[SelectedIndex]).Select(Custom.hexToColor).ToList();
            }
            manager.arenaSitting = null;
            if (restartCheckbox != null && restartCheckbox.Checked)
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

        private static List<Color> GetSlugcatColorsFromMiscProg(SlugcatStats.Name id)
        {
            var miscProg = Custom.rainWorld.progression.miscProgressionData;

            if (!miscProg.colorsEnabled.TryGetValue(id.value, out var colorsEnabled))
            {
                return [];
            }

            if (!colorsEnabled)
            {
                return [];
            }

            if (!miscProg.colorChoices.TryGetValue(id.value, out var partColorStrings))
            {
                return [];
            }

            var partColors = new List<Color>();

            foreach (var partColorString in partColorStrings)
            {
                // Shouldn't happen, means that the color save data is malformed
                if (!partColorString.Contains(","))
                {
                    partColors.Add(Color.magenta);
                    continue;
                }

                var hslString = partColorString.Split([',']);
                var hsl = new Vector3(float.Parse(hslString[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslString[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslString[2], NumberStyles.Any, CultureInfo.InvariantCulture));

                var partColor = Custom.HSL2RGB(hsl[0], hsl[1], hsl[2]);
                partColors.Add(partColor);
            }

            return partColors;
        }

        public override void Update()
        {
            base.Update();

            if (OnlineManager.lobby == null) return;

            if (!OnlineManager.lobby.isOwner)
            {

                if (onlineDifficultyLabel == null)
                {
                    onlineDifficultyLabel = new MenuLabel(this, pages[0], $"{GetCurrentCampaignName()}", new Vector2(startButton.pos.x - 100f, startButton.pos.y + 100f), new Vector2(200f, 30f), bigText: true);
                    onlineDifficultyLabel.label.alignment = FLabelAlignment.Center;
                    onlineDifficultyLabel.label.alpha = 0.5f;
                    pages[0].subObjects.Add(onlineDifficultyLabel);
                }
                // Remove all buttons scug buttons if requireCampaignSlugcat is on.
                if (scugButtons != null && storyGameMode.requireCampaignSlugcat)
                {
                    foreach (var button in scugButtons)
                    {
                        pages[0].subObjects.Remove(button);
                    };

                    scugButtons = null;
                }
                if (scugButtons != null)
                {
                    foreach (var button in scugButtons)
                    {
                        if (button != null)
                        {
                            button.pos.y = InputOverride.MoveMenuItemFromYInput(button.pos.y);
                        }
                    };
                }

                // Recall buttons scug buttons if requireCampaignSlugcat is off.    
                if (scugButtons == null && !storyGameMode.requireCampaignSlugcat)
                {
                    SetupSlugcatList();
                }

            }


            if (playerButtons != null)
            {
                playerButtons.ForEach(x => x.pos.y = InputOverride.MoveMenuItemFromYInput(x.pos.y));
                if (OnlineManager.lobby.isOwner && kicButtons != null)
                {
                    kicButtons.ForEach(k => k.pos.y = InputOverride.MoveMenuItemFromYInput(k.pos.y));
                }
            }

            if (slugcatLabel != null)
            {
                slugcatLabel.pos.y = InputOverride.MoveMenuItemFromYInput(slugcatLabel.pos.y);
            }
            if (lobbyLabel != null)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    lobbyLabel.pos.y = InputOverride.MoveMenuItemFromYInput(lobbyLabel.pos.y);
                }
                else
                {
                    if (slugcatLabel != null)
                    {
                        lobbyLabel.pos.y = slugcatLabel.pos.y;
                    }
                }
            }


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
                    onlineDifficultyLabel.text = GetCurrentCampaignName() + (string.IsNullOrEmpty(storyGameMode.region) ? Translate(" - New Game") : " - " + Translate(storyGameMode.region));
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
            kicButtons.Do(x => StoryMenuHelpers.RemoveMenuObjects(x));
            kicButtons.Clear();

            var pos = new Vector2(194, lobbyLabel.pos.y);
            RainMeadow.Debug("============" + lobbyLabel.pos.y);
            RainMeadow.Debug(lobbyLabel.size.y);
            var widthHeight = new Vector2(110, 30);
            foreach (var playerInfo in OnlineManager.players)
            {

                pos -= new Vector2(0, 38);
                var btn = new SimplerButton(this, this.pages[0], playerInfo.id.name, pos, widthHeight);
                btn.OnClick += (_) => playerInfo.id.OpenProfileLink();

                playerButtons.Add(btn);

                if (OnlineManager.lobby.isOwner && playerInfo != OnlineManager.lobby.owner)
                {
                    var kickBtn = new SimplerSymbolButton(this, this.pages[0], "Menu_Symbol_Clear_All", "KICKPLAYER", new Vector2(pos.x + widthHeight.x + 15f, pos.y));
                    kickBtn.OnClick += (_) => BanHammer.BanUser(playerInfo);
                    kicButtons.Add(kickBtn);

                }
            }
            foreach (var btn in playerButtons)
            {
                this.pages[0].subObjects.Add(btn);
            }

            if (OnlineManager.lobby.isOwner)
            {
                foreach (var kickBtn in kicButtons)
                {
                    this.pages[0].subObjects.Add(kickBtn);
                }
            }

        }

        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            if (RainMeadow.isStoryMode(out var _))
            {
                UpdatePlayerList();
            }
        }

        private void AddLobbyLabel()
        {
            lobbyLabel = new MenuLabel(this, pages[0], Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true);
            this.pages[0].subObjects.Add(lobbyLabel);
        }
        public int GetCurrentlySelectedOfSeries(string series) => series switch
        {
            "scugButtons" => !RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value ? -1 : SelectedIndex,
            _ => -1,
        };

        public void SetCurrentlySelectedOfSeries(string series, int to)
        {
            switch (series)
            {
                case "scugButtons":
                    SelectedIndex = to;
                    if (to >= slugcatPages.Count)
                    {
                        to = 0;
                    }

                    slugcatPageIndex = to;
                    return;
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

            var pos = new Vector2(394, 553);
            slugcatLabel = new MenuLabel(this, pages[0], Translate("Slugcats"), pos, new(110, 30), true);
            pages[0].subObjects.Add(slugcatLabel);
            scugButtons = new EventfulSelectOneButton[selectableSlugcats.Length];
            for (var i = 0; i < selectableSlugcats.Length; i++)
            {
                var scug = selectableSlugcats[i];
                pos -= new Vector2(0, 38);
                var btn = new EventfulSelectOneButton(this, pages[0], Translate(SlugcatStats.getSlugcatName(scug)), "scugButtons", pos, new Vector2(110, 30), scugButtons, i);
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
                scugButtons[i] = btn;
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
            AddLobbyLabel();

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
    }
}
