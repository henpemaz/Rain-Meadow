using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
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
        public MenuLabel challengeNameLabel;
        public MenuLabel notifyUserToQueueMap;

        public EventfulScrollButton? prevButton,
            nextButton;
        public OpTextBox challengeIDTextBox;

        public float dividerX = 50,
            dividerY = 160;
        private int currentOffset;

        public ArenaMode arenaMode;
        public ArenaChallengeMode challengeMode;

        public ChallengeInformation.ChallengeMeta meta;

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
            challengeIDTextBox.accept = OpTextBox.Accept.Int;
            challengeIDTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                if (challengeIDTextBox.valueInt < 0)
                {
                    challengeIDTextBox.valueInt = 0;
                }
                challenge.challengeID = challengeIDTextBox.valueInt;
                meta = new ChallengeInformation.ChallengeMeta(challenge.challengeID);
            };

            new PatchedUIelementWrapper(tabWrapper, challengeIDTextBox);
            meta = new ChallengeInformation.ChallengeMeta(challenge.challengeID);
            challengeNameLabel = new MenuLabel(
                menu,
                this,
                $"{meta.arena}: {meta.GetMetaDescription(this.menu)}",
                new Vector2(size.x * 0.5f, dividerY + 150f),
                new Vector2(0, 0),
                true
            );

            notifyUserToQueueMap = new MenuLabel(
                menu,
                this,
                this.menu.Translate("Queue up any map to begin"),
                new Vector2(size.x * 0.5f, challengeNameLabel.pos.y + 40),
                new Vector2(0, 0),
                false
            );
            this.SafeAddSubobjects(
                tabWrapper,
                challengeIDLabel,
                challengeNameLabel,
                notifyUserToQueueMap
            );
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
            RainMeadow.rainMeadowOptions.ChallengeID.Value = challengeMode.challengeID;
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
            if (challengeNameLabel != null)
            {
                challengeNameLabel.text = $"{meta.arena}: {meta.GetMetaDescription(this.menu)}";
            }
        }

        public void SetCurrentlySelectedOfSeries(string id, int index) =>
            arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team = index;

        public int GetCurrentlySelectedOfSeries(string id) =>
            arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team;
    }
}
