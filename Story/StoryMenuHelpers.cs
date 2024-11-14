using Menu;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ExtraExtentions;
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
        }

        #endregion


        #region StoryObjects
        internal static void RemoveExcessStoryObjects(StoryMenuRedux storyMenu)
        {
            if (storyMenu.startButton != null)
            {
                storyMenu.startButton.RemoveSprites();
                storyMenu.pages[0].RemoveSubObject(storyMenu.startButton);
            }

            if (storyMenu.restartCheckbox != null)
            {
                storyMenu.restartCheckbox.RemoveSprites();
                storyMenu.pages[0].RemoveSubObject(storyMenu.restartCheckbox);
            }
        }

        internal static void SetupOnlineMenuItems(StoryMenuRedux storyMenu)
        {
            // Music
            storyMenu.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;

            // Player lobby label
            var lobbyLabel = new MenuLabel(storyMenu, storyMenu.pages[0], storyMenu.Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true);
            storyMenu.pages[0].subObjects.Add(lobbyLabel);
            //var inviteFriendsIcon = new SimplerSymbolButton(storyMenu, storyMenu.pages[0], "Kill_Slugcat", "InviteFriends", new Vector2(storyMenu.playerButtons[0].pos.x - 35f, storyMenu.playerButtons[0].pos.y + 5f));
            //inviteFriendsIcon.OnClick += (_) =>
            //{

            //    SteamFriends.ActivateGameOverlay("friends");

            //};
            // storyMenu.pages[0].subObjects.Add(inviteFriendsIcon);

            if (RainMeadow.rainMeadowOptions.SlugcatCustomToggle.Value && !OnlineManager.lobby.isOwner)
            {
                storyMenu.pages[0].subObjects.Add(new MenuLabel(storyMenu, storyMenu.pages[0], storyMenu.Translate("Slugcats"), new Vector2(394, 553), new(110, 30), true));
            }
        }

        internal static void ModifyExistingMenuItems(StoryMenuRedux storyMenu)
        {
            foreach (var obj in storyMenu.pages[0].subObjects) // unfortunate locally declared variable.
            {
                if (obj is SimpleButton && (obj as SimpleButton).signalText == "BACK") {

                    (obj as SimpleButton).pos = new Vector2(50f, 50f);

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

        public static string GetCurrentCampaignName(StoryGameMode story)
        {
            return SlugcatStats.getSlugcatName(story.currentCampaign);
        }

        internal static void CustomSlugcatSetup(SlugcatSelectMenu ssm, SlugcatStats.Name customSelectedSlugcat)
        {

            var slugList = AllSlugcats();
            var slugButtons = new EventfulSelectOneButton[slugList.Count];


            for (int i = 0; i < slugButtons.Length; i++)
            {
                var slug = slugList[i];
                var btn = new SimplerButton(ssm, ssm.pages[0], SlugcatStats.getSlugcatName(slug), new Vector2(394, 515) - i * new Vector2(0, 38), new Vector2(110, 30));
                btn.toggled = false;
                ssm.pages[0].subObjects.Add(btn);

                // Store the current button in a variable accessible by the lambda
                var currentBtn = btn;
                btn.OnClick += (_) =>
                {
                    // Set the clicked button to true
                    currentBtn.toggled = !currentBtn.toggled;
                    customSelectedSlugcat = slug;

                    // Set all other buttons to false
                    foreach (var otherBtn in ssm.pages[0].subObjects.OfType<SimplerButton>())
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
        internal static void SetupHostMenu(StoryMenuRedux storyMenu, StoryGameMode story)
        {
            storyMenu.hostStartButton = new EventfulHoldButton(storyMenu, storyMenu.pages[0], storyMenu.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            storyMenu.hostStartButton.OnClick += (_) => { storyMenu.StartGame(); };
            storyMenu.hostStartButton.buttonBehav.greyedOut = false;
            storyMenu.pages[0].subObjects.Add(storyMenu.hostStartButton);

           
            storyMenu.resetSaveCheckbox = new CheckBox(storyMenu, storyMenu.pages[0], storyMenu, new Vector2(storyMenu.colorsCheckbox.pos.x + 150f, storyMenu.colorsCheckbox.pos.y), 70f, storyMenu.Translate("Reset Save"), "RESETSAVE", false);
            storyMenu.resetSaveCheckbox.Singal(storyMenu.pages[0], "RESETSAVE");
            storyMenu.pages[0].subObjects.Add(storyMenu.resetSaveCheckbox);



        }

        internal static void SetupClientMenu(StoryMenuRedux storyMenu, StoryGameMode story)
        {
            storyMenu.campaignContainer = new MenuLabel(storyMenu, storyMenu.pages[0], storyMenu.Translate(story.currentCampaign.value), new Vector2(583f, storyMenu.startButton.pos.y - 50f), new Vector2(200f, 30f), true);

            storyMenu.pages[0].subObjects.Add(storyMenu.campaignContainer);

            storyMenu.clientWaitingButton = new EventfulHoldButton(storyMenu, storyMenu.pages[0], storyMenu.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            storyMenu.clientWaitingButton.OnClick += (_) => { storyMenu.StartGame(); };
            storyMenu.clientWaitingButton.buttonBehav.greyedOut = !(story.isInGame && !story.changedRegions);

            storyMenu.pages[0].subObjects.Add(storyMenu.clientWaitingButton);

           
            storyMenu.clientWantsToOverwriteSave = new CheckBox(storyMenu, storyMenu.pages[0], storyMenu, new Vector2(storyMenu.colorsCheckbox.pos.x + 150f, storyMenu.colorsCheckbox.pos.y), 70f, storyMenu.Translate("Copy host progress"), "OVERWRITECLIENTSAVE", false);
            storyMenu.clientWantsToOverwriteSave.Singal(storyMenu.pages[0], "OVERWRITECLIENTSAVE");
            storyMenu.pages[0].subObjects.Add(storyMenu.clientWantsToOverwriteSave);
        }
        #endregion

        internal static Color HslToRgb(float hue, float saturation, float lightness)
        {
            // Convert HSL to RGB
            float c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            float x = c * (1 - Math.Abs(((hue / 60) % 2) - 1));
            float m = lightness - c / 2;

            float rPrime = 0, gPrime = 0, bPrime = 0;

            if (hue >= 0 && hue < 60)
            {
                rPrime = c;
                gPrime = x;
                bPrime = 0;
            }
            else if (hue >= 60 && hue < 120)
            {
                rPrime = x;
                gPrime = c;
                bPrime = 0;
            }
            else if (hue >= 120 && hue < 180)
            {
                rPrime = 0;
                gPrime = c;
                bPrime = x;
            }
            else if (hue >= 180 && hue < 240)
            {
                rPrime = 0;
                gPrime = x;
                bPrime = c;
            }
            else if (hue >= 240 && hue < 300)
            {
                rPrime = x;
                gPrime = 0;
                bPrime = c;
            }
            else
            {
                rPrime = c;
                gPrime = 0;
                bPrime = x;
            }

            // Add m to adjust RGB values
            int r = (int)((rPrime + m) * 255);
            int g = (int)((gPrime + m) * 255);
            int b = (int)((bPrime + m) * 255);

            return new Color(r, g, b); // Return the RGB color
        }
    }
}
