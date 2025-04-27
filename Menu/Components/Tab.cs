using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class TabContainer : PositionedMenuObject
{
    public class TabButton : OpSimpleButton
    {
        bool Active => container.activeIndex == index;
        int index;
        TabContainer container;

        public TabButton(string name, TabContainer container, int tabIndex) : base(new(container.pos.x - 20, container.pos.y + container.size.y - 135 - 130 * tabIndex), new(30, 125))
        {
            this.container = container;
            index = tabIndex;
            _rect.hiddenSide = DyeableRect.HiddenSide.Right;
            _rectH.hiddenSide = DyeableRect.HiddenSide.Right;
            _label.alignment = FLabelAlignment.Left;
            _label.rotation = -90f;
            _label.text = name;

            OnClick += _ => container.SwitchTab(tabIndex);
        }

        public override void Update()
        {
            base.Update();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            float num = Active ? 1f : base.bumpBehav.AddSize;
            _label.x = (0f - num) * 4f + 15f;
            _label.y = 6f + num;
            _rect.addSize = new Vector2(8f, 4f) * num;
            _rect.pos.x = (0f - _rect.addSize.x) * 0.5f;
            _rectH.addSize = new Vector2(4f, -4f) * num;
            _rectH.pos.x = (0f - _rectH.addSize.x) * 0.5f;

            float num3 = MouseOver ? ((0.5f + 0.5f * base.bumpBehav.Sin(10f)) * num) : 0f;
            for (int i = 0; i < 8; i++)
                _rectH.sprites[i].alpha = Active ? 1f : num3;
        }
    }

    public List<List<MenuObject>> tabs;
    public List<TabButton> tabButtons;
    public int activeIndex = 0;
    public List<MenuObject> activeTab = [];
    public RoundedRect background;
    public Vector2 size;

    public TabContainer(SmartMenu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos)
    {
        this.size = size;
        tabs = [];
        tabButtons = [];

        background = new RoundedRect(menu, this, new(0, 0), size, true);
    }

    public void AddTab<O>(string name, List<O> objects) where O : MenuObject, IRestorableMenuObj
    {
        int index = tabs.Count;

        TabButton btn = new(name, this, index);
        if (menu is SmartMenu smartMenu)
            new UIelementWrapper(smartMenu.tabWrapper, btn);
        tabButtons.Add(btn);

        subObjects.AddRange(objects);
        tabs.Add([.. objects.Cast<MenuObject>()]);

        // idk why but if this isn't run on every single object that is added everything just breaks so don't exclude the first set of tab elements added
        for (int objIndex = 0; objIndex < tabs[index].Count; objIndex++)
        {
            MenuObject obj = tabs[index][objIndex];
            obj.RemoveSprites();
            if (obj is SelectableMenuObject selectableObj) obj.page.selectables.Remove(selectableObj);
        }

        if (index == 0) SwitchTab(0);
    }

    public void SwitchTab(int tabIndex)
    {
        for (int i = 0; i < activeTab.Count; i++)
        {
            MenuObject obj = activeTab[i];
            obj.RemoveSprites();
            if (obj is SelectableMenuObject selectableObj) obj.page.selectables.Remove(selectableObj);
        }

        activeIndex = tabIndex;
        activeTab = tabs[activeIndex];

        for (int i = 0; i < activeTab.Count; i++)
        {
            MenuObject obj = activeTab[i];
            if (obj is not IRestorableMenuObj restorableObj)
                throw new InvalidCastException("An object within a Tab did not implement IRestorableMenuObj");
            restorableObj.RestoreSprites();
            restorableObj.RestoreSelectables();
        }
    }
}