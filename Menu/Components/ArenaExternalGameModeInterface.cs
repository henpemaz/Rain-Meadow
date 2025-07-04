using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using System.Linq;
using RainMeadow.UI.Components.Patched;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;

namespace RainMeadow.UI.Components
{
    public class OnlineTeamBattleSettingsInterface : PositionedMenuObject
    {
        public MenuTabWrapper tabWrapper;
        public PatchedUIelementWrapper gameTeamWrapper, martyrWrapper, outlawWrapper, dragonSlayerWrapper, chieftanWrapper, teamLerpWrapper;
        public OpComboBox arenaTeamComboBox;
        public ProperlyAlignedMenuLabel arenaGameModeLabel, martyrTeamLabel, outlawsTeamLabel, dragonsSlayersTeamLabel, chieftainsTeamLabel, teamColorLerpLabel;
        public OpTextBox martyrsTeamNameUpdate, outlawsTeamNameUpdate, dragonSlayersTeamNameUpdate, chieftainsTeamNameUpdate, teamLerpColorBox;
        public OpTinyColorPicker martyrColor, chieftainColor, dragonSlayerColor, outlawColor;
        public bool teamComboBoxLastHeld;

        public ArenaMode arenaMode;
        public TeamBattleMode teamBattleMode;
        public ArenaSetup GetArenaSetup => menu.manager.arenaSetup;
        public ArenaSetup.GameTypeSetup GetGameTypeSetup => GetArenaSetup.GetOrInitiateGameTypeSetup(GetArenaSetup.currentGameType);
        public OnlineTeamBattleSettingsInterface(ArenaMode arena, TeamBattleMode team, Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            tabWrapper = new(menu, this);
            arenaMode = arena;
            teamBattleMode = team;

            List<ListItem> teamNameListItems = [new(RainMeadow.rainMeadowOptions.MartyrTeamName.Value, 0), new(RainMeadow.rainMeadowOptions.OutlawsTeamName.Value, 1), 
                new(RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value, 2), new(RainMeadow.rainMeadowOptions.ChieftainTeamName.Value, 3)];

            arenaGameModeLabel = new(menu, this, menu.Translate("Team:"), new Vector2(50, 380f), new Vector2(0, 20), false);
            bool defaultTeamNameString = arenaMode.clientSettings.TryGetData<ArenaTeamClientSettings>(out var t);

            arenaTeamComboBox = new OpComboBox2(new Configurable<string>(defaultTeamNameString ? teamBattleMode.teamNameDictionary[t.team] : ""), new Vector2(arenaGameModeLabel.pos.x + 50, arenaGameModeLabel.pos.y), 175f, teamNameListItems);
            arenaTeamComboBox.OnValueChanged += (config, value, lastValue) =>
            {
                if (arenaMode.clientSettings.TryGetData<ArenaTeamClientSettings>(out var tb))
                {
                    var alListItems = arenaTeamComboBox.GetItemList();
                    for (int i = 0; i < alListItems.Length; i++)
                        if (alListItems[i].name == value)
                            tb.team = i;

                }
            };

            martyrTeamLabel = new(menu, this, menu.Translate("Team 1:"), new Vector2(arenaGameModeLabel.pos.x, arenaTeamComboBox.pos.y - 45), new Vector2(0, 20), false);
            martyrsTeamNameUpdate = new(new Configurable<string>(RainMeadow.rainMeadowOptions.MartyrTeamName.Value), new(martyrTeamLabel.pos.x + 50, martyrTeamLabel.pos.y), 150)
            {
                allowSpace = true
            };
            martyrsTeamNameUpdate.OnValueUpdate += (config, value, oldValue) => GetTeamNameUpdate(martyrsTeamNameUpdate, value, oldValue);

            outlawsTeamLabel = new(menu, this, menu.Translate("Team 2:"), new Vector2(arenaGameModeLabel.pos.x, martyrsTeamNameUpdate.pos.y - 45), new Vector2(0, 20), false);
            outlawsTeamNameUpdate = new(new Configurable<string>(RainMeadow.rainMeadowOptions.OutlawsTeamName.Value), new(outlawsTeamLabel.pos.x + 50, outlawsTeamLabel.pos.y), 150)
            {
                allowSpace = true
            };
            outlawsTeamNameUpdate.OnValueUpdate += (config, value, oldValue) => GetTeamNameUpdate(outlawsTeamNameUpdate, value, oldValue);

            dragonsSlayersTeamLabel = new(menu, this, menu.Translate("Team 3:"), new Vector2(arenaGameModeLabel.pos.x, outlawsTeamNameUpdate.pos.y - 45), new Vector2(0, 20), false);
            dragonSlayersTeamNameUpdate = new(new Configurable<string>(RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value), new(dragonsSlayersTeamLabel.pos.x + 50, dragonsSlayersTeamLabel.pos.y), 150)
            {
                allowSpace = true
            };
            dragonSlayersTeamNameUpdate.OnValueUpdate += (config, value, oldValue) => GetTeamNameUpdate(dragonSlayersTeamNameUpdate, value, oldValue);

            chieftainsTeamLabel = new(menu, this, menu.Translate("Team 4:"), new Vector2(arenaGameModeLabel.pos.x, dragonSlayersTeamNameUpdate.pos.y - 45), new Vector2(0, 20), false);
            chieftainsTeamNameUpdate = new(new Configurable<string>(RainMeadow.rainMeadowOptions.ChieftainTeamName.Value), new(chieftainsTeamLabel.pos.x + 50, chieftainsTeamLabel.pos.y), 150);

            chieftainsTeamNameUpdate.OnValueUpdate += (config, value, oldValue) => GetTeamNameUpdate(chieftainsTeamNameUpdate, value, oldValue);

            martyrColor = new OpTinyColorPicker(menu, new Vector2(martyrsTeamNameUpdate.pos.x + martyrsTeamNameUpdate.rect.size.x + 50, martyrsTeamNameUpdate.pos.y), TeamBattleMode.TeamColors[0], tabWrapper);
            chieftainColor = new OpTinyColorPicker(menu, new Vector2(chieftainsTeamNameUpdate.pos.x + chieftainsTeamNameUpdate.rect.size.x + 50, chieftainsTeamNameUpdate.pos.y), TeamBattleMode.TeamColors[1], tabWrapper);
            dragonSlayerColor = new OpTinyColorPicker(menu, new Vector2(dragonSlayersTeamNameUpdate.pos.x + dragonSlayersTeamNameUpdate.rect.size.x + 50, dragonSlayersTeamNameUpdate.pos.y), TeamBattleMode.TeamColors[2], tabWrapper);
            outlawColor = new OpTinyColorPicker(menu, new Vector2(outlawsTeamNameUpdate.pos.x + outlawsTeamNameUpdate.rect.size.x + 50, outlawsTeamNameUpdate.pos.y), TeamBattleMode.TeamColors[3], tabWrapper);

            martyrColor.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;
            chieftainColor.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;
            dragonSlayerColor.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;
            outlawColor.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;

            teamColorLerpLabel = new(menu, this, menu.Translate("Team Color Lerp Factor:"), new Vector2(chieftainsTeamLabel.pos.x, chieftainsTeamLabel.pos.y - 38), new Vector2(0, 20), false);
            teamLerpColorBox = new(new Configurable<float>(RainMeadow.rainMeadowOptions.TeamColorLerp.Value), new(teamColorLerpLabel.pos.x, teamColorLerpLabel.pos.y - 38), 50)
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("How strongly the team color mixes with your color"),
                accept = OpTextBox.Accept.Float
            };
            teamLerpColorBox.OnValueUpdate += (config, value, lastValue) =>
            {
                RainMeadow.rainMeadowOptions.TeamColorLerp.Value = teamLerpColorBox.valueFloat;
            };

            gameTeamWrapper = new(tabWrapper, arenaTeamComboBox);
            martyrWrapper = new(tabWrapper, martyrsTeamNameUpdate);
            outlawWrapper = new(tabWrapper, outlawsTeamNameUpdate);
            dragonSlayerWrapper = new(tabWrapper, dragonSlayersTeamNameUpdate);
            chieftanWrapper = new(tabWrapper, chieftainsTeamNameUpdate);
            teamLerpWrapper = new(tabWrapper, teamLerpColorBox);

            this.SafeAddSubobjects(tabWrapper, arenaGameModeLabel, martyrTeamLabel, outlawsTeamLabel, dragonSlayerWrapper, dragonsSlayersTeamLabel, chieftainsTeamLabel, teamColorLerpLabel, teamColorLerpLabel);
        }
        public void GetTeamNameUpdate(OpTextBox textBox, string newValue, string oldValue)
        {
            int value = -1;
            if (textBox == martyrsTeamNameUpdate)
            {
                RainMeadow.rainMeadowOptions.MartyrTeamName.Value = newValue;
                teamBattleMode.martyrsTeamName = newValue;
                value = 0;
            }
            if (textBox == outlawsTeamNameUpdate)
            {
                RainMeadow.rainMeadowOptions.OutlawsTeamName.Value = newValue;
                teamBattleMode.outlawTeamNames = newValue;
                value = 1;
            }
            if (textBox == dragonSlayersTeamNameUpdate)
            {
                RainMeadow.rainMeadowOptions.DragonSlayersTeamName.Value = newValue;
                teamBattleMode.dragonSlayersTeamNames = newValue;
                value = 2;
            }
            if (textBox == chieftainsTeamNameUpdate)
            {
                RainMeadow.rainMeadowOptions.ChieftainTeamName.Value = newValue;
                teamBattleMode.chieftainsTeamNames = newValue;
                value = 3;
            }
            UpdateDesiredNameList(value, newValue);
        }
        public void UpdateDesiredNameList(int desiredIndex, string newValue)
        {
            ListItem? item = arenaTeamComboBox.GetItemList().FirstOrDefault(x => x.value == desiredIndex);
            if (item.HasValue)
            {
                RainMeadow.Debug(item.Value.name);
                ListItem listItem = item.Value;
                listItem.name = newValue;
                listItem.desc = newValue;
                listItem.displayName = newValue;
                return;
            }
            RainMeadow.Error("Unable to find desired list item to change");
        }
        public void ColorSelector_OnValueChangedEvent()
        {
            TeamBattleMode.TeamColors[0] = Extensions.SafeColorRange(martyrColor.valuecolor);
            TeamBattleMode.TeamColors[1] = Extensions.SafeColorRange(outlawColor.valuecolor);
            TeamBattleMode.TeamColors[2] = Extensions.SafeColorRange(dragonSlayerColor.valuecolor);
            TeamBattleMode.TeamColors[3] = Extensions.SafeColorRange(chieftainColor.valuecolor);

        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

        }
        public override void Update()
        {
            if (tabWrapper.IsAllRemixUINotHeld() && tabWrapper.holdElement) tabWrapper.holdElement = false;
            base.Update();
            if (arenaTeamComboBox.greyedOut = arenaMode.currentGameMode != TeamBattleMode.TeamBattle.value)
                if (!arenaTeamComboBox.held && !teamComboBoxLastHeld) arenaTeamComboBox.value = arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team.ToString();
            if (!(OnlineManager.lobby?.isOwner == true))
            {
                UpdateDesiredNameList(0, teamBattleMode.martyrsTeamName);
                UpdateDesiredNameList(1, teamBattleMode.outlawTeamNames);
                UpdateDesiredNameList(2, teamBattleMode.dragonSlayersTeamNames);
                UpdateDesiredNameList(3, teamBattleMode.chieftainsTeamNames);
            }
        }

    }
}
