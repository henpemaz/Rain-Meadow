using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using System.Linq;
using RainMeadow.UI.Components.Patched;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using System.Globalization;
using HarmonyLib;
using System;

namespace RainMeadow.UI.Components
{
    public class OnlineTeamBattleSettingsInterface : RectangularMenuObject, SelectOneButton.SelectOneButtonOwner
    {
        public FSprite divider;
        public MenuTabWrapper tabWrapper;
        public TeamButton[] teamButtons = []; //uses buttonSelectedArray for dictionary parsing
        public OpTinyColorPicker[] teamColorPickers = []; //same count as teamButtons
        public OpTextBox[] teamNameBoxes = []; //same count as teamButtons
        public MenuLabel teamColorLerpLabel;
        public EventfulScrollButton? prevButton, nextButton;
        public OpTextBox teamLerpTextBox;
        public float dividerX = 50, dividerY = 160;
        private int currentOffset;

        public ArenaMode arenaMode;
        public TeamBattleMode teamBattleMode;

        public bool AllSettingsDisabled => arenaMode.initiateLobbyCountdown && arenaMode.arenaClientSettings.ready;
        public bool OwnerSettingsDisabled => !(OnlineManager.lobby?.isOwner == true) || AllSettingsDisabled;
        public bool IsTeamsAddedInCorrectly => !teamBattleMode.teamNames.All(x => teamBattleMode.teamColors.ContainsKey(x.Key) && teamBattleMode.teamIcons.ContainsKey(x.Key)) ||
            !(teamBattleMode.teamNames.Count == teamBattleMode.teamIcons.Count && teamBattleMode.teamNames.Count == teamBattleMode.teamColors.Count);
        public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, (teamBattleMode.teamNames.Count > 0) ? ((teamBattleMode.teamNames.Count - 1) / 4) : 0); }
        public int MaxOffset => Mathf.Max(teamBattleMode.teamNames.Count - 1, 0) / 4;
        public bool IsPagesOn => teamBattleMode.teamNames.Count > 4;
        public OnlineTeamBattleSettingsInterface(ArenaMode arena, TeamBattleMode team, Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            arenaMode = arena;
            teamBattleMode = team;
            divider = new("pixel")
            {
                anchorX = 0,
                scaleY = 2,
            };
            Container.AddChild(divider);
            tabWrapper = new(menu, this);
            PopulatePage(CurrentOffset);
            teamLerpTextBox = new(new Configurable<float>(teamBattleMode.lerp), new(size.x * 0.5f - 30, 20), 60)
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("How much do you want team color to show on players? From 0 to 1")
            };
            teamLerpTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                teamBattleMode.lerp = Mathf.Clamp01(teamLerpTextBox.valueFloat);
            };
            new PatchedUIelementWrapper(tabWrapper, teamLerpTextBox);
            teamColorLerpLabel = new(menu, this, menu.Translate("Team Lerp Color:"), new(teamLerpTextBox.pos.x, teamLerpTextBox.pos.y + teamLerpTextBox.size.y + 10), new(teamLerpTextBox.size.x, 0), false);
            this.SafeAddSubobjects(tabWrapper, teamColorLerpLabel);
        }
        public void PopulatePage(int offset)
        {
            ClearInterface();
            CurrentOffset = offset;
            int num = CurrentOffset * 4;
            float posXMultipler = size.x / 4;
            if (IsTeamsAddedInCorrectly) throw new NotImplementedException("teamNames, teamMappings and teamColors do not have all their keys");
            KeyValuePair<int, string>[] actualMappings = [.. teamBattleMode.teamIcons];
            teamButtons = new TeamButton[Mathf.Min(teamBattleMode.teamIcons.Count - num, 4)];
            teamNameBoxes = new OpTextBox[teamButtons.Length];
            teamColorPickers = new OpTinyColorPicker[teamButtons.Length];
            for (int i = 0; i < teamButtons.Length; i++)
            {

                KeyValuePair<int, string> mapping = actualMappings[num + i];
                string name = teamBattleMode.teamNames[mapping.Key];
                Color teamColor = teamBattleMode.teamColors[mapping.Key];

                Vector2 btnpos = i == 0 ? new(posXMultipler - 50, size.y - 125) : i % 2 == 0 ? new(teamButtons[0].pos.x, teamButtons[0].pos.y - 140) : new(posXMultipler * 3 - 50, teamButtons[i - 1].pos.y);
                teamButtons[i] = new(menu, this, btnpos, new(100, 100), teamButtons, mapping.Key, name, mapping.Value);

                Vector2 namePickerPos = i == 0 ? new(posXMultipler - 75, dividerY - 40) : i % 2 == 0 ? new(teamNameBoxes[0].pos.x, teamNameBoxes[0].pos.y - 40) : new(posXMultipler * 3 - 95, teamNameBoxes[i - 1].pos.y);
                OpTextBox textBox = teamNameBoxes[i] = new(new Configurable<string>(name), namePickerPos, 150)
                {
                    allowSpace = true,
                };
                textBox.OnValueUpdate += (config, value, oldValue) => NameTextBox_OnValueUpdated(mapping.Key, value);
                new PatchedUIelementWrapper(tabWrapper, textBox);

                OpTinyColorPicker tinyPicker = teamColorPickers[i] = new(menu, new(namePickerPos.x + 10 + textBox.size.x, namePickerPos.y), teamColor, tabWrapper);
                tinyPicker.colorPicker.OnValueUpdate += (config, value, oldValue) =>
                {
                    if (ColorUtility.TryParseHtmlString($"#{tinyPicker.colorPicker.value}", out Color color))
                        ColorPicker_OnValueChangedEvent(mapping.Key, color.SafeColorRange());
                };
            }
            this.SafeAddSubobjects(teamButtons);
            if (IsPagesOn) CreatePageButtons();
            else DeletePageButtons();
            tabWrapper._tab.myContainer.MoveToFront();
        }
        public void ClearInterface()
        {
            teamButtons.Do(this.ClearMenuObject);
            UnloadAnyConfig(teamColorPickers);
            UnloadAnyConfig(teamNameBoxes);

        }
        public void UnloadAnyConfig(params UIelement[]? elements)
        {
            if (elements == null) return;
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
        public void ColorPicker_OnValueChangedEvent(int value, Color newColor)
        {
            if (!teamBattleMode.teamColors.ContainsKey(value))
            {
                RainMeadow.Error($"Key,{value} is not stored in team colors");
                return;
            }
            teamBattleMode.teamColors[value] = Extensions.SafeColorRange(newColor);
        }
        public void NameTextBox_OnValueUpdated(int value, string newName)
        {
            if (!teamBattleMode.teamNames.ContainsKey(value))
            {
                RainMeadow.Error($"Key,{value} is not stored in team names");
                return;
            }
            teamBattleMode.teamNames[value] = newName;
        }
        public void OnShutdown()
        {
            if (!(OnlineManager.lobby?.isOwner == true)) return;

            RainMeadow.rainMeadowOptions.TeamColorLerp.Value = teamBattleMode.lerp;
            if (teamBattleMode.teamNames.ContainsKey(0))
                RainMeadow.rainMeadowOptions.MartyrTeamName.Value = teamBattleMode.teamNames[0];
            if (teamBattleMode.teamNames.ContainsKey(1))
                RainMeadow.rainMeadowOptions.OutlawsTeamName.Value = teamBattleMode.teamNames[1];
            if (teamBattleMode.teamNames.ContainsKey(2))
                RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value = teamBattleMode.teamNames[2];
            if (teamBattleMode.teamNames.ContainsKey(3))
                RainMeadow.rainMeadowOptions.ChieftainTeamName.Value = teamBattleMode.teamNames[3];

            if (teamBattleMode.teamColors.ContainsKey(0))
                RainMeadow.rainMeadowOptions.MartyrTeamColor.Value = teamBattleMode.teamColors[0];
            if (teamBattleMode.teamColors.ContainsKey(1))
                RainMeadow.rainMeadowOptions.OutlawsTeamColor.Value = teamBattleMode.teamColors[1];
            if (teamBattleMode.teamColors.ContainsKey(2))
                RainMeadow.rainMeadowOptions.DragonSlayersTeamColor.Value = teamBattleMode.teamColors[2];
            if (teamBattleMode.teamColors.ContainsKey(3))
                RainMeadow.rainMeadowOptions.ChieftainTeamColor.Value = teamBattleMode.teamColors[3];
        }
        public void PrevPage()
        {
            if (CurrentOffset > 0)
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePage(CurrentOffset - 1);
            }
        }
        public void NextPage()
        {
            if (CurrentOffset < MaxOffset)
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePage(CurrentOffset + 1);
            }
        }
        public void CreatePageButtons()
        {
            if (prevButton == null)
            {
                prevButton = new(menu, this, new(40, 20), 4, 24);
                prevButton.OnClick += _ => PrevPage();
                this.SafeAddSubobjects(prevButton);
            }
            if (nextButton == null)
            {
                nextButton = new(menu, this, new(size.x - 40, prevButton.pos.y), 1, 24);
                nextButton.OnClick += _ => NextPage();
                this.SafeAddSubobjects(nextButton);
            }
        }
        public void DeletePageButtons()
        {
            this.ClearMenuObject(ref prevButton);
            this.ClearMenuObject(ref nextButton);
        }
        public override void RemoveSprites()
        {
            divider.RemoveFromContainer();
            base.RemoveSprites();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 drawPos = DrawPos(timeStacker), drawSize = DrawSize(timeStacker);
            divider.x = drawPos.x + dividerX;
            divider.y = drawPos.y + dividerY;
            divider.scaleX = drawSize.x - dividerX * 2;
            divider.color = MenuColorEffect.rgbDarkGrey;
            teamColorLerpLabel.label.color = teamLerpTextBox.rect.colorEdge;
        }
        public override void Update()
        {
            base.Update();

            if (!teamLerpTextBox.held)
                teamLerpTextBox.valueFloat = teamBattleMode.lerp;
            if (prevButton != null)
                prevButton.buttonBehav.greyedOut = !(CurrentOffset > 0);
            if (nextButton != null)
                nextButton.buttonBehav.greyedOut = !(CurrentOffset < MaxOffset);
            teamLerpTextBox.greyedOut = OwnerSettingsDisabled;
            for (int i = 0; i < teamButtons.Length; i++)
            {
                int actualTeamIndex = teamButtons[i].buttonArrayIndex;
                string name = teamBattleMode.teamNames[actualTeamIndex];
                Color color = teamBattleMode.teamColors[actualTeamIndex];
                teamButtons[i].teamColor = color;
                teamButtons[i].teamName = teamBattleMode.teamNames[actualTeamIndex];
                teamButtons[i].buttonBehav.greyedOut = teamColorPickers.Any(x => x.currentlyPicking);
                teamButtons[i].teamCount.text = OnlineManager.lobby.clientSettings.Where(x => OnlineManager.players.Contains(x.Key) && x.Value.TryGetData<ArenaTeamClientSettings>(out var team) && team.team == actualTeamIndex).Count().ToString();
                teamButtons[i].teamCount.label.color = color;

                if (!teamNameBoxes[i].held) teamNameBoxes[i].value = name;
                if (!teamColorPickers[i].held) teamColorPickers[i].valuecolor = color;

                bool greyOutConfig = OwnerSettingsDisabled || teamColorPickers.Any(x => x.currentlyPicking && x != teamColorPickers[i]);

                teamNameBoxes[i].greyedOut = greyOutConfig;
                teamColorPickers[i].greyedOut = greyOutConfig;
              ;
            }
        }
        public void SetCurrentlySelectedOfSeries(string id, int index) => arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team = index;
        public int GetCurrentlySelectedOfSeries(string id) => arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team;
        public class TeamButton : EventfulSelectOneButton
        {
            public string teamName;
            public float widthOfText = 190;
            public FSprite symbol;
            public MenuLabel teamLabel;
            public ProperlyAlignedMenuLabel teamCount;
            public Color teamColor = Color.white;
            public TeamButton(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, EventfulSelectOneButton[] selectButtons, int index, string teamName, string symbolName, float symbolSpriteScale = 2.8f) : base(menu, owner, "", "TEAM", pos, size, selectButtons, index)
            {
                this.teamName = teamName;
                symbol = new(symbolName)
                {
                    scale = symbolSpriteScale
                };
                Container.AddChild(symbol);
                teamLabel = new(menu, this, this.teamName, new(0, -25), new(size.x, 20), true);
                teamCount = new(menu, this, "", new Vector2(symbol.x + 10, symbol.y + 10), new(10, 10), false);
                this.SafeAddSubobjects(teamLabel, teamCount);
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                Vector2 drawPos = DrawPos(timeStacker), drawSize = DrawSize(timeStacker);
                symbol.x = drawPos.x + drawSize.x * 0.5f;
                symbol.y = drawPos.y + drawSize.y * 0.5f;
                symbol.color = teamColor;
                teamLabel.text = LabelTest.TrimText(teamName, widthOfText, true, true);
                teamLabel.label.color = teamColor;
            }
            public override void RemoveSprites()
            {
                symbol.RemoveFromContainer();
                base.RemoveSprites();
            }
        }
    }
}
