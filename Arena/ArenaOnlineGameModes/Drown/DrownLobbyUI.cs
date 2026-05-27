using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class DrownInterface : RectangularMenuObject
    {
        public FSprite divider;
        public MenuTabWrapper tabWrapper;
        public EventfulScrollButton? prevButton, nextButton;
        private int currentOffset;

        public ArenaMode arenaMode;
        public DrownMode DROWN;
        public bool OwnerSettingsDisabled => !(OnlineManager.lobby?.isOwner == true);


        public OpTextBox? maxCreaturesTextBox;
        public OpTextBox? maxCTextBox;
        public OpTextBox? pointsForSpearTextBox;
        public OpTextBox? pointsForExplSpearTextBox;
        public OpTextBox? pointsForBombTextBox;
        public OpTextBox? pointsForElecSpear;
        public OpTextBox? pointsForBoomerangText;
        public OpTextBox? pointsForRockTextBox;

        public OpTextBox? pointsForRespawnTextBox;
        public OpTextBox? pointsForDenOpenTextBox;
        public OpTextBox? creatureCleanupsTextBox;
        public OpKeyBinder? storeButton;



        public DrownInterface(ArenaMode arena, DrownMode drown, Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            tabWrapper = new(menu, this);
            DROWN = drown;

            // --- 1. STORE KEYBIND ---
            var storeButtonLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Store keybind"), new Vector2(10f, 440f), new Vector2(0, 20), false);

            storeButton = new SafeKeyBinder(RainMeadow.rainMeadowOptions.DrownStoreKey, new Vector2(10, storeButtonLabel.pos.y - 25), new Vector2(100, 10), false);
            storeButton.OnValueUpdate += (config, value, oldValue) =>
            {
                if (System.Enum.TryParse(value, out KeyCode newKey))
                {
                    RainMeadow.rainMeadowOptions.DrownStoreKey.Value = newKey;
                }
            };
            UIelementWrapper storeButtonWrapper = new UIelementWrapper(tabWrapper, storeButton);

            // --- 2. ROCK ---

            var pointsForRockLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to buy a ... rock?"), new Vector2(10f, storeButton.pos.y - 15), new Vector2(0, 20), false);
            pointsForRockTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.DrownPointsForRock.Value), new Vector2(10, pointsForRockLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForRockTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.rockCost = pointsForRockTextBox.valueInt;
                RainMeadow.rainMeadowOptions.DrownPointsForRock.Value = pointsForRockTextBox.valueInt;

            };
            UIelementWrapper pointsForRockWrapper = new UIelementWrapper(tabWrapper, pointsForRockTextBox);

            // --- 3. SPEAR ---
            var pointsForSpearLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to buy a spear"), new Vector2(10f, pointsForRockTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForSpearTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.DrownPointsForSpear.Value), new Vector2(10, pointsForSpearLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForSpearTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.spearCost = pointsForSpearTextBox.valueInt;
                RainMeadow.rainMeadowOptions.DrownPointsForSpear.Value = pointsForSpearTextBox.valueInt;

            };
            UIelementWrapper pointsForSpearTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForSpearTextBox);

            // --- 4. EXPLOSIVE SPEAR ---
            var pointsForExplSpearLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to buy an explosive spear"), new Vector2(10f, pointsForSpearTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForExplSpearTextBox = new(new Configurable<int>(drown.spearExplCost), new Vector2(10, pointsForExplSpearLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForExplSpearTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.spearExplCost = pointsForExplSpearTextBox.valueInt;
                RainMeadow.rainMeadowOptions.DrownPointsForExplSpear.Value = pointsForExplSpearTextBox.valueInt;


            };
            UIelementWrapper pointsForExplSpearTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForExplSpearTextBox);

            // --- 5. SCAV BOMB ---
            var pointsForBombLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to buy a scav bomb"), new Vector2(10f, pointsForExplSpearTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForBombTextBox = new(new Configurable<int>(drown.bombCost), new Vector2(10, pointsForBombLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForBombTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.bombCost = pointsForBombTextBox.valueInt;
                RainMeadow.rainMeadowOptions.DrownPointsForBomb.Value = pointsForBombTextBox.valueInt;

            };
            UIelementWrapper pointsForBombTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForBombTextBox);

            // --- 6. ELECTRIC SPEAR ---
            var pointsForElectricSpear = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("[MSC]: Points required to buy an electric spear"), new Vector2(10f, pointsForBombTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForElecSpear = new(new Configurable<int>(drown.electricSpearCost), new Vector2(10, pointsForElectricSpear.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = !ModManager.MSC || OwnerSettingsDisabled
            };
            pointsForElecSpear.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.electricSpearCost = pointsForElecSpear.valueInt;
                RainMeadow.rainMeadowOptions.DrownPointsForElectricSpear.Value = pointsForElecSpear.valueInt;

            };
            UIelementWrapper pointsForElectricWrapper = new UIelementWrapper(tabWrapper, pointsForElecSpear);

            // --- 7. BOOMERANG ---
            var pointsForBoomerang = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("[Watcher]: Points required to buy a boomerang"), new Vector2(10f, pointsForElecSpear.pos.y - 15), new Vector2(0, 20), false);
            pointsForBoomerangText = new(new Configurable<int>(drown.boomerangeCost), new Vector2(10, pointsForBoomerang.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = !ModManager.Watcher || OwnerSettingsDisabled
            };
            pointsForBoomerangText.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.boomerangeCost = pointsForBoomerangText.valueInt;
                RainMeadow.rainMeadowOptions.DrownPointsForBoomerang.Value = pointsForBoomerangText.valueInt;

            };
            UIelementWrapper pointsForBoomerangWrapper = new UIelementWrapper(tabWrapper, pointsForBoomerangText);

            // --- 8. RESPAWN ---
            var pointsForRespawnLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to buy a respawn"), new Vector2(10f, pointsForBoomerangText.pos.y - 15), new Vector2(0, 20), false);
            pointsForRespawnTextBox = new(new Configurable<int>(drown.respCost), new Vector2(10, pointsForRespawnLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForRespawnTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.respCost = pointsForRespawnTextBox.valueInt;
                RainMeadow.rainMeadowOptions.DrownPointsForRespawn.Value = pointsForRespawnTextBox.valueInt;

            };
            UIelementWrapper pointsForRespawnTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForRespawnTextBox);

            // --- 9. OPEN DENS ---
            var pointsForDenOpenLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to open dens"), new Vector2(10f, pointsForRespawnTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForDenOpenTextBox = new(new Configurable<int>(drown.denCost), new Vector2(10, pointsForDenOpenLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForDenOpenTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.denCost = pointsForDenOpenTextBox.valueInt;
                RainMeadow.rainMeadowOptions.DrownPointsForDenOpen.Value = pointsForDenOpenTextBox.valueInt;

            };
            UIelementWrapper pointsForDenOpenTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForDenOpenTextBox);

            // --- 10. CLEANUPS ---
            var creatureCleanupsLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("How many waves before creature cleanup"), new Vector2(10f, pointsForDenOpenTextBox.pos.y - 15), new Vector2(0, 20), false);
            creatureCleanupsTextBox = new(new Configurable<int>(drown.creatureCleanupWaves), new Vector2(10, creatureCleanupsLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            creatureCleanupsTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.creatureCleanupWaves = creatureCleanupsTextBox.valueInt;
                RainMeadow.rainMeadowOptions.DrownCreatureCleanup.Value = creatureCleanupsTextBox.valueInt;

            };
            UIelementWrapper creatureCleanupsTextBoxWrapper = new UIelementWrapper(tabWrapper, creatureCleanupsTextBox);

            // --- 11. MAX CREATURES ---
            var maxCLLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Max creatures in level"), new Vector2(10f, creatureCleanupsTextBox.pos.y - 15), new Vector2(0, 20), false);
            maxCTextBox = new(new Configurable<int>(drown.maxCreatures), new Vector2(10, maxCLLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            maxCTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.maxCreatures = maxCTextBox.valueInt;
                RainMeadow.rainMeadowOptions.DrownMaxCreatureCount.Value = maxCTextBox.valueInt;
            };
            UIelementWrapper maxCTextBoxWrapper = new UIelementWrapper(tabWrapper, maxCTextBox);


            this.SafeAddSubobjects(tabWrapper,
                storeButtonLabel, storeButtonWrapper,
                pointsForRockLabel, pointsForRockWrapper,
                pointsForSpearLabel, pointsForSpearTextBoxWrapper,
                pointsForExplSpearLabel, pointsForExplSpearTextBoxWrapper,
                pointsForBombLabel, pointsForBombTextBoxWrapper,
                pointsForElectricSpear, pointsForElectricWrapper,
                pointsForBoomerang, pointsForBoomerangWrapper,
                pointsForRespawnLabel, pointsForRespawnTextBoxWrapper,
                pointsForDenOpenLabel, pointsForDenOpenTextBoxWrapper,
                creatureCleanupsLabel, creatureCleanupsTextBoxWrapper,
                maxCLLabel, maxCTextBoxWrapper);
        }
        public void PopulatePage(int offset)
        {
            ClearInterface();
            tabWrapper._tab.myContainer.MoveToFront();
        }
        public void ClearInterface()
        {
            //UnloadAnyConfig(teamColorPickers)

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


        public void OnShutdown()
        {
            if (!(OnlineManager.lobby?.isOwner == true)) return;

        }
        public void CreatePageButtons()
        {
        }
        //public void DeletePageButtons()
        //{
        //    this.ClearMenuObject(ref prevButton);
        //    this.ClearMenuObject(ref nextButton);
        //}
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

            if (maxCTextBox != null)
            {
                maxCTextBox.valueInt = DROWN.maxCreatures;
            }

            if (pointsForRockTextBox != null)
            {
                pointsForRockTextBox.valueInt = DROWN.rockCost;
            }
            if (pointsForSpearTextBox != null)
            {
                pointsForSpearTextBox.valueInt = DROWN.spearCost;
            }
            if (pointsForExplSpearTextBox != null)
            {
                pointsForExplSpearTextBox.valueInt = DROWN.spearExplCost;
            }
            if (pointsForBombTextBox != null)
            {
                pointsForBombTextBox.valueInt = DROWN.bombCost;
            }
            if (pointsForElecSpear != null)
            {
                pointsForElecSpear.valueInt = DROWN.electricSpearCost;
            }

            if (pointsForBoomerangText != null)
            {
                pointsForBoomerangText.valueInt = DROWN.boomerangeCost;
            }
            if (pointsForRespawnTextBox != null)
            {
                pointsForRespawnTextBox.valueInt = DROWN.respCost;
            }
            if (pointsForDenOpenTextBox != null)
            {
                pointsForDenOpenTextBox.valueInt = DROWN.denCost;
            }
            if (creatureCleanupsTextBox != null)
            {
                creatureCleanupsTextBox.valueInt = DROWN.creatureCleanupWaves;
            }


        }

    }
}