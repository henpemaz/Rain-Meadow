using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow.Arena.ArenaOnlineGameModes.Drown
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




        public DrownInterface(ArenaMode arena, DrownMode drown, Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            tabWrapper = new(menu, this);
            DROWN = drown;

            var pointsForRockLabel = new ProperlyAlignedMenuLabel(menu, owner, "Points required to buy a ... rock?", new Vector2(10f, 400), new Vector2(0, 20), false);
            pointsForRockTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.PointsForRock.Value), new Vector2(10, pointsForRockLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForRockTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.rockCost = pointsForRockTextBox.valueInt;
                RainMeadow.rainMeadowOptions.PointsForRock.Value = pointsForRockTextBox.valueInt;

            };
            UIelementWrapper pointsForRockWrapper = new UIelementWrapper(tabWrapper, pointsForRockTextBox);

            var pointsForSpearLabel = new ProperlyAlignedMenuLabel(menu, owner, "Points required to buy a spear", new Vector2(10f, pointsForRockTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForSpearTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.PointsForSpear.Value), new Vector2(10, pointsForSpearLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForSpearTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.spearCost = pointsForSpearTextBox.valueInt;
                RainMeadow.rainMeadowOptions.PointsForSpear.Value = pointsForSpearTextBox.valueInt;

            };
            UIelementWrapper pointsForSpearTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForSpearTextBox);

            var pointsForExplSpearLabel = new ProperlyAlignedMenuLabel(menu, owner, "Points required to buy an explosive spear", new Vector2(10f, pointsForSpearTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForExplSpearTextBox = new(new Configurable<int>(drown.spearExplCost), new Vector2(10, pointsForExplSpearLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForExplSpearTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.spearExplCost = pointsForExplSpearTextBox.valueInt;
                RainMeadow.rainMeadowOptions.PointsForExplSpear.Value = pointsForExplSpearTextBox.valueInt;


            };
            UIelementWrapper pointsForExplSpearTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForExplSpearTextBox);

            var pointsForBombLabel = new ProperlyAlignedMenuLabel(menu, owner, "Points required to buy a scav bomb", new Vector2(10f, pointsForExplSpearTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForBombTextBox = new(new Configurable<int>(drown.bombCost), new Vector2(10, pointsForBombLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForBombTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.bombCost = pointsForBombTextBox.valueInt;
                RainMeadow.rainMeadowOptions.PointsForBomb.Value = pointsForBombTextBox.valueInt;

            };
            UIelementWrapper pointsForBombTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForBombTextBox);

            var pointsForElectricSpear = new ProperlyAlignedMenuLabel(menu, owner, "[MSC]: Points required to buy an electric spear", new Vector2(10f, pointsForBombTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForElecSpear = new(new Configurable<int>(drown.electricSpearCost), new Vector2(10, pointsForElectricSpear.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = !ModManager.MSC || OwnerSettingsDisabled
            };
            pointsForElecSpear.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.electricSpearCost = pointsForElecSpear.valueInt;
                RainMeadow.rainMeadowOptions.PointsForElectricSpear.Value = pointsForElecSpear.valueInt;

            };

            UIelementWrapper pointsForElectricWrapper = new UIelementWrapper(tabWrapper, pointsForElecSpear);


            var pointsForBoomerang = new ProperlyAlignedMenuLabel(menu, owner, "[Watcher]: Points required to buy a boomerang", new Vector2(10f, pointsForElecSpear.pos.y - 15), new Vector2(0, 20), false);
            pointsForBoomerangText = new(new Configurable<int>(drown.boomerangeCost), new Vector2(10, pointsForBoomerang.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = !ModManager.Watcher || OwnerSettingsDisabled
            };
            pointsForBoomerangText.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.boomerangeCost = pointsForBoomerangText.valueInt;
                RainMeadow.rainMeadowOptions.PointsForBoomerang.Value = pointsForBoomerangText.valueInt;

            };

            UIelementWrapper pointsForBoomerangWrapper = new UIelementWrapper(tabWrapper, pointsForBoomerangText);


            var pointsForRespawnLabel = new ProperlyAlignedMenuLabel(menu, owner, "Points required to buy a respawn", new Vector2(10f, pointsForBoomerangText.pos.y - 15), new Vector2(0, 20), false);
            pointsForRespawnTextBox = new(new Configurable<int>(drown.respCost), new Vector2(10, pointsForRespawnLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForRespawnTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.respCost = pointsForRespawnTextBox.valueInt;
                RainMeadow.rainMeadowOptions.PointsForRespawn.Value = pointsForRespawnTextBox.valueInt;

            };
            UIelementWrapper pointsForRespawnTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForRespawnTextBox);

            var pointsForDenOpenLabel = new ProperlyAlignedMenuLabel(menu, owner, "Points required to open dens", new Vector2(10f, pointsForRespawnTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForDenOpenTextBox = new(new Configurable<int>(drown.denCost), new Vector2(10, pointsForDenOpenLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForDenOpenTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.denCost = pointsForDenOpenTextBox.valueInt;
                RainMeadow.rainMeadowOptions.PointsForDenOpen.Value = pointsForDenOpenTextBox.valueInt;

            };
            UIelementWrapper pointsForDenOpenTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForDenOpenTextBox);

            var creatureCleanupsLabel = new ProperlyAlignedMenuLabel(menu, owner, "How many waves before creature cleanup", new Vector2(10f, pointsForDenOpenTextBox.pos.y - 15), new Vector2(0, 20), false);
            creatureCleanupsTextBox = new(new Configurable<int>(drown.creatureCleanupWaves), new Vector2(10, creatureCleanupsLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            creatureCleanupsTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.creatureCleanupWaves = creatureCleanupsTextBox.valueInt;
                RainMeadow.rainMeadowOptions.CreatureCleanup.Value = creatureCleanupsTextBox.valueInt;

            };
            UIelementWrapper creatureCleanupsTextBoxWrapper = new UIelementWrapper(tabWrapper, creatureCleanupsTextBox);


            var maxCLLabel = new ProperlyAlignedMenuLabel(menu, owner, "Max creatures in level", new Vector2(10f, creatureCleanupsTextBox.pos.y - 15), new Vector2(0, 20), false);
            maxCTextBox = new(new Configurable<int>(drown.maxCreatures), new Vector2(10, maxCLLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled

            };
            maxCTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.maxCreatures = maxCTextBox.valueInt;
                RainMeadow.rainMeadowOptions.MaxCreatureCount.Value = maxCTextBox.valueInt;
            };
            UIelementWrapper maxCTextBoxWrapper = new UIelementWrapper(tabWrapper, maxCTextBox);

            this.SafeAddSubobjects(tabWrapper, maxCLLabel, maxCTextBoxWrapper,
                pointsForSpearLabel, pointsForSpearTextBoxWrapper, pointsForExplSpearLabel, pointsForExplSpearTextBoxWrapper, pointsForBoomerang, pointsForBoomerangWrapper, pointsForElectricSpear, pointsForElectricWrapper, pointsForRockWrapper, pointsForRockLabel,
                pointsForBombLabel, pointsForBombTextBoxWrapper, pointsForRespawnLabel, pointsForRespawnTextBoxWrapper,
                pointsForDenOpenLabel, pointsForDenOpenTextBoxWrapper, creatureCleanupsLabel, creatureCleanupsTextBoxWrapper);
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