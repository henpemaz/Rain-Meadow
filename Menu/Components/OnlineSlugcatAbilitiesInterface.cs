using Menu;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RainMeadow.UI.Components.TabContainer;

namespace RainMeadow.UI.Components
{
    public class OnlineSlugcatAbilitiesInterface : PositionedMenuObject
    {
        public const string WATCHERSETTINGS = "WATCHERSETTINGS", MSCSETTINGS = "MSCSETTINGS", VANILLASETTINGS = "VANILLASETTINGS", BACKTOSELECT = "BACKTOSELECTSETTINGS";
        public SettingsPage? activeSettings;
        public Dictionary<string, SettingsPage> settingSignals = [];
        public MSCSlugcatSettings? mscSettingsTab;
        public WatcherSlugcatSetting? watcherSettingsTab;
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
        public static void ShowSyncInRemixCheckbox(OpCheckBox config, bool greyout, bool tosync)
        {
            config.greyedOut = greyout;
            if (!config.held)
                config.SetValueBool(tosync);
        }
        public static void ShowSyncInTextbox(OpTextBox textbox, bool greyout, object obj)
        {

            textbox.greyedOut = greyout;
            textbox.held = textbox._KeyboardOn;

            if (textbox.held) return;

            if (textbox.accept == OpTextBox.Accept.Int)
                textbox.valueInt = (int)obj;
            else if (textbox.accept == OpTextBox.Accept.Float)
                textbox.valueFloat = (float)obj;
            else textbox.value = (string)obj;
        }
        public static void ShowSyncInGenericUIConfig(UIconfig uiConfig, bool greyout, object obj)
        {
            uiConfig.greyedOut = greyout;
            if (!uiConfig.held)
                uiConfig.value = obj.ToString();
        }
        public void SaveAllInterfaceOptions(bool isOwner)
        {
            foreach (SettingsPage settings in settingSignals.Values)
            {
                if (isOwner)
                    settings.SaveInterfaceOptions();
                else settings.SaveInterfaceClientOptions();
            }
        }
        public void CallForSync() //call this after ctor if needed for sync at start
        {
            // dusty says this does something, just trust them future Timbits
            foreach (SettingsPage settings in settingSignals.Values)
                settings.CallForSync();
        }
        public void AddAllSettings(string paincatName)
        {
            AddSettingsTab(new VanillaSetting(menu, this), VANILLASETTINGS);
            if (ModManager.MSC)
            {
                mscSettingsTab = new(menu, this, paincatName);
                AddSettingsTab(mscSettingsTab, MSCSETTINGS);
            }
            if (ModManager.Watcher)
            {
                watcherSettingsTab = new (menu, this);
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
            public virtual void SaveInterfaceClientOptions()
            {

            }
        }

    }
}
