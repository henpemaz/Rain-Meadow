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

        public ArenaOnlineGameMode arenaMode;
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


        private static int NonNegative(OpTextBox textBox) => Mathf.Max(0, textBox.valueInt);
        private static void SyncTextBox(OpTextBox? textBox, int value, bool greyedOut)
        {
            if (textBox == null) return;

            textBox.held = textBox._KeyboardOn;
            if (!textBox.held)
            {
                textBox.valueInt = value;
            }

            textBox.greyedOut = greyedOut;
        }

        public DrownInterface(ArenaOnlineGameMode arena, DrownMode drown, Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
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
                DROWN.RockCost = NonNegative(pointsForRockTextBox);
                RainMeadow.rainMeadowOptions.DrownPointsForRock.Value = DROWN.RockCost;

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
                DROWN.SpearCost = NonNegative(pointsForSpearTextBox);
                RainMeadow.rainMeadowOptions.DrownPointsForSpear.Value = DROWN.SpearCost;

            };
            UIelementWrapper pointsForSpearTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForSpearTextBox);

            // --- 4. EXPLOSIVE SPEAR ---
            var pointsForExplSpearLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to buy an explosive spear"), new Vector2(10f, pointsForSpearTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForExplSpearTextBox = new(new Configurable<int>(drown.ExplosiveSpearCost), new Vector2(10, pointsForExplSpearLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForExplSpearTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.ExplosiveSpearCost = NonNegative(pointsForExplSpearTextBox);
                RainMeadow.rainMeadowOptions.DrownPointsForExplSpear.Value = DROWN.ExplosiveSpearCost;


            };
            UIelementWrapper pointsForExplSpearTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForExplSpearTextBox);

            // --- 5. SCAV BOMB ---
            var pointsForBombLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to buy a scav bomb"), new Vector2(10f, pointsForExplSpearTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForBombTextBox = new(new Configurable<int>(drown.BombCost), new Vector2(10, pointsForBombLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForBombTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.BombCost = NonNegative(pointsForBombTextBox);
                RainMeadow.rainMeadowOptions.DrownPointsForBomb.Value = DROWN.BombCost;

            };
            UIelementWrapper pointsForBombTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForBombTextBox);

            // --- 6. ELECTRIC SPEAR ---
            var pointsForElectricSpear = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("[MSC]: Points required to buy an electric spear"), new Vector2(10f, pointsForBombTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForElecSpear = new(new Configurable<int>(drown.ElectricSpearCost), new Vector2(10, pointsForElectricSpear.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = !ModManager.MSC || OwnerSettingsDisabled
            };
            pointsForElecSpear.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.ElectricSpearCost = NonNegative(pointsForElecSpear);
                RainMeadow.rainMeadowOptions.DrownPointsForElectricSpear.Value = DROWN.ElectricSpearCost;

            };
            UIelementWrapper pointsForElectricWrapper = new UIelementWrapper(tabWrapper, pointsForElecSpear);

            // --- 7. BOOMERANG ---
            var pointsForBoomerang = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("[Watcher]: Points required to buy a boomerang"), new Vector2(10f, pointsForElecSpear.pos.y - 15), new Vector2(0, 20), false);
            pointsForBoomerangText = new(new Configurable<int>(drown.BoomerangCost), new Vector2(10, pointsForBoomerang.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = !ModManager.Watcher || OwnerSettingsDisabled
            };
            pointsForBoomerangText.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.BoomerangCost = NonNegative(pointsForBoomerangText);
                RainMeadow.rainMeadowOptions.DrownPointsForBoomerang.Value = DROWN.BoomerangCost;

            };
            UIelementWrapper pointsForBoomerangWrapper = new UIelementWrapper(tabWrapper, pointsForBoomerangText);

            // --- 8. RESPAWN ---
            var pointsForRespawnLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to buy a respawn"), new Vector2(10f, pointsForBoomerangText.pos.y - 15), new Vector2(0, 20), false);
            pointsForRespawnTextBox = new(new Configurable<int>(drown.RespCost), new Vector2(10, pointsForRespawnLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForRespawnTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.RespCost = NonNegative(pointsForRespawnTextBox);
                RainMeadow.rainMeadowOptions.DrownPointsForRespawn.Value = DROWN.RespCost;

            };
            UIelementWrapper pointsForRespawnTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForRespawnTextBox);

            // --- 9. OPEN DENS ---
            var pointsForDenOpenLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Points required to open dens"), new Vector2(10f, pointsForRespawnTextBox.pos.y - 15), new Vector2(0, 20), false);
            pointsForDenOpenTextBox = new(new Configurable<int>(drown.DenCost), new Vector2(10, pointsForDenOpenLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            pointsForDenOpenTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.DenCost = NonNegative(pointsForDenOpenTextBox);
                RainMeadow.rainMeadowOptions.DrownPointsForDenOpen.Value = DROWN.DenCost;

            };
            UIelementWrapper pointsForDenOpenTextBoxWrapper = new UIelementWrapper(tabWrapper, pointsForDenOpenTextBox);

            // --- 10. CLEANUPS ---
            var creatureCleanupsLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("How many waves before creature cleanup"), new Vector2(10f, pointsForDenOpenTextBox.pos.y - 15), new Vector2(0, 20), false);
            creatureCleanupsTextBox = new(new Configurable<int>(drown.CreatureCleanupWaves), new Vector2(10, creatureCleanupsLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            creatureCleanupsTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                // Used as a modulo divisor when scheduling cleanups, so it must never reach 0.
                DROWN.CreatureCleanupWaves = Mathf.Max(1, creatureCleanupsTextBox.valueInt);
                RainMeadow.rainMeadowOptions.DrownCreatureCleanup.Value = DROWN.CreatureCleanupWaves;

            };
            UIelementWrapper creatureCleanupsTextBoxWrapper = new UIelementWrapper(tabWrapper, creatureCleanupsTextBox);

            // --- 11. MAX CREATURES ---
            var maxCLLabel = new ProperlyAlignedMenuLabel(menu, owner, menu.Translate("Creature limit before waves pause"), new Vector2(10f, creatureCleanupsTextBox.pos.y - 15), new Vector2(0, 20), false);
            maxCTextBox = new(new Configurable<int>(drown.MaxCreatures), new Vector2(10, maxCLLabel.pos.y - 25), 160f)
            {
                accept = OpTextBox.Accept.Int,
                greyedOut = OwnerSettingsDisabled
            };
            maxCTextBox.OnValueUpdate += (config, value, oldValue) =>
            {
                DROWN.MaxCreatures = NonNegative(maxCTextBox);
                RainMeadow.rainMeadowOptions.DrownMaxCreatureCount.Value = DROWN.MaxCreatures;
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

            SyncTextBox(pointsForRockTextBox, DROWN.RockCost, OwnerSettingsDisabled);
            SyncTextBox(pointsForSpearTextBox, DROWN.SpearCost, OwnerSettingsDisabled);
            SyncTextBox(pointsForExplSpearTextBox, DROWN.ExplosiveSpearCost, OwnerSettingsDisabled);
            SyncTextBox(pointsForBombTextBox, DROWN.BombCost, OwnerSettingsDisabled);
            SyncTextBox(pointsForElecSpear, DROWN.ElectricSpearCost, !ModManager.MSC || OwnerSettingsDisabled);
            SyncTextBox(pointsForBoomerangText, DROWN.BoomerangCost, !ModManager.Watcher || OwnerSettingsDisabled);
            SyncTextBox(pointsForRespawnTextBox, DROWN.RespCost, OwnerSettingsDisabled);
            SyncTextBox(pointsForDenOpenTextBox, DROWN.DenCost, OwnerSettingsDisabled);
            SyncTextBox(creatureCleanupsTextBox, DROWN.CreatureCleanupWaves, OwnerSettingsDisabled);
            SyncTextBox(maxCTextBox, DROWN.MaxCreatures, OwnerSettingsDisabled);
        }

    }
}
