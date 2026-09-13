using Menu;
using Menu.Remix;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using System;
using HarmonyLib;
using RainMeadow.UI.Components.Configurables;
using RainMeadow.UI.Systems;

namespace RainMeadow.UI.Components;

public abstract class OnlineSlugcatSettingsBase : SettingsPage
{
    public static readonly Vector2 fullSize = new(450, 475);
    public static readonly Vector2 defaultBoxSize = new(450, 440);
    public Vector2 settingsBoxSize;
    public Vector2 offset;
    public float margin;
    public SimpleButton? backButton;
    public SimplerButton? resetButton;
    public MenuTabWrapper tabWrapper;
    public ScrollableContainer scrollableContainer;
    public ScrollableContainer.Scrollable scroller;
    protected List<OnlineSettingElement> elements;
    public float spacing;
    public float textSpacing;
    public bool wasHidden = true;
    public int lastVisibleElementCount = 0;

    private int position;

    public OnlineSettingTab? GetSettingTab(SlugcatStats.Name slugcatTab)
    {
        return elements.Find(x =>
            x is OnlineSettingTab tab
            && tab.data.name is null
            && tab.data.slugcatIcon == slugcatTab)
        as OnlineSettingTab;
    }
    public OnlineSettingTab? GetSettingTab(string tabName)
    {
        return elements.Find(x =>
            x is OnlineSettingTab tab
            && tab.data.name == tabName)
        as OnlineSettingTab;
    }
    public OnlineSettingConfigurable? GetSettingParameter(string paramName)
    {
        return elements.Find(x =>
            x is OnlineSettingConfigurable param
            && param.data.name == paramName)
        as OnlineSettingConfigurable;
    }
    public OnlineSettingConfigurable? GetSettingParameter(ConfigurableBase configurable)
    {
        return elements.Find(x =>
            x is OnlineSettingConfigurable param
            && param.data.configurable == configurable)
        as OnlineSettingConfigurable;
    }
    public OnlineSettingConfigurable? GetSettingParameter(string attributeName, Type attributeOwnerType)
    {
        return elements.Find(x =>
            x is OnlineSettingConfigurable param
            && param.data.attributeName == attributeName
            && param.data.attributeOwnerType == attributeOwnerType)
        as OnlineSettingConfigurable;
    }

    protected OnlineSlugcatSettingsBase(Menu.Menu menu, MenuObject owner, float spacing = 5f, float margin = 30f, float textSpacing = 300) : base(menu, owner)
    {
        offset = fullSize - defaultBoxSize;
        offset.x /= 2f;
        scrollableContainer = new(menu, this, offset, defaultBoxSize);
        scrollableContainer.uiMask.CamViewSizeOffset = (new Vector2(0, -2) + Vector2.up * 0.01f);
        scrollableContainer.uiMask.CamViewPosOffset = new(0, 22.5f);
        scroller = scrollableContainer.CreateAndAttachScrollable(1000);
        scroller.defaultSubObjectAnchorRelativeToScrollable = ScrollSystem.Anchor.TopLeft;

        tabWrapper = new(menu, scroller);

        elements = [];
        this.spacing = spacing;
        this.textSpacing = textSpacing;
        this.margin = margin;

        settingsBoxSize = defaultBoxSize - Vector2.right * margin * 2;
        this.SafeAddSubobjects(scrollableContainer);
        scroller.subObjects.Add(tabWrapper);
    }

    public void AddElement(OnlineSettingElement? el, int index = -1, bool updatePosition = true)
    {
        if (el is not null)
        {
            if (!elements.Contains(el))
            {
                if (index < 0 || index > elements.Count)
                    elements.Add(el);
                else
                    elements.Insert(index, el);
            }

            if (!scroller.subObjects.Contains(el))
            {
                scroller.subObjects.Add(el);
            }

            if (updatePosition)
            {
                UpdateElementsPosition();
            }

            el.HardSetPosition(el.WantedPosition);
        }
    }
    public void AddElements(params OnlineSettingElement?[] els)
    {
        els.Do(el => AddElement(el, -1, false));
        UpdateElementsPosition();
    }
    public void AddBackButton()
    {
        if (backButton is null)
        {
            backButton = new(menu, this, menu.Translate("BACK"), BACKTOSELECT, new(margin, 20), new(80, 30));
            scroller.subObjects.Add(backButton);
            scroller.ForceAnchor(backButton, ScrollSystem.Anchor.BottomLeft);
        }
    }
    public void AddResetButton()
    {
        if (resetButton is null)
        {
            resetButton = new(menu, this, menu.Translate("RESET"), new(settingsBoxSize.x - 40 - 20, 20), new(80, 30));
            resetButton.OnClick += (b) => ResetSettings();
            scroller.subObjects.Add(resetButton);
            scroller.ForceAnchor(resetButton, ScrollSystem.Anchor.BottomRight);
        }
    }

    public void ReorderTabAndElements()
    {
        List<OnlineSettingTab> tabs = [];
        List<OnlineSettingElement> els = [];
        foreach (var el in elements)
        {
            if (el is OnlineSettingTab tab)
                tabs.Add(tab);
            else
                els.Add(el);
        }

        List<OnlineSettingElement> newElementsList = [];
        foreach (var tab in tabs)
        {
            newElementsList.Add(tab);
            foreach (var el in els)
            {
                if (el.tab == tab) newElementsList.Add(el);
            }
        }

        foreach (var el in els)
        {
            if (el.tab is null) newElementsList.Add(el);
        }

        elements = newElementsList;
        UpdateElementsVisibility();
        UpdateElementsPosition();
    }
    public void UpdateElementsVisibility()
    {
        int visibleElementCount = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].tab is OnlineSettingTab tab)
            {
                if (tab.grayedOut && !elements[i].tabIndependant && !elements[i].isClient)
                    elements[i].grayedOut = true;
                if (!tab.visible || tab.folded)
                    elements[i].visible = false;
            }

            if (elements[i].visible)
            {
                elements[i].alpha = 1;
                visibleElementCount++;
            }
            else
            {
                elements[i].HardSetAlpha(0);
            }
        }
        if (lastVisibleElementCount != visibleElementCount)
        {
            lastVisibleElementCount = visibleElementCount;
            BindSettingsButtons(IsActuallyHidden);
        }
    }
    public void UpdateElementsPosition()
    {
        position = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].position = position;
            elements[i].offsetPos = -Vector2.up * offset.y;
            if (elements[i].visible)
            {
                position++;
                if (elements[i].additionalPositionsTaken > 0)
                    position += elements[i].additionalPositionsTaken;
            }

            if (elements[i].tab is OnlineSettingTab tab && tab.position > position)
            {
                RainMeadow.Debug($"Tab {tab.data.name} is after element <{i}>{elements[i]} ! Reordering...");
                ReorderTabAndElements();
                return;
            }
        }
    }
    public virtual void UpdateScrollerSize()
    {
        float? minY = null, maxY = null;
        for (int i = 0; i < elements.Count; i++)
        {
            if (minY is null || minY > elements[i].pos.y)
                minY = elements[i].pos.y;
            if (maxY is null || maxY < elements[i].pos.y)
                maxY = elements[i].pos.y;
        }

        if (maxY is not null && minY is not null)
        {
            float idealPos = Mathf.Max(
                scrollableContainer.size.y,
                offset.y + (float)maxY - (float)minY + 2 * (spacing + OnlineSettingElement.elementHeight)
            );

            // Ease does stop the menu from jumping when closing tabs
            scroller.size.y = Mathf.Lerp(scroller.size.y, idealPos, 0.1f);
            scrollableContainer.contentSystem.ContentSize = scroller.size.y;
        }
    }

    public void ResetSettings()
    {
        menu.PlaySound(SoundID.MENU_Button_Successfully_Assigned);

        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.ResetValueToDefault();
            }
        }
    }
    public void BindSettingsButtons(bool isHidden)
    {
        if (isHidden)
        {
            elements.Select(el => el.selectable).Do(sel =>
            {
                sel.RemoveBind(bottom:true, top:true);
            });
            backButton?.RemoveBind(right:true, top:true, bottom:true);
            resetButton?.RemoveBind(left:true, top:true, bottom:true);
        }
        else
        {
            List<MenuObject> visibleElements = elements.FindAll(x => x.visible).Select(el => el.selectable).ToList();

            menu.TryMutualBind(resetButton, visibleElements.FirstOrDefault(), bottomTop:true);
            menu.TryMutualBind(visibleElements.LastOrDefault(), resetButton, bottomTop:true);

            if (backButton is not null)
                visibleElements.Insert(0, backButton);
            else if (resetButton is not null)
                visibleElements.Insert(0, resetButton);

            menu.TrySequentialMutualBind(visibleElements, bottomTop: true, loopLastIndex: true, reverseList:true);

            if (backButton is not null && resetButton is not null)
                menu.MutualHorizontalButtonBind(backButton, resetButton);
        }
    }

    public override void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
    {
        AddBackButton();
        AddResetButton();

        BindSettingsButtons(IsActuallyHidden);

        if (forceSelectedObject)
            menu.selectedObject = elements.FirstOrDefault()?.selectable ?? backButton;
    }
    public override void Update()
    {
        base.Update();

        if (wasHidden != IsActuallyHidden)
        {
            wasHidden = IsActuallyHidden;
            BindSettingsButtons(IsActuallyHidden);
        }

        if (IsActuallyHidden) return;

        bool greyoutNonClient = SettingsDisabled;
        bool greyoutAll = (OnlineManager.lobby?.gameMode as ArenaOnlineGameMode)?.initiateLobbyCountdown ?? true;

        foreach (MenuObject obj in subObjects)
        {
            if (obj != backButton && obj is ButtonTemplate btn)
                btn.buttonBehav.greyedOut = greyoutNonClient;
        }
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].visible = !IsActuallyHidden;
            elements[i].grayedOut = elements[i].isClient
                ? greyoutAll
                : greyoutNonClient;
        }

        UpdateElementsVisibility();
        UpdateElementsPosition();
        UpdateScrollerSize();
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (IsActuallyHidden)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                elements[i].pos = elements[i].targetPos + Vector2.up * 5f;
                elements[i].HardSetAlpha(0);
            }
        }
    }

    public override void SaveInterfaceOptions()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.SaveOption();
            }
        }
    }
    public override void SaveInterfaceClientOptions()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.SaveOption(true);
            }
        }
    }
    public override void CallForSync()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] is OnlineSettingConfigurable param)
            {
                param.SyncValueToAttribute();
            }
        }
    }
}
public abstract class OnlineSlugcatSettings<TSelf> : OnlineSlugcatSettingsBase where TSelf : class
{
    protected static List<OnlineSettingConfigurable.SettingsConfigData> onlineConfigurables = [];
    protected static List<OnlineSettingTab.SettingsTabData> onlineConfigurableTabs = [];

    public static void AddSlugcatSettingsTab(OnlineSettingTab.SettingsTabData tab)
    {
        if (onlineConfigurableTabs.Exists(x => x == tab))
        {
            RainMeadow.Error($"Could not add online configurable tab {tab.name ?? tab.slugcatIcon?.value} : {tab.name ?? tab.slugcatIcon?.value} is already in the page !");
            return;
        }
        onlineConfigurableTabs.Add(tab);
    }
    public static void AddSlugcatSettingsConfigurable(OnlineSettingConfigurable.SettingsConfigData config)
    {
        if (string.IsNullOrWhiteSpace(config.attributeName))
        {
            RainMeadow.Warn($"Adding configurable {config.name} with nowhere to save the value for the lobby !");
        }
        else
        {
            if (onlineConfigurables.Exists(x => x.attributeName == config.attributeName && x.attributeOwnerType == config.attributeOwnerType))
            {
                RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name}.{config.attributeName} is already in the page !");
                return;
            }
            if (!OnlineSettingConfigurable.SettingsConfigData.GetAttributeOwnerDict.ContainsKey(config.attributeOwnerType))
            {
                RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name} is not registered and has no GET function !");
                return;
            }
            if (config.attributeOwnerType.GetField(config.attributeName) is null)
            {
                RainMeadow.Error($"Could not add online configurable {config.name} : {config.attributeOwnerType.Name}.{config.attributeName} doesn't exist or is not an attribute !");
                return;
            }
        }

        if (config.slugcatTab is not null
            && !onlineConfigurableTabs.Exists(x => x.name is null && x.slugcatIcon == config.slugcatTab))
        {
            AddSlugcatSettingsTab(new(config.slugcatTab));
        }

        if (config.tabName is not null && !onlineConfigurableTabs.Exists(x => x.name == config.tabName))
        {
            AddSlugcatSettingsTab(new(config.tabName, Color.gray));
        }

        onlineConfigurables.Add(config);
    }

    private static List<OnlineSettingConfigurable.SettingsConfigData> GetAllConfigurablesFromTab(OnlineSettingTab.SettingsTabData? tab = null)
    {
        if (tab is OnlineSettingTab.SettingsTabData onlineConfigurableTab)
        {
            if (onlineConfigurableTab.name is null)
            {
                return onlineConfigurables.FindAll(x => x.slugcatTab == onlineConfigurableTab.slugcatIcon);
            }
            else
            {
                return onlineConfigurables.FindAll(x => x.tabName == onlineConfigurableTab.name);
            }
        }
        return onlineConfigurables.FindAll(x => x.tabName is null && x.slugcatTab is null);
    }
    private OnlineSettingTab GetElementFromConfig(OnlineSettingTab.SettingsTabData tab)
    {
        return new OnlineSettingTab(menu, this, tab);
    }
    private OnlineSettingConfigurable? GetElementFromConfig(OnlineSettingConfigurable.SettingsConfigData configurable, OnlineSettingTab? tab = null)
    {
        if (configurable.AttributeType == typeof(int))
        {
            return new OnlineSettingIntValue(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType == typeof(float))
        {
            return new OnlineSettingFloatValue(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType == typeof(string))
        {
            return new OnlineSettingStringValue(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType == typeof(bool))
        {
            return new OnlineSettingCheckBox(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType == typeof(KeyCode))
        {
            return new OnlineSettingKeycode(menu, this, configurable, tab);
        }
        else if (configurable.AttributeType.IsEnum || configurable.AttributeType.IsExtEnum())
        {
            return new OnlineSettingEnumList(menu, this, configurable, tab);
        }
        RainMeadow.Error($"Error trying to find UI element for [{configurable.name} : {configurable.attributeOwnerType}.{configurable.attributeName}] : type {configurable.configurable.settingType} is not handled !");
        return null;
    }

    protected OnlineSlugcatSettings(Menu.Menu menu, MenuObject owner, float spacing = 5f, float margin = 30f, float textSpacing = 300)
         : base(menu, owner, spacing, margin, textSpacing)
    {
        foreach (var tab in onlineConfigurableTabs)
        {
            OnlineSettingTab tabElement = GetElementFromConfig(tab);
            elements.Add(tabElement);
            GetAllConfigurablesFromTab(tab).Do(config =>
            {
                if (GetElementFromConfig(config, tabElement) is OnlineSettingConfigurable param)
                {
                    elements.Add(param);
                }
                else
                {
                    RainMeadow.Error($"Error trying to create UI element for [{config.name} : {config.attributeOwnerType}.{config.attributeName}], it will not be added !");
                }
            });
        }
        GetAllConfigurablesFromTab().Do(config =>
        {
            if (GetElementFromConfig(config) is OnlineSettingConfigurable param)
            {
                elements.Add(param);
            }
            else
            {
                RainMeadow.Error($"Error trying to create UI element for [{config.name} : {config.attributeOwnerType}.{config.attributeName}], it will not be added !");
            }
        });

        UpdateElementsPosition();
        for (int i = 0; i < elements.Count; i++)
        {
            elements[i].HardSetPosition(elements[i].WantedPosition);
        }
        scroller.subObjects.AddRange([.. elements]);
    }
}