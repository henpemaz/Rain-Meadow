using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using RainMeadow.UI.Components.Patched;
using System.Linq;
namespace RainMeadow.UI.Components
{
    public class OnlineArenaBaseGameModeTab
        : RectangularMenuObject
    {
        public MenuTabWrapper tabWrapper;
        public MenuLabel spearScoreLabel;
        public OpTextBox spearScoreTextBox;
        public MenuLabel winScoreLabel;
        public OpTextBox winScoreTextBox;
        public MenuLabel denEntryRuleLabel;

        public OpComboBox2 denEntryRule;
        public EventfulScrollButton? prevButton,
            nextButton;
        public ArenaMode arena => OnlineManager.lobby.gameMode as ArenaOnlineGameMode;

        public List<int> unstableChallenges = new List<int> { 70 };

        public bool AllSettingsDisabled =>
            arena.initiateLobbyCountdown && arena.arenaClientSettings.ready;
        public bool OwnerSettingsDisabled =>
            !(OnlineManager.lobby?.isOwner == true) || AllSettingsDisabled;


        // TODO: sync arena.spearScore, arena.winScore, arena.denEntryRule
        public OnlineArenaBaseGameModeTab(
            Menu.Menu menu,
            MenuObject owner,
            Vector2 pos,
            Vector2 size
        )
            : base(menu, owner, pos, size)
        {
            tabWrapper = new(menu, this);
            float leftMargin = 20f;
            float labelWidth = 100f;
            float topOffset = size.y - 60f;
            float rowHeight = 40f;
            float boxMargin = leftMargin + labelWidth + 30f; // The X-position for all boxes
            // --- Row 1: Spear Score ---
            spearScoreLabel = new(menu, this, menu.Translate("Spear Score:"),
                new(leftMargin, topOffset), new(labelWidth, 20f), false);
            spearScoreLabel.label.alignment = FLabelAlignment.Left;

            spearScoreTextBox = new(new Configurable<int>(arena.spearScore),
                new(boxMargin, topOffset - 2f), 60) // Now using boxMargin
            { alignment = FLabelAlignment.Center, description = menu.Translate("Adjust points per spear kill") };

            // --- Row 2: Win Score ---
            winScoreLabel = new(menu, this, menu.Translate("Win Score:"),
                new(leftMargin, topOffset - rowHeight), new(labelWidth, 20f), false);
            winScoreLabel.label.alignment = FLabelAlignment.Left;

            winScoreTextBox = new(new Configurable<int>(arena.winScore),
                new(boxMargin, (topOffset - rowHeight) - 2f), 60)
            { alignment = FLabelAlignment.Center, description = menu.Translate("How many points a win is worth") };

            // --- Row 3: Den Entry ---
            denEntryRuleLabel = new(menu, this, menu.Translate("Den Entry:"),
                new(leftMargin, topOffset - (rowHeight * 2)), new(labelWidth, 20f), false);
            denEntryRuleLabel.label.alignment = FLabelAlignment.Left;

            spearScoreTextBox.accept = OpTextBox.Accept.Int;
            spearScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (spearScoreTextBox.valueInt < 0) spearScoreTextBox.valueInt = 0;
                arena.spearScore = spearScoreTextBox.valueInt;
            };

            var denRuleItems = OpResourceSelector.GetEnumNames(null, typeof(ArenaSetup.GameTypeSetup.DenEntryRule))
    .Select(li =>
    {
        li.displayName = menu.Translate(li.displayName);
        return li;
    }).ToList();
            denEntryRule = new OpComboBox2(
                new Configurable<ArenaSetup.GameTypeSetup.DenEntryRule>(ArenaSetup.GameTypeSetup.DenEntryRule.Standard),
                new(boxMargin, (topOffset - (rowHeight * 2)) - 5f),
                110,
                denRuleItems
            )
            {
                description = menu.Translate("Select den entry behavior"),
            };

            winScoreTextBox.accept = OpTextBox.Accept.Int;
            winScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (winScoreTextBox.valueInt < 0) winScoreTextBox.valueInt = 0;
                arena.winScore = winScoreTextBox.valueInt;
            };

            spearScoreTextBox.accept = OpTextBox.Accept.Int;
            spearScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (spearScoreTextBox.valueInt < 0)
                {
                    spearScoreTextBox.valueInt = 0;
                }
            };

            spearScoreTextBox.accept = OpTextBox.Accept.Int;
            spearScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (spearScoreTextBox.valueInt < 0)
                {
                    spearScoreTextBox.valueInt = 0;
                }
            };


            denEntryRule.OnValueChanged += (UIconfig, value, oldValue) =>
            {
                var result = new ArenaSetup.GameTypeSetup.DenEntryRule(value);
                arena.denEntryRule = result.Index;
            };

            this.SafeAddSubobjects(
                tabWrapper,
                spearScoreLabel,
                winScoreLabel,
                denEntryRuleLabel
            );
            new PatchedUIelementWrapper(tabWrapper, spearScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, denEntryRule);
            new PatchedUIelementWrapper(tabWrapper, winScoreTextBox);

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
            RainMeadow.rainMeadowOptions.ArenaSpearScore.Value = arena.spearScore;
            RainMeadow.rainMeadowOptions.ArenaWinScore.Value = arena.winScore;
            RainMeadow.rainMeadowOptions.ArenaDenType.Value = arena.denEntryRule;

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

        public override void Update()
        {
            base.Update();
            if (spearScoreTextBox != null)
            {
                spearScoreTextBox.held = spearScoreTextBox._KeyboardOn;

                spearScoreTextBox.valueInt = arena.spearScore;
                spearScoreTextBox.greyedOut = OwnerSettingsDisabled;
            }
            if (winScoreTextBox != null)
            {
                winScoreTextBox.greyedOut = OwnerSettingsDisabled;
                winScoreTextBox.held = winScoreTextBox._KeyboardOn;

                winScoreTextBox.valueInt = arena.spearScore;
            }
            if (denEntryRule != null)
            {
                denEntryRule.greyedOut = OwnerSettingsDisabled;
                string ruleName = ArenaSetup.GameTypeSetup.DenEntryRule.values.GetEntry(arena.denEntryRule);
                denEntryRule.value = ruleName;
            }
        }
    }
}