using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
using RainMeadow.UI.Components.Patched;
using UnityEngine;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using System.Linq;
using Expedition;

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

        public EventfulScrollButton? prevButton,
            nextButton;
        public OpTextBox challengeIDTextBox;

        public float dividerX = 50,
            dividerY = 160;
        private int currentOffset;

        public ArenaMode arenaMode;
        public ArenaChallengeMode challengeMode;

        public ChallengeInformation.ChallengeMeta meta;

        public List<int> unstableChallenges = new List<int> { 62 };

        public bool changedChallenge;

        public int challengeIdForChangingSprite;

        public bool AllSettingsDisabled =>
            arenaMode.initiateLobbyCountdown && arenaMode.arenaClientSettings.ready;
        public bool OwnerSettingsDisabled =>
            !(OnlineManager.lobby?.isOwner == true) || AllSettingsDisabled;
        public FSprite previewSprite;
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
                $"{this.menu.Translate(meta.arena)}: {this.menu.Translate(meta.GetMetaDescription(this.menu))}",
                new Vector2(size.x * 0.5f, dividerY + 80f),
                new Vector2(0, 0),
                true
            );
            previewSprite = new FSprite("Futile_White")
            {
                x = size.x * 0.5f,
                y = challengeIDLabel.pos.y + 150,
                anchorX = 0.5f, // Changed from 0 to 0.5
                anchorY = 0.5f,
                scale = 1f
            };
            this.SafeAddSubobjects(
                tabWrapper,
                challengeIDLabel,
                challengeNameLabel


            );
            Container.AddChild(previewSprite);
            challengeIdForChangingSprite = challengeMode.challengeID;
            UpdatePreviewImage();
            if (menu is ArenaOnlineLobbyMenu m && challengeMode.challengeID > 0)
            {
                m.arenaMainLobbyPage.levelSelector.SelectedPlayList.Clear();
                m.arenaMainLobbyPage.levelSelector.AddItemToSelectedList(meta.arena);
            }
        }
        private void EnsureThumbnailLoaded(string arenaName)
        {
            string thumbName = arenaName + "_Thumb";

            if (Futile.atlasManager.DoesContainElementWithName(thumbName))
            {
                return;
            }

            // 2. Resolve the file path. Both vanilla and custom arena thumbnails 
            // are typically located in the "Levels" folder and end with "_thumb.png"
            string filePath = AssetManager.ResolveFilePath($"Levels{System.IO.Path.DirectorySeparatorChar}{arenaName}_thumb.png");

            // 3. If the file is found, load it manually into memory
            if (System.IO.File.Exists(filePath))
            {
                // Create a blank texture (size doesn't matter, LoadImage will resize it)
                Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

                // Important: Set filter mode to Point to keep Rain World's pixel art crisp!
                texture.filterMode = FilterMode.Point;

                // Read the file and apply it to the texture
                texture.LoadImage(System.IO.File.ReadAllBytes(filePath));

                // Inject the texture into Futile's Atlas Manager using the expected thumbName
                Futile.atlasManager.LoadAtlasFromTexture(thumbName, texture, false);
            }
            else
            {
                RainMeadow.Error($"Could not find thumbnail for arena: {arenaName}");
            }
        }
        private void UpdatePreviewImage()
        {
            if (meta == null || string.IsNullOrEmpty(meta.arena) || challengeMode.challengeID <= 0) return;

            EnsureThumbnailLoaded(meta.arena);
            string thumbName = meta.arena + "_Thumb";

            if (Futile.atlasManager.DoesContainElementWithName(thumbName))
            {
                previewSprite.element = Futile.atlasManager.GetElementWithName(thumbName);
                previewSprite.color = Color.white; // Reset color if it was hidden/tinted
            }
            else
            {
                // Fallback if the thumbnail isn't found (e.g., missing custom map thumb)
                if (Futile.atlasManager.DoesContainElementWithName("LevelThumb_Error"))
                {
                    previewSprite.element = Futile.atlasManager.GetElementWithName("LevelThumb_Error");
                }
                else
                {
                    // If no error thumb exists, just hide or tint a placeholder
                    previewSprite.element = Futile.atlasManager.GetElementWithName("Futile_White");
                    previewSprite.color = new Color(0.2f, 0.2f, 0.2f);
                }
            }
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
            if (challengeNameLabel != null)
            {
                previewSprite.x = drawPos.x + (size.x * 0.5f);
                previewSprite.y = drawPos.y + challengeNameLabel.pos.y + 100f;
            }
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
                if (challengeIdForChangingSprite != challengeMode.challengeID && challengeMode.challengeID > 0)
                {
                    UpdatePreviewImage();
                    if (menu is ArenaOnlineLobbyMenu m)
                    {
                        m.arenaMainLobbyPage.levelSelector.SelectedPlayList.Clear();
                        m.arenaMainLobbyPage.levelSelector.AddItemToSelectedList(meta.arena);
                    }
                    challengeIdForChangingSprite = challengeMode.challengeID;
                }
                challengeNameLabel.text = $"{meta.arena}: {meta.GetMetaDescription(this.menu)}";
                if (unstableChallenges.Contains(challengeMode.challengeID))
                {
                    challengeNameLabel.label.color = Color.red;
                }
                else
                {
                    challengeNameLabel.label.color = Futile.white;
                }
            }
        }

        public void SetCurrentlySelectedOfSeries(string id, int index) =>
            arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team = index;

        public int GetCurrentlySelectedOfSeries(string id) =>
            arenaMode.clientSettings.GetData<ArenaTeamClientSettings>().team;
    }
}
