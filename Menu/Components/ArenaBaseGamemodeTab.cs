using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using RainMeadow.UI.Components.Patched;
using System.Linq;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
namespace RainMeadow.UI.Components
{
    public class OnlineArenaBaseGameModeTab
        : RectangularMenuObject
    {
        public MenuTabWrapper tabWrapper;
        public MenuLabel spearScoreLabel;
        public OpTextBox spearScoreTextBox;
        public MenuLabel aliveScoreLabel;
        public OpTextBox aliveScoreTextBox;
        public MenuLabel denEntryRuleLabel;
        public OpTextBox denScoreTextBox;
        public MenuLabel denScoreLabel;
        public OpComboBox2 denEntryRule;

        public MenuLabel emptyKillTagScoreLabel;
        public OpTextBox emptyKillTagScore;
        public EventfulScrollButton? prevButton,

            nextButton;
        public ArenaMode arena => OnlineManager.lobby.gameMode as ArenaOnlineGameMode;

        public List<int> unstableChallenges = new List<int> { 70 };

        public bool AllSettingsDisabled =>
            arena.initiateLobbyCountdown && arena.arenaClientSettings.ready;
        public bool OwnerSettingsDisabled =>
            !(OnlineManager.lobby?.isOwner == true) || AllSettingsDisabled || arena.externalArenaGameMode is ArenaChallengeMode;


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

            // --- Row 1: Spear Score ---
            spearScoreLabel = new(menu, this, menu.Translate("Kill Score:"),
                new(leftMargin, topOffset), new(labelWidth, 20f), false);
            spearScoreLabel.label.alignment = FLabelAlignment.Left;

            spearScoreTextBox = new(RainMeadow.rainMeadowOptions.ArenaSpearScore,
                new(boxMargin, topOffset - 2f), 60)
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("Points a kill is worth"),
                accept = OpTextBox.Accept.Int
            };

            spearScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (spearScoreTextBox.valueInt < 0) spearScoreTextBox.valueInt = 0;
                arena.spearScore = spearScoreTextBox.valueInt;
            };

            // --- Row 2: Win Score ---
            aliveScoreLabel = new(menu, this, menu.Translate("Survival Score:"),
                new(leftMargin, topOffset - rowHeight), new(labelWidth, 20f), false);
            aliveScoreLabel.label.alignment = FLabelAlignment.Left;

            aliveScoreTextBox = new(RainMeadow.rainMeadowOptions.ArenaAliveScore,
                new(boxMargin, topOffset - rowHeight - 2f), 60)
            { alignment = FLabelAlignment.Center, description = menu.Translate("Points for surviving inside the shelter"), accept = OpTextBox.Accept.Int };

            aliveScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (aliveScoreTextBox.valueInt < 0) aliveScoreTextBox.valueInt = 0;
                arena.aliveScore = aliveScoreTextBox.valueInt;
            };


            // --- Row 3: Den Score ---
            denScoreLabel = new(menu, this, menu.Translate("Den Score:"),
                    new(leftMargin, topOffset - (rowHeight * 2)), new(labelWidth, 20f), false);
            denScoreLabel.label.alignment = FLabelAlignment.Left;

            denScoreTextBox = new(RainMeadow.rainMeadowOptions.ArenaDenScore,
                new(boxMargin, topOffset - (rowHeight * 2) - 2f), 60) // FIXED: Added '- (rowHeight * 2)'
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("Points required to open dens"),
                accept = OpTextBox.Accept.Int
            };

            denScoreTextBox.OnValueUpdate += (config, value, oldValue) =>
                    {
                        if (denScoreTextBox.valueInt < 0) denScoreTextBox.valueInt = 0;
                        arena.denScore = denScoreTextBox.valueInt;
                    };

            // --- Row 4: Den Entry ---
            denEntryRuleLabel = new(menu, this, menu.Translate("Den Entry:"),
                new(leftMargin, topOffset - (rowHeight * 3)), new(labelWidth, 20f), false);
            denEntryRuleLabel.label.alignment = FLabelAlignment.Left;

            var denRuleItems = OpResourceSelector.GetEnumNames(null, typeof(ArenaSetup.GameTypeSetup.DenEntryRule))
                .Select(li =>
                {
                    li.displayName = menu.Translate(li.displayName);
                    return li;
                }).ToList();

            denEntryRule = new OpComboBox2(
                RainMeadow.rainMeadowOptions.ArenaDenType,
                new(boxMargin, topOffset - (rowHeight * 3) - 2f),
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

            this.SafeAddSubobjects(
                tabWrapper,
                spearScoreLabel,
                aliveScoreLabel,
                denEntryRuleLabel,
                denScoreLabel,
                emptyKillTagScoreLabel
            );
            new PatchedUIelementWrapper(tabWrapper, spearScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, denEntryRule);
            new PatchedUIelementWrapper(tabWrapper, aliveScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, denScoreTextBox);
            new PatchedUIelementWrapper(tabWrapper, emptyKillTagScore);

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
            RainMeadow.rainMeadowOptions.ArenaAliveScore.Value = arena.aliveScore;
            RainMeadow.rainMeadowOptions.ArenaDenType.Value = arena.denEntryRule;
            RainMeadow.rainMeadowOptions.ArenaDenScore.Value = arena.denScore;
            RainMeadow.rainMeadowOptions.ArenaEmptyKillTagScore.Value = arena.emptyKillTagScore;

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

        public override void Update()
        {
            base.Update();
            if (spearScoreTextBox != null)
            {
                spearScoreTextBox.held = spearScoreTextBox._KeyboardOn;
                if (!spearScoreTextBox.held)
                {
                    spearScoreTextBox.valueInt = arena.spearScore;
                }

                spearScoreTextBox.greyedOut = OwnerSettingsDisabled;
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
        }
    }
}