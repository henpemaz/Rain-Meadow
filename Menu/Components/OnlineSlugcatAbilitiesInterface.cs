using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RainMeadow.UI.Components.Patched;
using RainMeadow.UI.Interfaces;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RainMeadow.UI.Components.TabContainer;
using ArenaMode = RainMeadow.ArenaOnlineGameMode;

namespace RainMeadow.UI.Components
{
    public class OnlineSlugcatAbilitiesInterface : PositionedMenuObject
    {
        public const string WATCHERSETTINGS = "WATCHERSETTINGS", MSCSETTINGS = "MSCSETTINGS", BACKTOSELECT = "BACKTOSELECTSETTINGS";
        public SettingsPage? activeSettings;
        public Dictionary<string, SettingsPage> settingSignals = [];
        public MSCSettingsPage? mscSettingsTab;
        public WatcherSettingsPage? watcherSettingsTab;
        public SelectSettingsPage? selectSettings;
        public OnlineSlugcatAbilitiesInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName) : base(menu, owner, pos)
        {
            AddAllSettings(painCatName);
            if (settingSignals.Count > 1)
            {
                //no settingSignals should have BACKTOSELECT when selectSettings is being instansized
                AddSettingsTab(selectSettings = new(menu, this, settingSignals.Where(x => x.Key != BACKTOSELECT).ToDictionary()), BACKTOSELECT);
                SwitchTab(selectSettings);
            }
        }
        public void SaveAllInterfaceOptions()
        {
            foreach (SettingsPage settings in settingSignals.Values)
                settings.SaveInterfaceOptions();
        }
        public void CallForSync() //call this after ctor if needed for sync at start
        {
            // dusty says this does something, just trust them future Timbits
            foreach (SettingsPage settings in settingSignals.Values)
                settings.CallForSync();
        }
        public void AddAllSettings(string paincatName)
        {
            if (ModManager.MSC)
            {
                mscSettingsTab = new(menu, this, new(0f, 50f), paincatName);
                AddSettingsTab(mscSettingsTab, MSCSETTINGS);
            }
            if (ModManager.Watcher)
            {
                watcherSettingsTab = new(menu, this, new(0f, 50f));
                AddSettingsTab(watcherSettingsTab, WATCHERSETTINGS);
            }
        }
        public void AddSettingsTab(SettingsPage settings, string signal)
        {
            settingSignals[signal] = settings;
            subObjects.Add(settings);
            settings.Hide();
            if (activeSettings == null) SwitchTab(settings);
        }
        public void SwitchTab(SettingsPage settings)
        {
            activeSettings?.Hide();
            activeSettings = settings;
            activeSettings.Show();
        }
        public void OnSwitchSettingsTab(SettingsPage? page, SettingsPage? prevPage)
        {
            if (page == null) return;
            SoundID soundID = page == selectSettings ? SoundID.MENU_Checkbox_Uncheck : SoundID.MENU_Checkbox_Check;
            menu.PlaySound(soundID);
            page.SelectAndCreateBackButtons(prevPage, !menu.manager.menuesMouseMode);
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (settingSignals.ContainsKey(message))
            {
                SettingsPage settings = settingSignals[message];
                SettingsPage? prevSettings = activeSettings;
                if (prevSettings == settings) return;
                OnSwitchSettingsTab(settings, prevSettings);
                SwitchTab(settings);
            }
        }

        public class MSCSettingsPage : SettingsPage, CheckBox.IOwnCheckBox
        {
            public const string SAINOT = "SAINOT", PAINCATTHROWS = "PAINCATTHROWS", PAINCATEGG = "PAINCATEGG", DISABLEARTISTUN = "DISABLEARTISTUN", DISABLEMAUL = "DISABLEMAUL", PAINCATLIZARD = "PAINCATLIZARD", SAINTASENSIONTIMER = "SAINTASCENSIONTIMER";
            public SimpleButton? backButton;
            public MenuTabWrapper tabWrapper;
            public OpTextBox saintAscendDurationTimerTextBox;
            public MenuLabel saintAscendanceTimerLabel;
            public RestorableCheckbox blockMaulCheckBox, blockArtiStunCheckBox, sainotCheckBox, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox;
            public override string Name => "MSC Settings";
            public MSCSettingsPage(Menu.Menu menu, MenuObject owner, Vector2 spacing, string painCatName, float textSpacing = 300) : base(menu, owner)
            {
                tabWrapper = new(menu, this);
                Vector2 positioner = new(360, 380);
                blockMaulCheckBox = new(menu, this, this, positioner, textSpacing, Translate("Disable Mauling:"), DISABLEMAUL, false, Translate("Prevent Artificer and <PAINCATNAME> from mauling"));
                blockArtiStunCheckBox = new(menu, this, this, positioner - spacing, textSpacing, Translate("Disable Artificer Stun:"), DISABLEARTISTUN, false, Translate("Prevent Artificer from stunning other players"));
                sainotCheckBox = new(menu, this, this, positioner - spacing * 2, textSpacing, Translate("Sain't:"), SAINOT, false, Translate("Disable Saint ascendance ability, but allow it to throw spears"));
                saintAscendDurationTimerTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value), positioner - spacing * 3 + new Vector2(-7.5f, 0), 40)
                {
                    alignment = FLabelAlignment.Center,
                    description = Translate("How long Saint's ascendance ability lasts for. Default: 3s")
                };
                saintAscendDurationTimerTextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
                {
                    if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                    arena.arenaSaintAscendanceTimer = saintAscendDurationTimerTextBox.valueInt;
                };
                saintAscendanceTimerLabel = new(menu, this, Translate("Saint Ascendance Duration:"), saintAscendDurationTimerTextBox.pos + new Vector2(-textSpacing * 1.5f + 7.5f, 3), new(textSpacing, 20), false);
                saintAscendanceTimerLabel.label.alignment = FLabelAlignment.Left;
                new PatchedUIelementWrapper(tabWrapper, saintAscendDurationTimerTextBox);
                painCatEggCheckBox = new(menu, this, this, positioner - spacing * 4, 300, Translate("<PAINCATNAME> gets egg at 0 throw skill:"), PAINCATEGG, description: Translate("If <PAINCATNAME> spawns with 0 throw skill, also spawn with Eggzer0"));
                painCatThrowsCheckBox = new(menu, this, this, positioner - spacing * 5, 300, Translate("<PAINCATNAME> can always throw spears:"), PAINCATTHROWS, description: Translate("Always allow <PAINCATNAME> to throw spears, even if throw skill is 0"));
                painCatLizardCheckBox = new(menu, this, this, positioner - spacing * 6, 300, Translate("<PAINCATNAME> sometimes gets a friend:"), PAINCATLIZARD, description: Translate("Allow <PAINCATNAME> to rarely spawn with a little friend"));
                this.SafeAddSubobjects(tabWrapper, blockMaulCheckBox, blockArtiStunCheckBox, sainotCheckBox, saintAscendanceTimerLabel, painCatEggCheckBox, painCatThrowsCheckBox, painCatLizardCheckBox);
                string Translate(string s) => menu.LongTranslate(s).Replace("<PAINCATNAME>", painCatName);
            }
            public void SyncMenuObjectStatus(MenuObject obj)
            {
                if (obj is CheckBox checkBox)
                    checkBox.Checked = checkBox.Checked;
            }
            public override void SaveInterfaceOptions()
            {
                RainMeadow.rainMeadowOptions.BlockMaul.Value = blockMaulCheckBox.Checked;
                RainMeadow.rainMeadowOptions.BlockArtiStun.Value = blockArtiStunCheckBox.Checked;
                RainMeadow.rainMeadowOptions.ArenaSAINOT.Value = sainotCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatEgg.Value = painCatEggCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatThrows.Value = painCatThrowsCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatLizard.Value = painCatLizardCheckBox.Checked;
                RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value = saintAscendDurationTimerTextBox.valueInt;
            }
            public override void CallForSync()
            {
                foreach (MenuObject menuObj in subObjects)
                    SyncMenuObjectStatus(menuObj);
                if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                arena.arenaSaintAscendanceTimer = saintAscendDurationTimerTextBox.valueInt;
            }
            public override void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
            {
                if (backButton == null)
                {
                    backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(30, 30), new(80, 30));
                    AddObjects(backButton);
                    menu.MutualVerticalButtonBind(backButton, painCatLizardCheckBox);
                    menu.MutualVerticalButtonBind(blockMaulCheckBox, backButton); //loop
                }
                if (forceSelectedObject) menu.selectedObject = blockMaulCheckBox;
            }
            public override void Update()
            {
                base.Update();

                if (IsActuallyHidden) return; //lets not update this when hidden
                bool greyoutAll = SettingsDisabled;
                foreach (MenuObject obj in subObjects)
                {
                    if (obj != backButton && obj is ButtonTemplate btn)
                        btn.buttonBehav.greyedOut = greyoutAll;
                }
                if (RainMeadow.isArenaMode(out ArenaMode arena))
                {
                    saintAscendDurationTimerTextBox.greyedOut = greyoutAll;
                    saintAscendDurationTimerTextBox.held = saintAscendDurationTimerTextBox._KeyboardOn;
                    if (!saintAscendDurationTimerTextBox.held)
                        saintAscendDurationTimerTextBox.valueInt = arena.arenaSaintAscendanceTimer;
                }
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (IsActuallyHidden) return;
                saintAscendanceTimerLabel.label.color = saintAscendDurationTimerTextBox.rect.colorEdge;
            }
            public bool GetChecked(CheckBox box)
            {
                string id = box.IDString;
                if (RainMeadow.isArenaMode(out ArenaMode arena))
                {
                    if (id == DISABLEMAUL) return arena.disableMaul;
                    if (id == DISABLEARTISTUN) return arena.disableArtiStun;
                    if (id == SAINOT) return arena.sainot;
                    if (id == PAINCATEGG) return arena.painCatEgg;
                    if (id == PAINCATTHROWS) return arena.painCatThrows;
                    if (id == PAINCATLIZARD) return arena.painCatLizard;
                }
                return false;
            }
            public void SetChecked(CheckBox box, bool c)
            {
                if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                string id = box.IDString;
                if (id == DISABLEMAUL) arena.disableMaul = c; //owner can only edit it, its fine
                if (id == DISABLEARTISTUN) arena.disableArtiStun = c;
                if (id == SAINOT) arena.sainot = c;
                if (id == PAINCATEGG) arena.painCatEgg = c;
                if (id == PAINCATTHROWS) arena.painCatThrows = c;
                if (id == PAINCATLIZARD) arena.painCatLizard = c;
            }
        }
        public class WatcherSettingsPage : SettingsPage
        {
            public SimplerButton? backButton;
            public MenuTabWrapper tabWrapper;
            public MenuLabel watcherCamoLimitLabel, watcherRippleLevelLabel, weaverWatcherLabel, voidMasterLabel, amoebaDurationLabel, amoebaControlLabel;
            public OpTextBox watcherCamoLimitTextBox, watcherRippleLevelTextBox, amoebaLifespanTextBox;
            public OpCheckBox weaverWatcherCheckBox, voidMasterCheckbox, amoebaControlCheckbox;
            public override string Name => "Watcher Settings";
            public WatcherSettingsPage(Menu.Menu menu, MenuObject owner, Vector2 spacing, float textSpacing = 300) : base(menu, owner)
            {
                tabWrapper = new(menu, this);
                Vector2 positioner = new(360, 380);
                watcherCamoLimitTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.ArenaWatcherCamoTimer.Value), new(positioner.x - 7.5f, positioner.y), 40)
                {
                    alignment = FLabelAlignment.Center,
                    description = menu.Translate("How long Watcher's abilities last for. Default: 12s")
                };
                watcherCamoLimitTextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
                {
                    if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                    arena.watcherCamoTimer = watcherCamoLimitTextBox.valueInt;
                };
                new PatchedUIelementWrapper(tabWrapper, watcherCamoLimitTextBox);
                watcherCamoLimitLabel = new(menu, this, menu.Translate("Watcher Camo Duration:"), watcherCamoLimitTextBox.pos + new Vector2(-textSpacing * 1.5f + 7.5f, 3), new(textSpacing, 20), false);
                watcherCamoLimitLabel.label.alignment = FLabelAlignment.Left;


                watcherRippleLevelTextBox = new(new Configurable<int>(RainMeadow.rainMeadowOptions.ArenaWatcherRippleLevel.Value), positioner - spacing + new Vector2(-7.5f, 0), 40)
                {
                    alignment = FLabelAlignment.Center,
                    description = menu.Translate("Updates Watcher's ripple level. Ranges from 1 to 9. Default: 1")

                };
                watcherRippleLevelTextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
                {
                    if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                    arena.watcherRippleLevel = Mathf.Clamp(watcherRippleLevelTextBox.valueInt, 1, 9);
                };
                new PatchedUIelementWrapper(tabWrapper, watcherRippleLevelTextBox);
                watcherRippleLevelLabel = new(menu, this, menu.Translate("Watcher Ripple Level:"), watcherRippleLevelTextBox.pos + new Vector2(-textSpacing * 1.5f + 7.5f, 3), new(textSpacing, 20), false);
                watcherRippleLevelLabel.label.alignment = FLabelAlignment.Left;

                weaverWatcherCheckBox = new(new Configurable<bool>(RainMeadow.rainMeadowOptions.WeaverWatcher.Value), positioner - spacing * 2)
                {
                    colorEdge = RainWorld.GoldRGB * 1.5f
                };
                weaverWatcherCheckBox.OnChange += () => weaverWatcherCheckBox.description = weaverWatcherCheckBox.GetValueBool()? menu.Translate("Your watcher has synced weaver cosmetics") : menu.Translate("Your watcher has synced normal cosmetics");
                new PatchedUIelementWrapper(tabWrapper, weaverWatcherCheckBox);
                weaverWatcherLabel = new(menu, this, menu.Translate("Weaver Watcher:"), weaverWatcherCheckBox.pos + new Vector2(-textSpacing * 1.5f, 3), new(textSpacing, 20), false);
                weaverWatcherLabel.label.alignment = FLabelAlignment.Left;

                weaverWatcherCheckBox.Change(); //update desc


                // Voidmaster
                voidMasterCheckbox = new(RainMeadow.rainMeadowOptions.VoidMaster, positioner - spacing * 3)
                {
                    colorEdge = RainWorld.RippleColor * 1.5f
                };
                voidMasterCheckbox.OnChange += () =>
                {
                    if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                    arena.voidMasterEnabled = voidMasterCheckbox.GetValueBool();
                    voidMasterCheckbox.description = voidMasterCheckbox.GetValueBool() ? menu.Translate("Summon amoebas at the cost of your camo timer") : menu.Translate("Amoeba summoning is disabled lobby-wide");

                };
                new PatchedUIelementWrapper(tabWrapper, voidMasterCheckbox);
                voidMasterLabel = new(menu, this, menu.Translate("Voidkeeper:"), voidMasterCheckbox.pos + new Vector2(-textSpacing * 1.5f, 3), new(textSpacing, 20), false);
                voidMasterLabel.label.alignment = FLabelAlignment.Left;

                voidMasterCheckbox.Change(); //update desc


                //Amoeba duration
                amoebaLifespanTextBox = new(new Configurable<float>(RainMeadow.rainMeadowOptions.AmoebaDuration.Value), positioner - spacing * 4 + new Vector2(-7.5f, 0), 40)
                {
                    alignment = FLabelAlignment.Center,
                    description = menu.Translate("Amoeba duration time in seconds")

                };
                amoebaLifespanTextBox.OnValueUpdate += (UIconfig config, string value, string lastValue) =>
                {
                    if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                    arena.amoebaDuration = amoebaLifespanTextBox.valueInt;
                };
                new PatchedUIelementWrapper(tabWrapper, amoebaLifespanTextBox);
                amoebaDurationLabel = new(menu, this, menu.Translate("Voidkeeper Amoeba Duration:"), amoebaLifespanTextBox.pos + new Vector2(-textSpacing * 1.5f + 7.5f, 3), new(textSpacing, 20), false);
                amoebaDurationLabel.label.alignment = FLabelAlignment.Left;


                amoebaLifespanTextBox.Change();

                amoebaControlCheckbox = new(RainMeadow.rainMeadowOptions.AmoebaControl, positioner - spacing * 5);
                amoebaControlCheckbox.OnChange += () =>
                {
                    if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                    arena.amoebaControl = amoebaControlCheckbox.GetValueBool();
                    amoebaControlCheckbox.description = amoebaControlCheckbox.GetValueBool() ? menu.Translate("Amoeba's direction is influenced by pointing") : menu.Translate("Amoebas chase targets at-will");

                };
                new PatchedUIelementWrapper(tabWrapper, amoebaControlCheckbox);
                amoebaControlLabel = new(menu, this, menu.Translate("Void's Vengeance:"), amoebaControlCheckbox.pos + new Vector2(-textSpacing * 1.5f, 3), new(textSpacing, 20), false);
                amoebaControlLabel.label.alignment = FLabelAlignment.Left;

                amoebaControlCheckbox.Change();
                this.SafeAddSubobjects(tabWrapper, watcherCamoLimitLabel, watcherRippleLevelLabel, weaverWatcherLabel, voidMasterLabel, amoebaDurationLabel, amoebaControlLabel);

            }
            public override void SaveInterfaceOptions()
            {
                RainMeadow.rainMeadowOptions.ArenaWatcherCamoTimer.Value = watcherCamoLimitTextBox.valueInt;
                RainMeadow.rainMeadowOptions.ArenaWatcherRippleLevel.Value = watcherRippleLevelTextBox.valueInt;
                RainMeadow.rainMeadowOptions.WeaverWatcher.Value = weaverWatcherCheckBox.GetValueBool();
                RainMeadow.rainMeadowOptions.VoidMaster.Value = voidMasterCheckbox.GetValueBool();
                RainMeadow.rainMeadowOptions.AmoebaDuration.Value = amoebaLifespanTextBox.valueInt;
                RainMeadow.rainMeadowOptions.AmoebaControl.Value = amoebaControlCheckbox.GetValueBool();

            }
            public override void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
            {
                if (backButton == null)
                {
                    backButton = new(menu, this, menu.Translate("BACK"), new(30, 30), new(80, 30))
                    {
                        signalText = BACKTOSELECT,
                    };
                    AddObjects(backButton);
                    menu.TrySequentialMutualBind([backButton, weaverWatcherCheckBox.wrapper, watcherRippleLevelTextBox.wrapper, watcherCamoLimitTextBox.wrapper], bottomTop: true, loopLastIndex: true);
                }
                if (forceSelectedObject)
                    menu.selectedObject = watcherCamoLimitTextBox.wrapper;
            }
            public override void CallForSync()
            {
                if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;
                arena.watcherCamoTimer = watcherCamoLimitTextBox.valueInt;
                arena.watcherRippleLevel = watcherRippleLevelTextBox.valueInt;
                arena.arenaClientSettings.weaverTail = weaverWatcherCheckBox.GetValueBool();
                arena.voidMasterEnabled = voidMasterCheckbox.GetValueBool();
                arena.amoebaDuration = amoebaLifespanTextBox.valueInt;
                arena.amoebaControl = amoebaControlCheckbox.GetValueBool();
            }
            public override void Update()
            {
                base.Update();

                if (IsActuallyHidden) return;

                bool greyoutall = SettingsDisabled;
                if (!RainMeadow.isArenaMode(out ArenaMode arena)) return;

                watcherCamoLimitTextBox.greyedOut = greyoutall;
                watcherCamoLimitTextBox.held = watcherCamoLimitTextBox._KeyboardOn;
                if (!watcherCamoLimitTextBox.held)
                    watcherCamoLimitTextBox.valueInt = arena.watcherCamoTimer;

                watcherRippleLevelTextBox.greyedOut = greyoutall;
                watcherRippleLevelTextBox.held = watcherRippleLevelTextBox._KeyboardOn;
                if (!watcherRippleLevelTextBox.held)
                    watcherRippleLevelTextBox.valueInt = arena.watcherRippleLevel;

                arena.arenaClientSettings.weaverTail = weaverWatcherCheckBox.GetValueBool();

                voidMasterCheckbox.greyedOut = greyoutall;
                arena.voidMasterEnabled = voidMasterCheckbox.GetValueBool();
                amoebaControlCheckbox.greyedOut = !voidMasterCheckbox.GetValueBool() || greyoutall;
                arena.amoebaControl = amoebaControlCheckbox.GetValueBool();
                amoebaLifespanTextBox.greyedOut = !voidMasterCheckbox.GetValueBool() || greyoutall;
                amoebaLifespanTextBox.held = amoebaLifespanTextBox._KeyboardOn;
                if (!amoebaLifespanTextBox.held)
                    amoebaLifespanTextBox.valueFloat = arena.amoebaDuration;


            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (IsActuallyHidden) return;
                watcherCamoLimitLabel.label.color = watcherCamoLimitTextBox.rect.colorEdge;
                watcherRippleLevelLabel.label.color = watcherRippleLevelTextBox.rect.colorEdge;
                weaverWatcherLabel.label.color = weaverWatcherCheckBox.rect.colorEdge;
                voidMasterLabel.label.color = voidMasterCheckbox.rect.colorEdge;
                amoebaDurationLabel.label.color = amoebaLifespanTextBox.rect.colorEdge;
                amoebaControlLabel.label.color = amoebaControlCheckbox.rect.colorEdge;
            }
        }
        public class SelectSettingsPage : SettingsPage
        {
            public FLabel titleLabel;
            public FSprite titleDivider;
            public ButtonScroller scroller;
            public List<SettingsButton> SettingBtns => scroller.GetSpecificButtons<SettingsButton>();
            public override string Name => "Select Settings";
            public SelectSettingsPage(Menu.Menu menu, MenuObject owner, Dictionary<string, SettingsPage> allSettings) : base(menu, owner)
            {
                titleLabel = new(Custom.GetDisplayFont(), menu.Translate(Name), new())
                {
                    anchorY = 1
                };
                Container.AddChild(titleLabel);
                titleDivider = new("pixel")
                {
                    scaleX = 300,
                    scaleY = 2,
                    color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey)
                };
                Container.AddChild(titleDivider);
                scroller = new(menu, this, new(80, 420 - ButtonScroller.CalculateHeightBasedOnAmtOfButtons(8, 45, 0)), 8, 290, new(45, 0), sliderPosOffset: new(0, 0), sliderSizeYOffset: -40);
                scroller.CreateSideButtonLines();
                KeyValuePair<string, SettingsPage>[] array = [.. allSettings];
                for (int i = 0; i < array.Length; i++)
                {
                    KeyValuePair<string, SettingsPage> pair = array[i];
                    SettingsButton btn = new(menu, scroller, pair.Value, pair.Key, new(0, scroller.GetIdealYPosWithScroll(i)), new(290, 45));
                    if (i > 0)
                        btn.CreateTopDivider();
                    scroller.AddScrollObjects(btn);
                }
                this.SafeAddSubobjects(scroller);
            }
            public override void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
            {
                base.SelectAndCreateBackButtons(previousSettingPage, forceSelectedObject);
                if (forceSelectedObject && previousSettingPage != null)
                    menu.selectedObject = scroller.GetSpecificButtons<SettingsButton>().Find(x => x.settingsPage == previousSettingPage);
            }
            public override void RemoveSprites()
            {
                base.RemoveSprites();
                titleLabel.RemoveFromContainer();
                titleDivider.RemoveFromContainer();
            }
            public override void Update()
            {
                base.Update();
                if (IsActuallyHidden) return;
                List<SettingsButton> settingBtns = SettingBtns;
                scroller.scrollSlider.TryBind(settingBtns[Mathf.Min(Mathf.CeilToInt(scroller.DownScrollOffset), settingBtns.Count - 1)], right: true);
            }
            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                if (IsActuallyHidden) return;
                Vector2 screenPos = DrawPos(timeStacker);
                //tabContainer size: 450, 475;
                titleLabel.x = screenPos.x + 225; //450 * 0.5f
                titleLabel.y = screenPos.y + 465;
                titleDivider.x = titleLabel.x;
                titleDivider.y = titleLabel.y - titleLabel.textRect.height - 3;
            }
            public class SettingsButton : BigSimpleButton, ButtonScroller.IPartOfButtonScroller
            {
                public float Alpha { get; set; } = 1;
                public Vector2 Pos { get => pos; set => pos = value; }
                public Vector2 Size { get => size; set => size = value; }
                public float AlphaOfButtonAbove => owner is ButtonScroller scroller ? scroller.buttons.GetValueOrDefault(scroller.buttons.IndexOf(this) - 1)?.Alpha ?? 0 : 0;
                public FSprite? topDivSprite;
                public FSprite arrowSprite;
                public SettingsPage settingsPage;
                public SettingsButton(Menu.Menu menu, MenuObject owner, SettingsPage settingsPage, string signal, Vector2 pos, Vector2 size) : base(menu, owner, menu.Translate(settingsPage.Name), signal, pos, size, FLabelAlignment.Left, true)
                {
                    this.settingsPage = settingsPage;
                    roundedRect.RemoveSprites();
                    selectRect.RemoveSprites();
                    arrowSprite = new("Menu_Symbol_Arrow")
                    {
                        rotation = 90,
                        anchorX = 0.5f,
                        anchorY = 0.5f
                    };
                    Container.AddChild(arrowSprite);
                }
                public void CreateTopDivider()
                {
                    if (topDivSprite != null) return;

                    topDivSprite = new("pixel")
                    {
                        anchorX = 0,
                        scaleY = 2,
                        color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey),
                    };
                    Container.AddChild(topDivSprite);

                }
                public override void RemoveSprites()
                {
                    base.RemoveSprites();
                    topDivSprite?.RemoveFromContainer();
                    arrowSprite.RemoveFromContainer();
                }
                public override void GrafUpdate(float timeStacker)
                {
                    base.GrafUpdate(timeStacker);
                    Vector2 screenPos = DrawPos(timeStacker), screenSize = DrawSize(timeStacker);
                    arrowSprite.x = screenPos.x + size.x + 3;
                    arrowSprite.y = menuLabel.label.y;
                    arrowSprite.color = menuLabel.label.color;

                    //each end extend by 4
                    float desiredX = screenPos.x - 4, desiredScale = screenSize.x + 4;
                    if (topDivSprite != null)
                    {
                        topDivSprite.x = desiredX;
                        topDivSprite.y = screenPos.y + screenSize.y;
                        topDivSprite.scaleX = desiredScale;
                        topDivSprite.alpha = AlphaOfButtonAbove;
                    }

                }

            }
        }
        public abstract class SettingsPage(Menu.Menu menu, MenuObject owner) : Tab(menu, owner)
        {
            public bool SettingsDisabled => (menu as ArenaOnlineLobbyMenu)?.SettingsDisabled ?? true;
            public abstract string Name { get; }
            public virtual void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
            {
                if (forceSelectedObject)
                    menu.selectedObject = null;
            }
            public virtual void CallForSync()
            {

            }
            public virtual void SaveInterfaceOptions()
            {

            }
        }

    }
}


