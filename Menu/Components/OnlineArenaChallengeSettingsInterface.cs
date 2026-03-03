using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
using RainMeadow.UI.Components.Patched;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;

namespace RainMeadow.UI.Components
{
    public class OnlineArenaChallengeSettingsInterface
        : RectangularMenuObject,
            SelectOneButton.SelectOneButtonOwner
    {
        public FSprite divider;
        public MenuTabWrapper tabWrapper;
        public MenuLabel challengeIDLabel;

        public EventfulScrollButton? prevButton,
            nextButton;
        public OpTextBox challengeIDTextBox;

        public float dividerX = 50,
            dividerY = 160;
        private int currentOffset;

        public ArenaMode arenaMode;
        public ArenaChallengeMode challengeMode;

        public bool AllSettingsDisabled =>
            arenaMode.initiateLobbyCountdown && arenaMode.arenaClientSettings.ready;
        public bool OwnerSettingsDisabled =>
            !(OnlineManager.lobby?.isOwner == true) || AllSettingsDisabled;

        public OnlineArenaChallengeSettingsInterface(
            ArenaMode arena,
            ArenaChallengeMode challenge,
            Menu.Menu menu,
            MenuObject owner,
            Vector2 pos,
            Vector2 size
        )
            : base(menu, owner, pos, size)
        {
            arenaMode = arena;
            challengeMode = challenge;
            divider = new("pixel") { anchorX = 0, scaleY = 2 };
            Container.AddChild(divider);
            tabWrapper = new(menu, this);
            challengeIDTextBox = new(
                new Configurable<int>(challenge.challengeID),
                new(size.x * 0.5f - 30, 20),
                60
            )
            {
                alignment = FLabelAlignment.Center,
                description = menu.Translate("Challenge ID"),
            };
            challengeIDTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                challenge.challengeID = challengeIDTextBox.valueInt;
                RainMeadow.Debug(challenge.challengeID);
            };

            new PatchedUIelementWrapper(tabWrapper, challengeIDTextBox);
            challengeIDLabel = new(
                menu,
                this,
                menu.Translate("Challenge ID"),
                new(
                    challengeIDTextBox.pos.x,
                    challengeIDTextBox.pos.y + challengeIDTextBox.size.y + 10
                ),
                new(challengeIDTextBox.size.x, 0),
                false
            );

            this.SafeAddSubobjects(tabWrapper, challengeIDLabel);
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
        }

        // public void CreatePageButtons()
        // {
        //     if (prevButton == null)
        //     {
        //         prevButton = new(menu, this, new(40, 20), 4, 24);
        //         prevButton.OnClick += _ => PrevPage();
        //         this.SafeAddSubobjects(prevButton);
        //     }
        //     if (nextButton == null)
        //     {
        //         nextButton = new(menu, this, new(size.x - 40, prevButton.pos.y), 1, 24);
        //         nextButton.OnClick += _ => NextPage();
        //         this.SafeAddSubobjects(nextButton);
        //     }
        // }

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
            Vector2 drawPos = DrawPos(timeStacker),
                drawSize = DrawSize(timeStacker);
            divider.x = drawPos.x + dividerX;
            divider.y = drawPos.y + dividerY;
            divider.scaleX = drawSize.x - dividerX * 2;
            divider.color = MenuColorEffect.rgbDarkGrey;
            challengeIDLabel.label.color = challengeIDTextBox.rect.colorEdge;
        }

        public override void Update()
        {
            base.Update();

            if (challengeIDTextBox != null)
            {
                challengeIDTextBox.held = challengeIDTextBox._KeyboardOn;
                challengeIDTextBox.valueInt = challengeMode.challengeID;
                challengeIDTextBox.greyedOut = OwnerSettingsDisabled;
            }

            // if (!challengeIDTextBox.held)
            //     challengeIDTextBox.valueFloat = challengeMode.lerp;
            // if (prevButton != null)
            //     prevButton.buttonBehav.greyedOut = !(CurrentOffset > 0);
            // if (nextButton != null)
            //     nextButton.buttonBehav.greyedOut = !(CurrentOffset < MaxOffset);
            // if (friendlyFireCheckbox != null)
            // {
            //     friendlyFireCheckbox.SetValueBool(this.arenaMode.friendlyFire);
            //     friendlyFireCheckbox.greyedOut = OwnerSettingsDisabled;
            // }
        }

        public void SetCurrentlySelectedOfSeries(string id, int index) =>
            arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team = index;

        public int GetCurrentlySelectedOfSeries(string id) =>
            arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team;

        public class TeamButton : EventfulSelectOneButton
        {
            public string teamName;
            public float widthOfText = 190;
            public FSprite symbol;
            public MenuLabel teamLabel;
            public ProperlyAlignedMenuLabel teamCount;
            public Color teamColor = Color.white;

            public TeamButton(
                Menu.Menu menu,
                MenuObject owner,
                Vector2 pos,
                Vector2 size,
                EventfulSelectOneButton[] selectButtons,
                int index,
                string teamName,
                string symbolName,
                float symbolSpriteScale = 2.8f
            )
                : base(menu, owner, "", "TEAM", pos, size, selectButtons, index)
            {
                this.teamName = teamName;
                symbol = new(symbolName) { scale = symbolSpriteScale };
                Container.AddChild(symbol);
                teamLabel = new(menu, this, this.teamName, new(0, -25), new(size.x, 20), true);
                teamCount = new(
                    menu,
                    this,
                    "",
                    new Vector2(symbol.x + 10, symbol.y + 10),
                    new(10, 10),
                    false
                );
                this.SafeAddSubobjects(teamLabel, teamCount);
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                Vector2 drawPos = DrawPos(timeStacker),
                    drawSize = DrawSize(timeStacker);
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
