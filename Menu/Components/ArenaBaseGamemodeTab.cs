using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using RainMeadow.UI.Components.Patched;
using System.Linq;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
using Menu.Remix.MixedUI.ValueTypes;
using System;
using System.Text;
namespace RainMeadow.UI.Components
{
    public class OnlineArenaBaseGameModeTab
        : RectangularMenuObject
    {
        public MenuTabWrapper tabWrapper;

        public MenuLabel foodScoreLabel;
        public OpTextBox foodScoreTextBox;

        public MenuLabel spearHitScoreLabel;
        public OpTextBox spearHitScoreTextBox;
        public MenuLabel killScoreLabel;
        public OpTextBox killScoreTextBox;
        public MenuLabel aliveScoreLabel;
        public OpTextBox aliveScoreTextBox;
        public MenuLabel denEntryRuleLabel;
        public OpTextBox denScoreTextBox;
        public MenuLabel denScoreLabel;
        public OpComboBox2 denEntryRule;

        public MenuLabel emptyKillTagScoreLabel;
        public OpTextBox emptyKillTagScore;

        public MenuLabel challengeDenEjectionLabel;
        public OpCheckBox challengeDenEjectionCheckbox;
        public EventfulScrollButton? prevButton,


            nextButton;
        public ArenaMode arena => OnlineManager.lobby.gameMode as ArenaOnlineGameMode;
        public MenuLabel arenaImportExportLabel;
        public OpSimpleButton arenaPlaylistImportButton;
        public OpSimpleButton arenaPlaylistExportButton;



        public bool AllSettingsDisabled =>
            arena.initiateLobbyCountdown && arena.arenaClientSettings.ready;
        public bool OwnerSettingsDisabled =>
            !(OnlineManager.lobby?.isOwner == true) || AllSettingsDisabled;


        public OnlineArenaBaseGameModeTab(
            Menu.Menu menu,
            MenuObject owner,
            Vector2 pos,
            Vector2 size
        )
            : base(menu, owner, pos, size)
        {
            tabWrapper = new(menu, this);
            float leftMargin = 10f;
            float labelWidth = 100f;
            float topOffset = size.y - 60f;
            float rowHeight = 40f;
            float boxMargin = leftMargin + labelWidth + 50f; // The X-position for all boxes


            foodScoreLabel = new(menu, this, menu.Translate("Food Score:"),
                new(leftMargin, topOffset), new(labelWidth, 20f), false);
            foodScoreLabel.label.alignment = FLabelAlignment.Left;

            foodScoreTextBox = new(RainMeadow.rainMeadowOptions.ArenaFoodScore,
                new(boxMargin, topOffset - 2f), 60)
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("Food points multiplier"),
                accept = OpTextBox.Accept.Int
            };

            foodScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (foodScoreTextBox.valueInt < 0) foodScoreTextBox.valueInt = 0;
                arena.foodScore = foodScoreTextBox.valueInt;
            };


            spearHitScoreLabel = new(menu, this, menu.Translate("Spear Hit Score:"),
                new(leftMargin, topOffset - rowHeight), new(labelWidth, 20f), false);
            spearHitScoreLabel.label.alignment = FLabelAlignment.Left;

            spearHitScoreTextBox = new(RainMeadow.rainMeadowOptions.ArenaSpearHitScore,
                new(boxMargin, topOffset - rowHeight - 2f), 60)
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("Points a spear is worth (non-lethal)"),
                accept = OpTextBox.Accept.Int
            };

            spearHitScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (spearHitScoreTextBox.valueInt < 0) spearHitScoreTextBox.valueInt = 0;
                arena.spearHitScore = spearHitScoreTextBox.valueInt;
            };

            killScoreLabel = new(menu, this, menu.Translate("Kill Score:"),
                new(leftMargin, topOffset - (rowHeight * 2)), new(labelWidth, 20f), false);
            killScoreLabel.label.alignment = FLabelAlignment.Left;

            killScoreTextBox = new(RainMeadow.rainMeadowOptions.ArenaKillScore,
                new(boxMargin, topOffset - (rowHeight * 2) - 2f), 60)
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("Points a kill is worth"),
                accept = OpTextBox.Accept.Int
            };

            killScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (killScoreTextBox.valueInt < 0) killScoreTextBox.valueInt = 0;
                arena.killScore = killScoreTextBox.valueInt;
            };

            aliveScoreLabel = new(menu, this, menu.Translate("Survival Score:"),
                new(leftMargin, topOffset - (rowHeight * 3)), new(labelWidth, 20f), false);
            aliveScoreLabel.label.alignment = FLabelAlignment.Left;

            aliveScoreTextBox = new(RainMeadow.rainMeadowOptions.ArenaAliveScore,
               new(boxMargin, topOffset - (rowHeight * 3) - 2f), 60)
            { alignment = FLabelAlignment.Center, description = menu.Translate("Points for surviving inside the shelter"), accept = OpTextBox.Accept.Int };

            aliveScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (aliveScoreTextBox.valueInt < 0) aliveScoreTextBox.valueInt = 0;
                arena.aliveScore = aliveScoreTextBox.valueInt;
            };

            emptyKillTagScoreLabel = new(menu, this, menu.Translate("Empty Kill Score:"),
                new(leftMargin, topOffset - rowHeight * 4), new(labelWidth, 20f), false);
            emptyKillTagScoreLabel.label.alignment = FLabelAlignment.Left;

            emptyKillTagScore = new(RainMeadow.rainMeadowOptions.ArenaEmptyKillTagScore,
            new(boxMargin, topOffset - (rowHeight * 4)), 60)
            { alignment = FLabelAlignment.Center, description = menu.Translate("Points for other players if someone dies without a killer"), accept = OpTextBox.Accept.Int };

            emptyKillTagScore.OnValueUpdate += (config, value, oldValue) =>
            {
                if (emptyKillTagScore.valueInt < 0) emptyKillTagScore.valueInt = 0;
                arena.emptyKillTagScore = emptyKillTagScore.valueInt;
            };

            denScoreLabel = new(menu, this, menu.Translate("Unlock Dens:"),
                    new(leftMargin, topOffset - (rowHeight * 5)), new(labelWidth, 20f), false);
            denScoreLabel.label.alignment = FLabelAlignment.Left;

            denScoreTextBox = new(RainMeadow.rainMeadowOptions.ArenaDenScore,
                new(boxMargin, topOffset - (rowHeight * 5) - 2f), 60) // FIXED: Added '- (rowHeight * 2)'
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("Points required to unlock dens"),
                accept = OpTextBox.Accept.Int
            };

            denScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
                    {
                        if (denScoreTextBox.valueInt < 0) denScoreTextBox.valueInt = 0;
                        arena.denScore = denScoreTextBox.valueInt;
                    };

            denEntryRuleLabel = new(menu, this, menu.Translate("Den Entry:"),
                new(leftMargin, topOffset - (rowHeight * 6)), new(labelWidth, 20f), false);
            denEntryRuleLabel.label.alignment = FLabelAlignment.Left;

            var denRuleItems = OpResourceSelector.GetEnumNames(null, typeof(ArenaSetup.GameTypeSetup.DenEntryRule))
                .Select(li =>
                {
                    li.displayName = menu.Translate(li.displayName);
                    return li;
                }).ToList();

            denEntryRule = new OpComboBox2(
                RainMeadow.rainMeadowOptions.ArenaDenType,
                new(boxMargin, topOffset - (rowHeight * 6) - 2f),
                110,
                denRuleItems
            )
            {
                description = menu.Translate("Den entry behavior"),
            };

            denEntryRule.OnValueChanged += (UIconfig, value, oldValue) =>
            {
                arena.denEntryRule = new ArenaSetup.GameTypeSetup.DenEntryRule(value); ;
            };
            denEntryRule.Change();




            challengeDenEjectionLabel = new(menu, this, menu.Translate("Den Ejection:"),
                new(leftMargin, topOffset - rowHeight * 7), new(labelWidth, 20f), false);
            challengeDenEjectionLabel.label.alignment = FLabelAlignment.Left;

            challengeDenEjectionCheckbox = new(RainMeadow.rainMeadowOptions.ChallengeDenEjection, boxMargin, topOffset - (rowHeight * 7));

            challengeDenEjectionCheckbox.OnValueUpdate += (config, value, oldValue) =>
            {
            };
            challengeDenEjectionCheckbox.OnChange += () =>
            {
                challengeDenEjectionCheckbox.description = challengeDenEjectionCheckbox.GetValueBool() ? menu.Translate("Dens eject and block players after some time") : menu.Translate("Normal den behavior");
                arena.challengeDenEjection = challengeDenEjectionCheckbox.GetValueBool();
            };
            challengeDenEjectionCheckbox.Change();


            arenaImportExportLabel = new(menu, this, menu.Translate("Playlist:"),
                new(leftMargin, topOffset - rowHeight * 8), new(labelWidth, 20f), false);
            arenaImportExportLabel.label.alignment = FLabelAlignment.Left;

            arenaPlaylistExportButton = new(new Vector2(boxMargin, topOffset - (rowHeight * 8) - 2f), new Vector2(180f, 30f), this.menu.Translate("Copy playlist to clipboard"));
            arenaPlaylistExportButton.OnClick += (_) =>
            {
                try
                {

                    var arenaMenu = menu as ArenaOnlineLobbyMenu;
                    string result = EncodePlaylist(arenaMenu?.arenaMainLobbyPage.levelSelector.SelectedPlayList);
                    // Copy the code to the user's clipboard
                    GUIUtility.systemCopyBuffer = result;
                    arenaImportExportLabel.text = menu.Translate("Copied");
                    arenaImportExportLabel.label.color = Color.green;

                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                    arenaImportExportLabel.text = menu.Translate("Failed");
                    arenaImportExportLabel.label.color = Color.red;

                }
            };

            arenaPlaylistImportButton = new(new Vector2(boxMargin, topOffset - (rowHeight * 9) - 2f), new Vector2(180f, 30f), this.menu.Translate("Import playlist from clipboard"));
            arenaPlaylistImportButton.OnClick += (_) =>
            {
                try
                {
                    var arenaMenu = menu as ArenaOnlineLobbyMenu;
                    string clipboardText = UnityEngine.GUIUtility.systemCopyBuffer;

                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        arenaMenu?.arenaMainLobbyPage.levelSelector.SelectedPlayList.Clear();
                        List<string> playlist = DecodePlaylist(clipboardText);
                        if (playlist.Count == 0)
                        {
                            arenaImportExportLabel.text = menu.Translate("Failed");
                            arenaImportExportLabel.label.color = Color.red;
                            return;
                        }

                        for (int i = 0; i < playlist.Count; i++)
                        {
                            arenaMenu?.arenaMainLobbyPage.levelSelector.AddItemToSelectedList(playlist[i]);
                        }
                        arenaImportExportLabel.text = menu.Translate("Imported");
                        arenaImportExportLabel.label.color = Color.green;

                    }
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                    arenaImportExportLabel.text = menu.Translate("Failed import");
                    arenaImportExportLabel.label.color = Color.red;

                }
            };




            this.SafeAddSubobjects(
                tabWrapper,
                foodScoreLabel,
                spearHitScoreLabel,
                killScoreLabel,
                aliveScoreLabel,
                denEntryRuleLabel,
                denScoreLabel,
                emptyKillTagScoreLabel,
                challengeDenEjectionLabel,
                arenaImportExportLabel
            );
            new PatchedUIelementWrapper(tabWrapper, foodScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, spearHitScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, killScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, denEntryRule);
            new PatchedUIelementWrapper(tabWrapper, aliveScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, denScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, emptyKillTagScore);
            new PatchedUIelementWrapper(tabWrapper, challengeDenEjectionCheckbox);
            new PatchedUIelementWrapper(tabWrapper, arenaPlaylistExportButton);
            new PatchedUIelementWrapper(tabWrapper, arenaPlaylistImportButton);


        }
        public void PopulatePage(int offset)
        {
            ClearInterface();

            float posXMultipler = size.x / 4;
            tabWrapper._tab.myContainer.MoveToFront();
        }

        public void ClearInterface() { }

        public void UnloadAnyConfig(params UIelement[]? elements)
        {
            if (elements == null)
                return;
            foreach (UIelement element in elements)
            {
                if (tabWrapper.wrappers.ContainsKey(element))
                {
                    tabWrapper.ClearMenuObject(tabWrapper.wrappers[element]);
                    tabWrapper.wrappers.Remove(element);
                }
                element.Unload();
            }
        }

        public void OnShutdown()
        {
            if (!(OnlineManager.lobby?.isOwner == true))
                return;
            RainMeadow.rainMeadowOptions.ArenaFoodScore.Value = arena.foodScore;
            RainMeadow.rainMeadowOptions.ArenaSpearHitScore.Value = arena.spearHitScore;
            RainMeadow.rainMeadowOptions.ArenaKillScore.Value = arena.killScore;
            RainMeadow.rainMeadowOptions.ArenaAliveScore.Value = arena.aliveScore;
            RainMeadow.rainMeadowOptions.ArenaDenType.Value = arena.denEntryRule;
            RainMeadow.rainMeadowOptions.ArenaDenScore.Value = arena.denScore;
            RainMeadow.rainMeadowOptions.ArenaEmptyKillTagScore.Value = arena.emptyKillTagScore;
            RainMeadow.rainMeadowOptions.ChallengeDenEjection.Value = arena.challengeDenEjection;

            RainMeadow.rainMeadowOptions.config.Save();

        }

        public void DeletePageButtons()
        {
            this.ClearMenuObject(ref prevButton);
            this.ClearMenuObject(ref nextButton);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }

        public int timeToClearMessage = 120;
        public override void Update()
        {
            base.Update();
            if (foodScoreTextBox != null)
            {
                foodScoreTextBox.held = foodScoreTextBox._KeyboardOn;
                if (!foodScoreTextBox.held)
                {
                    foodScoreTextBox.valueInt = arena.foodScore;
                }

                foodScoreTextBox.greyedOut = OwnerSettingsDisabled;
            }
            if (spearHitScoreTextBox != null)
            {
                spearHitScoreTextBox.held = spearHitScoreTextBox._KeyboardOn;
                if (!spearHitScoreTextBox.held)
                {
                    if (!ModManager.MSC && arena.spearHitScore > 0) // reset
                    {
                        arena.spearHitScore = 0;
                    }
                    spearHitScoreTextBox.valueInt = arena.spearHitScore;
                }

                spearHitScoreTextBox.greyedOut = OwnerSettingsDisabled || !ModManager.MSC;
            }
            if (killScoreTextBox != null)
            {
                killScoreTextBox.held = killScoreTextBox._KeyboardOn;
                if (!killScoreTextBox.held)
                {
                    killScoreTextBox.valueInt = arena.killScore;
                }

                killScoreTextBox.greyedOut = OwnerSettingsDisabled;
            }
            if (aliveScoreTextBox != null)
            {
                aliveScoreTextBox.greyedOut = OwnerSettingsDisabled;
                aliveScoreTextBox.held = aliveScoreTextBox._KeyboardOn;
                if (!aliveScoreTextBox.held)
                {
                    aliveScoreTextBox.valueInt = arena.aliveScore;

                }

            }
            if (denEntryRule != null)
            {
                denEntryRule.greyedOut = OwnerSettingsDisabled; ;
                denEntryRule.value = arena.denEntryRule.value;
            }

            if (denScoreTextBox != null)
            {
                denScoreTextBox.held = denScoreTextBox._KeyboardOn;
                denScoreTextBox.greyedOut = OwnerSettingsDisabled;
                if (!denScoreTextBox.held)
                {
                    denScoreTextBox.valueInt = arena.denScore;

                }
            }

            if (emptyKillTagScore != null)
            {
                emptyKillTagScore.greyedOut = OwnerSettingsDisabled;
                emptyKillTagScore.held = emptyKillTagScore._KeyboardOn;
                if (!emptyKillTagScore.held)
                {
                    emptyKillTagScore.valueInt = arena.emptyKillTagScore;

                }

            }

            if (challengeDenEjectionCheckbox != null)
            {
                challengeDenEjectionCheckbox.greyedOut = OwnerSettingsDisabled;
                if (!challengeDenEjectionCheckbox.held)
                {
                    challengeDenEjectionCheckbox.SetValueBool(arena.challengeDenEjection);
                }
            }

            if (arenaImportExportLabel.text != "Playlist:")
            {
                timeToClearMessage--;
                if (timeToClearMessage <= 0)
                {
                    arenaImportExportLabel.text = "Playlist:";
                    arenaImportExportLabel.label.color = Color.white;

                    timeToClearMessage = 120;
                }
            }
            if (arenaPlaylistImportButton != null)
            {
                arenaPlaylistImportButton.greyedOut = OwnerSettingsDisabled;
            }
        }

        /// <summary>
        /// Encodes a List<string>  into a base64 encoding of Arena map names.
        /// </summary>
        public static string EncodePlaylist(List<string> arenaMaps)
        {
            if (arenaMaps == null || arenaMaps.Count == 0)
            {
                return string.Empty;
            }

            // Join the list into a single string delimited by semicolons
            string joinedMaps = string.Join(";", arenaMaps);

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(joinedMaps);


            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Decodes a Base64 string back into a List of Arena map names.
        /// </summary>
        public static List<string> DecodePlaylist(string base64EncodedData)
        {
            if (string.IsNullOrEmpty(base64EncodedData))
            {
                return new List<string>();
            }

            try
            {
                byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
                string decodedString = Encoding.UTF8.GetString(base64EncodedBytes);
                return decodedString.Split(';').ToList();
            }
            catch (FormatException)
            {
                Debug.LogError("Failed to load playlist: The provided string is not a valid Base64 format.");
                return new List<string>();
            }
        }
    }
}