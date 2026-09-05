using Menu;
using Menu.Remix;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using static RainMeadow.UI.Components.TabContainer;
using static Menu.Menu;
using System;
using HarmonyLib;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using Menu.Remix.MixedUI.ValueTypes;

namespace RainMeadow.UI.Components;
public class OnlineSettingEnumList : OnlineSettingUIconfig
{
    public string valueString => comboBox.value;
    public override int additionalPositionsTaken
        => comboBox.held
            ? Mathf.CeilToInt((comboBox._rectList?.size.y ?? 0)/(spacing + elementHeight))
            : 0;
    public OpComboBox2 comboBox => (OpComboBox2)uiConfig;

    public static List<ListItem> EnumToTranslatedItemList(Type enumType, Menu.Menu menu)
    {
        if (!enumType.IsExtEnum() && !enumType.IsEnum) // if we don't do this now, it might throw weird stuff after. I'd rather have a clear error.
            throw new ElementFormatException("enumType is neither Enum or ExtEnum!");

        return OpResourceSelector.GetEnumNames(null, enumType)
            .Select(li => { li.displayName = menu.Translate(li.displayName); return li; })
            .ToList();
    }
    public static List<string> EnumToTranslatedStrings(Type enumType)
    {
        if (!enumType.IsExtEnum() && !enumType.IsEnum) // if we don't do this now, it might throw weird stuff after. I'd rather have a clear error.
            throw new ElementFormatException("enumType is neither Enum or ExtEnum!");

        List<string> list = (enumType.IsEnum ? Enum.GetNames(enumType) : ExtEnumBase.GetNames(enumType)).ToList();
        list.Sort((x ,y) => ListItem.GetRealName(x).CompareTo(ListItem.GetRealName(y)));
        return list;
    }
    public OnlineSettingEnumList(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, tab) {}
    public OnlineSettingEnumList(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(menu,
            owner,
            tabWrapper,
            config,
            new OpComboBox2(new Configurable<string>(config.configurable.defaultValue), Vector2.zero, 120, [new(config.configurable.defaultValue, 0)]),
            tab)
    {
        InitWithNewList();
    }
    public void InitWithNewList() => InitWithNewList(data.AttributeType);
    public void InitWithNewList(Type enumType) => InitWithNewList(EnumToTranslatedItemList(enumType, menu));
    public void InitWithNewList(List<string> list) => InitWithNewList(list.ToArray());
    public void InitWithNewList(string[] list) => InitWithNewList(OpComboBox._ArrayToList(list));
    public void InitWithNewList(List<ListItem> list)
    {
        if (list == null || list.Count < 1)
            throw new ElementFormatException(comboBox, "The enum must contain at least one item", comboBox.Key);

        list.Sort(new Comparison<ListItem>(ListItem.Comparer));
        comboBox._itemList = list.ToArray();
        comboBox._ResetIndex();

        comboBox._rect?.container.RemoveAllChildren();
        comboBox._rect?.container.RemoveFromContainer();
        comboBox._rectList?.container.RemoveAllChildren();
        comboBox._rectList?.container.RemoveFromContainer();
        comboBox._rectScroll?.container.RemoveAllChildren();
        comboBox._rectScroll?.container.RemoveFromContainer();
        comboBox._glowFocus?.sprite.RemoveFromContainer();
        comboBox._lblText.RemoveFromContainer();
        comboBox._sprArrow?.RemoveFromContainer();
        comboBox._searchCursor?.RemoveFromContainer();
        comboBox._lblList.Do(x => x.RemoveFromContainer());

        comboBox._Initialize(data.configurable.defaultValue);
    }
    protected override void ShowSyncInUIConfig(bool grayedOut, object value)
        => ShowSyncInGenericUIConfig(comboBox, grayedOut, value);

    public override void GrafUpdate(float timeStacker)
    {
        if (color is not null) comboBox.colorEdge = (Color)color;
        base.GrafUpdate(timeStacker);
        label.label.color = comboBox._rect.colorEdge;

        comboBox._glowFocus?.sprite.isVisible = visible && !comboBox._glowFocus.isHidden;
        comboBox._glowFocus?.sprite.alpha *= currentAlpha;
        comboBox._lblText.isVisible = visible;
        comboBox._lblText.alpha = currentAlpha;
        comboBox._sprArrow?.isVisible = visible;
        comboBox._sprArrow?.alpha = currentAlpha;
        comboBox._searchCursor?.isVisible = visible;
        comboBox._searchCursor?.alpha *= currentAlpha;
        comboBox._lblList.Do(x =>
        {
            x.isVisible = visible;
            x.alpha = currentAlpha;
        });
        HandleRectAlpha(comboBox._rect);
        HandleRectAlpha(comboBox._rectList);
        HandleRectAlpha(comboBox._rectScroll);
    }
}
