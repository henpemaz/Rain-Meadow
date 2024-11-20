using Menu;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HUD;
using RWCustom;
namespace RainMeadow
{
    internal static class StoryMenuHelpers
    {
        #region Remix
        public static List<string> nonCampaignSlugcats = new List<string> { "Night", "Inv", "Slugpup", "MeadowOnline", "MeadowOnlineRemote" };

        public static List<string> nonGameplayRemixSettings = new List<string> { "cfgSpeedrunTimer", "cfgHideRainMeterNoThreat", "cfgLoadingScreenTips", "cfgExtraTutorials", "cfgClearerDeathGradients", "cfgShowUnderwaterShortcuts", "cfgBreathTimeVisualIndicator", "cfgCreatureSense", "cfgTickTock", "cfgFastMapReveal", "cfgThreatMusicPulse", "cfgExtraLizardSounds", "cfgQuieterGates", "cfgDisableScreenShake", "cfgHunterBatflyAutograb", "cfgNoMoreTinnitus" };

        internal static (Dictionary<string, bool> hostBoolSettings, Dictionary<string, float> hostFloatSettings, Dictionary<string, int> hostIntSettings) GetHostBoolStoryRemixSettings()
        {
            Dictionary<string, bool> configurableBools = new Dictionary<string, bool>();
            Dictionary<string, float> configurableFloats = new Dictionary<string, float>();
            Dictionary<string, int> configurableInts = new Dictionary<string, int>();

            if (ModManager.MMF)
            {
                Type type = typeof(MoreSlugcats.MMF);

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                var sortedFields = fields.OrderBy(f => f.Name);

                foreach (FieldInfo field in sortedFields)
                {
                    if (nonGameplayRemixSettings.Contains(field.Name)) continue;
                    var reflectedValue = field.GetValue(null);
                    if (reflectedValue is Configurable<bool> boolOption)
                    {
                        configurableBools.Add(field.Name, boolOption._typedValue);
                    }

                    if (reflectedValue is Configurable<float> floatOption)
                    {
                        configurableFloats.Add(field.Name, floatOption._typedValue);
                    }

                    if (reflectedValue is Configurable<int> intOption)
                    {
                        configurableInts.Add(field.Name, intOption._typedValue);
                    }
                }
                RainMeadow.Debug(configurableBools);
                RainMeadow.Debug(configurableInts);
                RainMeadow.Debug(configurableFloats);
                return (configurableBools, configurableFloats, configurableInts);
            }
            return (configurableBools, configurableFloats, configurableInts);
        }

        internal static void SetClientStoryRemixSettings(Dictionary<string, bool> hostBoolRemixSettings, Dictionary<string, float> hostFloatRemixSettings, Dictionary<string, int> hostIntRemixSettings)
        {

            Type type = typeof(MoreSlugcats.MMF);

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            var sortedFields = fields.OrderBy(f => f.Name);

            foreach (FieldInfo field in sortedFields)
            {
                var reflectedValue = field.GetValue(null);
                if (reflectedValue is Configurable<bool> boolOption)
                {
                    for (int i = 0; i < hostBoolRemixSettings.Count; i++)
                    {
                        if (field.Name == hostBoolRemixSettings.Keys.ElementAt(i) && boolOption._typedValue != hostBoolRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {boolOption._typedValue} does not match host's, setting to {hostBoolRemixSettings.Values.ElementAt(i)}");
                            boolOption._typedValue = hostBoolRemixSettings.Values.ElementAt(i);

                        }
                    }
                }

                if (reflectedValue is Configurable<float> floatOption)
                {
                    for (int i = 0; i < hostFloatRemixSettings.Count; i++)
                    {
                        if (field.Name == hostFloatRemixSettings.Keys.ElementAt(i) && floatOption._typedValue != hostFloatRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {floatOption._typedValue} does not match host's, setting to {hostFloatRemixSettings.Values.ElementAt(i)}");
                            floatOption._typedValue = hostFloatRemixSettings.Values.ElementAt(i);

                        }
                    }
                }

                if (reflectedValue is Configurable<int> intOption)
                {
                    for (int i = 0; i < hostIntRemixSettings.Count; i++)
                    {

                        if (field.Name == hostIntRemixSettings.Keys.ElementAt(i) && intOption._typedValue != hostIntRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {intOption._typedValue} does not match host's, setting to {hostIntRemixSettings.Values.ElementAt(i)}");
                            intOption._typedValue = hostIntRemixSettings.Values.ElementAt(i);

                        }
                    }
                }
            }
        }

        internal static void SanitizeStoryClientSettings(StoryClientSettingsData clientSettings)
        {
            clientSettings.readyForWin = false;
            clientSettings.isDead = false;
        }

        internal static void SanitizeStoryGameMode(StoryGameMode gameMode)
        {
            gameMode.isInGame = false;
            gameMode.changedRegions = false;
            gameMode.didStartCycle = false;
            gameMode.defaultDenPos = null;
            gameMode.ghostsTalkedTo = new();
            gameMode.consumedItems = new();
            gameMode.myLastDenPos = null;
            gameMode.hasSheltered = false;
            gameMode.region = null;
        }

        #endregion


        #region StoryObjects
        internal static void RemoveExcessStoryObjects(StoryMenuRedux storyMenu, StoryGameMode storyModeOnline)
        {
            if (storyMenu.startButton != null)
            {
                storyMenu.startButton.RemoveSprites();
                storyMenu.pages[0].RemoveSubObject(storyMenu.startButton);
            }

            if (!OnlineManager.lobby.isOwner && storyMenu.restartCheckbox != null)
            {
                storyMenu.restartCheckbox.RemoveSprites();
                storyMenu.pages[0].RemoveSubObject(storyMenu.restartCheckbox);
            }

            if (!OnlineManager.lobby.isOwner)
            {
                if (storyMenu.nextButton != null)
                {
                    storyMenu.nextButton.RemoveSprites();
                    storyMenu.pages[0].RemoveSubObject(storyMenu.nextButton);
                }
                if (storyMenu.prevButton != null)
                {
                    storyMenu.prevButton.RemoveSprites();
                    storyMenu.pages[0].RemoveSubObject(storyMenu.prevButton);
                }

                if ((storyMenu.slugcatPages[storyMenu.indexFromColor(storyModeOnline.currentCampaign)] != null && storyMenu.slugcatPages[storyMenu.indexFromColor(storyModeOnline.currentCampaign)] is SlugcatSelectMenu.SlugcatPageContinue p))
                {
                    p.regionLabel.RemoveSprites();
                    storyMenu.pages[0].RemoveSubObject(p.regionLabel);
                }

                if ((storyMenu.slugcatPages[storyMenu.indexFromColor(storyModeOnline.currentCampaign)] != null && storyMenu.slugcatPages[storyMenu.indexFromColor(storyModeOnline.currentCampaign)] is SlugcatSelectMenu.SlugcatPageNewGame pN))
                {
                    pN.difficultyLabel.RemoveSprites();
                    pN.infoLabel.RemoveSprites();
                    storyMenu.pages[0].RemoveSubObject(pN.infoLabel);
                    storyMenu.pages[0].RemoveSubObject(pN.difficultyLabel);
                }

            }
        }

        internal static void SetupOnlineMenuItems(StoryMenuRedux storyMenu, StoryGameMode storyModeOnline)
        {
            // Music
            storyMenu.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;

            // Player lobby label
            var lobbyLabel = new MenuLabel(storyMenu, storyMenu.pages[0], storyMenu.Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true);
            storyMenu.pages[0].subObjects.Add(lobbyLabel);

            var invite = new SimplerButton[1];
            invite[0] = new SimplerButton(storyMenu, storyMenu.pages[0], storyMenu.Translate("Invite Friends"), new(storyMenu.nextButton.pos.x + 80f, 50f), new(110, 35));
            storyMenu.pages[0].subObjects.Add(invite[0]);

            invite[0].OnClick += (_) =>
            {
                SteamFriends.ActivateGameOverlay("friends");
            };

            if (RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value && !OnlineManager.lobby.isOwner)
            {
                storyMenu.pages[0].subObjects.Add(new MenuLabel(storyMenu, storyMenu.pages[0], storyMenu.Translate("Slugcats"), new Vector2(394, 553), new(110, 30), true));
            }

            if (!OnlineManager.lobby.isOwner && (storyMenu.slugcatPages[storyMenu.indexFromColor(storyModeOnline.currentCampaign)] != null && storyMenu.slugcatPages[storyMenu.indexFromColor(storyModeOnline.currentCampaign)] is SlugcatSelectMenu.SlugcatPage p))
            {

                storyMenu.onlineDifficultyLabel = new MenuLabel(storyMenu, storyMenu.pages[0], $"{GetCurrentCampaignName(storyModeOnline)}", new Vector2(storyMenu.startButton.pos.x - 100f, storyMenu.startButton.pos.y + 100f), new Vector2(200f, 30f), bigText: true);
                storyMenu.onlineDifficultyLabel.label.alignment = FLabelAlignment.Center;
                storyMenu.onlineDifficultyLabel.label.alpha = 0.5f;
                storyMenu.pages[0].subObjects.Add(storyMenu.onlineDifficultyLabel);
            }

            var sameSpotOtherSide = storyMenu.colorsCheckbox.pos.x - storyMenu.startButton.pos.x;
            storyMenu.friendlyFire = new CheckBox(storyMenu, storyMenu.pages[0], storyMenu, new Vector2(storyMenu.startButton.pos.x - sameSpotOtherSide, storyMenu.colorsCheckbox.pos.y), 70f, storyMenu.Translate("Friendly Fire"), "ONLINEFRIENDLYFIRE", false);
            if (!OnlineManager.lobby.isOwner) storyMenu.friendlyFire.buttonBehav.greyedOut = true;
            storyMenu.pages[0].subObjects.Add(storyMenu.friendlyFire);
        }

        internal static void ModifyExistingMenuItems(StoryMenuRedux storyMenu)
        {
            foreach (var obj in storyMenu.pages[0].subObjects) // unfortunate locally declared variable.
            {
                if (obj is SimpleButton && (obj as SimpleButton).signalText == "BACK")
                {

                    (obj as SimpleButton).pos = new Vector2(storyMenu.prevButton.pos.x - 140f, 50f);

                }
            }

        }

        #endregion

        #region SlugcatSetup
        private static List<SlugcatStats.Name> AllSlugcats()
        {
            var filteredList = new List<SlugcatStats.Name>();
            foreach (var name in SlugcatStats.Name.values.entries.Except(nonCampaignSlugcats))
            {
                if (ExtEnumBase.TryParse(typeof(SlugcatStats.Name), name, false, out var rawEnumBase))
                {
                    filteredList.Add((SlugcatStats.Name)rawEnumBase);
                }
            }
            return filteredList;
        }

        public static string GetCurrentCampaignName(StoryGameMode storyModeOnline)
        {
            return "Current Campaign: " + SlugcatStats.getSlugcatName(storyModeOnline.currentCampaign);
        }

        internal static void CustomSlugcatSetup(StoryMenuRedux storyMenu, StoryGameMode storyModeOnline)
        {

            var slugList = AllSlugcats();
            var slugButtons = new EventfulSelectOneButton[slugList.Count];


            for (int i = 0; i < slugButtons.Length; i++)
            {
                var slug = slugList[i];
                var btn = new SimplerButton(storyMenu, storyMenu.pages[0], SlugcatStats.getSlugcatName(slug), new Vector2(394, 515) - i * new Vector2(0, 38), new Vector2(110, 30));
                btn.toggled = false;
                storyMenu.pages[0].subObjects.Add(btn);

                // Store the current button in a variable accessible by the lambda
                var currentBtn = btn;
                btn.OnClick += (_) =>
                {
                    // Set the clicked button to true
                    currentBtn.toggled = !currentBtn.toggled;
                    //storyMenu.customSelectedSlugcat =
                    storyModeOnline.avatarSettings.playingAs = slug;


                    // Set all other buttons to false
                    foreach (var otherBtn in storyMenu.pages[0].subObjects.OfType<SimplerButton>())
                    {
                        if (otherBtn != currentBtn)
                        {
                            otherBtn.toggled = false;
                        }

                    }

                };
            }
        }

        internal static void SetupOnlineCustomization(StoryMenuRedux storyMenu)
        {

            storyMenu.personaSettings = (OnlineManager.lobby.gameMode as StoryGameMode).avatarSettings;
            storyMenu.personaSettings.playingAs = storyMenu.slugcatPages[storyMenu.slugcatPageIndex].slugcatNumber;
            storyMenu.personaSettings.bodyColor = Extensions.SafeColorRange(RainMeadow.rainMeadowOptions.BodyColor.Value);
            storyMenu.personaSettings.eyeColor = Extensions.SafeColorRange(RainMeadow.rainMeadowOptions.EyeColor.Value);

        }

        #endregion

        #region Host vs Client
        internal static void SetupHostMenu(StoryMenuRedux storyMenu, StoryGameMode storyModeOnline)
        {
            storyMenu.hostStartButton = new EventfulHoldButton(storyMenu, storyMenu.pages[0], storyMenu.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            storyMenu.hostStartButton.OnClick += (_) => { storyMenu.StartGame(); };
            storyMenu.hostStartButton.buttonBehav.greyedOut = false;
            storyMenu.pages[0].subObjects.Add(storyMenu.hostStartButton);

            try
            {
                if (storyMenu.saveGameData[storyModeOnline.currentCampaign].shelterName != null && storyMenu.saveGameData[storyModeOnline.currentCampaign].shelterName.Length > 2)

                {
                    storyModeOnline.region = Region.GetRegionFullName(storyMenu.saveGameData[storyModeOnline.currentCampaign].shelterName.Substring(0, 2), storyModeOnline.currentCampaign);
                }
            }
            catch
            {
                RainMeadow.Error("Error getting next region name");
                storyModeOnline.region = "";

            }


        }

        internal static void SetupClientMenu(StoryMenuRedux storyMenu, StoryGameMode storyModeOnline)
        {

            storyMenu.clientWaitingButton = new EventfulHoldButton(storyMenu, storyMenu.pages[0], storyMenu.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            storyMenu.clientWaitingButton.OnClick += (_) => { storyMenu.StartGame(); };
            storyMenu.clientWaitingButton.buttonBehav.greyedOut = !(storyModeOnline.isInGame && !storyModeOnline.changedRegions);

            storyMenu.pages[0].subObjects.Add(storyMenu.clientWaitingButton);

            var sameSpotOtherSide = storyMenu.colorsCheckbox.pos.x - storyMenu.startButton.pos.x;

            storyMenu.clientWantsToOverwriteSave = new CheckBox(storyMenu, storyMenu.pages[0], storyMenu, new Vector2(storyMenu.colorsCheckbox.pos.x, storyMenu.colorsCheckbox.pos.y - 30f), 70f, storyMenu.Translate("Match save"), "CLIENTSAVERESET", false);
            storyMenu.pages[0].subObjects.Add(storyMenu.clientWantsToOverwriteSave);


            if (storyMenu.slugcatPages[storyMenu.indexFromColor(storyModeOnline.currentCampaign)] != null && storyMenu.slugcatPages[storyMenu.indexFromColor(storyModeOnline.currentCampaign)] is SlugcatSelectMenu.SlugcatPageContinue p)
            {
                storyMenu.campaignContainer.pos = p.KarmaSymbolPos;

                GetRegionAndCampaignNameForClient(storyMenu, storyModeOnline);


                List<HudPart> partsToRemove = new List<HudPart>();

                foreach (HudPart part in p.hud.parts)
                {
                    RainMeadow.Debug(part);
                    if (part is KarmaMeter || part is FoodMeter)
                    {
                        partsToRemove.Add(part);
                    }
                }

                foreach (HudPart part in partsToRemove)
                {
                    part.slatedForDeletion = true;
                    part.ClearSprites();
                    p.hud.parts.Remove(part);
                }
            }


        }

        public static void GetRegionAndCampaignNameForClient(StoryMenuRedux storyMenu, StoryGameMode storyModeOnline)
        {
            if (!OnlineManager.lobby.isOwner && storyMenu.onlineDifficultyLabel != null)
            {

                storyMenu.onlineDifficultyLabel.text = storyModeOnline.region != "" ? GetCurrentCampaignName(storyModeOnline) + $" - {storyModeOnline.region}" : GetCurrentCampaignName(storyModeOnline) + storyMenu.Translate(" - New Game");

            }
        }

        public static void TryGetRegion(StoryMenuRedux storyMenu, StoryGameMode storyModeOnline, int index)
        {
            storyModeOnline.currentCampaign = storyMenu.slugcatPages[index].slugcatNumber;
            RainMeadow.Debug(storyModeOnline.currentCampaign);

            try
            {
                if ((storyMenu.saveGameData[storyModeOnline.currentCampaign].shelterName != null && storyMenu.saveGameData[storyModeOnline.currentCampaign].shelterName.Length > 2))

                {
                    storyModeOnline.region = Region.GetRegionFullName(storyMenu.saveGameData[storyModeOnline.currentCampaign].shelterName.Substring(0, 2), storyModeOnline.currentCampaign);
                }
            }
            catch
            {
                RainMeadow.Error("Error getting prev region name");
                storyModeOnline.region = "";
            }
        }
        #endregion
    }
}
