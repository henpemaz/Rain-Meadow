using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using RainMeadow.UI.Interfaces;
using UnityEngine;
using static RainMeadow.UI.Components.TabContainer;

namespace RainMeadow.UI.Components;

public class TabContainer : RectangularMenuObject
{
    public class TabButton : OpSimpleButton
    {
        public bool Active => container.activeTab == myTab;
        public readonly Tab myTab;
        public TabContainer container;
        public TabButton(string name, Tab myTab, TabContainer container, MenuTabWrapper myTabWrapper, Vector2 pos, float ySize = 125) : base(pos, new(30, ySize))
        {
            this.container = container;
            wrapper = new PatchedUIelementWrapper(myTabWrapper, this);
            this.myTab = myTab;
            _rect.hiddenSide = DyeableRect.HiddenSide.Right;
            _rectH.hiddenSide = DyeableRect.HiddenSide.Right;
            _label.alignment = FLabelAlignment.Left;
            _label.rotation = -90f;
            _label.text = name;
            description = $"Click to open {name} tab";

            OnClick += _ => container.SwitchTab(myTab);
        }

        public override void Update()
        {
            soundClick = Active ? SoundID.MENU_Greyed_Out_Button_Clicked : SoundID.MENU_Button_Standard_Button_Pressed;
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
    public class TabButtonsContainer : PositionedMenuObject
    {
        public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, MaxOffset); }
        public int MaxOffset => Mathf.Max((registeredTabButtons.Count - 1), 0) / PerPage;
        public int PerPage => Mathf.Max((int)((container.size.y - 5) / (DefaultTabButtonYSize + 5)), 1);
        public bool PagesOn => registeredTabButtons.Count > PerPage;
        public float DefaultTabButtonYSize { get => tabButtonYSize; set => tabButtonYSize = Mathf.Max(value, LabelTest.GetWidth(LongestName) + 20); }
        public string LongestName => registeredTabButtons.Select(x => x.Item2).FirstOrDefault(s => s.Length == registeredTabButtons.Max(str => str.Item2 == null ? 0 : str.Item2.Length));
        public TabButtonsContainer(Menu.Menu menu, TabContainer container) : base(menu, container, new(-23, 0))
        {
            registeredTabButtons = [];
            activeTabButtons = [];
            this.container = container;
            tabWrapper = new(menu, this);
            subObjects.Add(tabWrapper);
        }
        public override void Update()
        {
            base.Update();
            if (topArrowButton != null)
            {
                topArrowButton.GetButtonBehavior.greyedOut = !(CurrentOffset > 0);
                TabButton? topTabBtn = activeTabButtons.First();
                topArrowButton.pos.y = topTabBtn != null ? topTabBtn.pos.y + topTabBtn.size.y + 10 : container.size.y;
            }
            if (bottomArrowButton != null)
            {
                bottomArrowButton.GetButtonBehavior.greyedOut = !(CurrentOffset < MaxOffset);
                TabButton? bottomTabBtn = activeTabButtons.Last();
                bottomArrowButton.pos.y = (bottomTabBtn != null ? bottomTabBtn.pos.y : 0) - 34;
            }
        }
        public void AddNewTabButton(string name, Tab tab)
        {
            registeredTabButtons.Add(new(tab, name));
            if (PagesOn)
                DefaultTabButtonYSize = (container.size.y - 5) / registeredTabButtons.Count;
            PopulatePages(CurrentOffset);
        }
        public void RemoveTabButton(Tab tab)
        {
            int index = registeredTabButtons.FindIndex(x => x.Item1 == tab);
            if (index < 0)
            {
                RainMeadow.Error("Unable to find specific tab");
                return;
            }
            int previousOffset = CurrentOffset;
            registeredTabButtons.RemoveAt(index);
            if (PagesOn) DefaultTabButtonYSize = (container.size.y - 5) / registeredTabButtons.Count;
            PopulatePages(CurrentOffset);
            if (tab == container.activeTab) container.SwitchTab(activeTabButtons.Last().myTab);
        }
        public void GoPrevPage()
        {
            if (CurrentOffset > 0)
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePages(CurrentOffset - 1);
                container.SwitchTab(activeTabButtons.Last().myTab);
            }
        }
        public void GoNextPage()
        {
            if (CurrentOffset < MaxOffset)
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePages(CurrentOffset + 1);
                container.SwitchTab(activeTabButtons.First().myTab);
            }
        }
        public void PopulatePages(int offset)
        {
            ClearVisibleTabButtons();
            CurrentOffset = offset;
            int num = CurrentOffset * PerPage;
            while (num < registeredTabButtons.Count && num < (CurrentOffset + 1) * PerPage)
            {
                float sizeY = DefaultTabButtonYSize, posY = container.size.y - (sizeY + 15) + (-(sizeY + 5) * (num % PerPage));
                TabButton tabButton = new(registeredTabButtons[num].Item2, registeredTabButtons[num].Item1, container, tabWrapper, new(0, posY), DefaultTabButtonYSize);
                activeTabButtons.Add(tabButton);
                num++;
            }
            if (PagesOn)
            {
                AddPageButtons();
                return;
            }
            RemovePageButtons();
        }
        public void ClearVisibleTabButtons()
        {
            for (int i = 0; i < activeTabButtons.Count; i++)
            {
                activeTabButtons[i].wrapper.tabWrapper.ClearMenuObject(activeTabButtons[i].wrapper);
                activeTabButtons[i].wrapper.tabWrapper.wrappers.Remove(activeTabButtons[i]);
                activeTabButtons[i].Hide();
                activeTabButtons[i].Unload();
            }
            activeTabButtons.Clear();
        }
        public void AddPageButtons()
        {
            if (topArrowButton == null)
            {
                topArrowButton = new(menu, this, "Menu_Symbol_Arrow", "TabButtons_MoveUp", new(-5, container.size.y));
                topArrowButton.OnClick += _ => GoPrevPage();
                subObjects.Add(topArrowButton);
            }
            if (bottomArrowButton == null)
            {
                bottomArrowButton = new(menu, this, "Menu_Symbol_Arrow", "TabButtons_MoveDown", new(-5, -24));
                bottomArrowButton.symbolSprite.rotation = 180f;
                bottomArrowButton.OnClick += _ => GoNextPage();
                subObjects.Add(bottomArrowButton);
            }
        }
        public void RemovePageButtons()
        {
            this.ClearMenuObject(topArrowButton);
            this.ClearMenuObject(bottomArrowButton);
        }

        private int currentOffset = 0;
        private float tabButtonYSize = 125;
        public MenuTabWrapper tabWrapper;
        public SimplerSymbolButton? topArrowButton, bottomArrowButton;
        public List<ValueTuple<Tab, string>> registeredTabButtons;
        public readonly List<TabButton> activeTabButtons;
        public TabContainer container;
    }
    public class Tab : PositionedMenuObject
    {
        public Tab(Menu.Menu menu, MenuObject owner) : base(menu, owner, Vector2.zero)
        {
            myContainer = new();
            (owner?.Container ?? menu.container).AddChild(myContainer);
            myTabWrapper = new(menu, this);
            subObjects.Add(myTabWrapper);
        }
        public bool IsHidden { get; private set; }
        public override void Update()
        {
            if (IsHidden)
            {
                UpdateHiddenObjects(this);
                return;
            }
            base.Update();
        }
        public override void GrafUpdate(float timeStacker)
        {
            if (IsHidden)
            {
                GrafUpdateHiddenObjects(this, timeStacker);
                return;
            }
            base.GrafUpdate(timeStacker);
        }
        public void Show()
        {
            if (!IsHidden) return;
            myContainer.isVisible = true;
            IsHidden = false;
            for (int i = 0; i < subObjects.Count; i++)
            {
                ShowObject(subObjects[i]);
            }
        }
        public void Hide()
        {
            if (IsHidden) return;
            myContainer.isVisible = false;
            IsHidden = true;
            for (int i = 0; i < subObjects.Count; i++)
            {
                HideObject(subObjects[i]);
            }
        }
        public void ShowObject(MenuObject? obj)
        {
            if (obj is SelectableMenuObject selectableObj && !obj.page.selectables.Contains(selectableObj)) obj.page.selectables.Add(selectableObj);
            if (obj is IPLEASEUPDATEME updatableObj) updatableObj.IsHidden = false;
            for (int i = 0; i < obj?.subObjects?.Count; i++)
            {
                ShowObject(obj.subObjects[i]);
            }
        }
        public void HideObject(MenuObject? obj)
        {
            if (obj != null) RecursiveRemoveSelectables(obj);
            if (obj is IPLEASEUPDATEME updatableObj) updatableObj.IsHidden = true;
        }
        public void UpdateHiddenObjects(MenuObject obj)
        {
            for (int i = 0; i < obj.subObjects.Count; i++)
            {
                MenuObject subObj = obj.subObjects[i];
                if (subObj is IPLEASEUPDATEME) subObj.Update(); //assuming you update all subobjects as well
                else UpdateHiddenObjects(subObj);
            }
        }
        public void GrafUpdateHiddenObjects(MenuObject obj, float timeStacker)
        {
            for (int i = 0; i < obj.subObjects.Count; i++)
            {
                MenuObject subObj = obj.subObjects[i];
                if (subObj is IPLEASEUPDATEME) subObj.GrafUpdate(timeStacker);
                else GrafUpdateHiddenObjects(subObj, timeStacker);

            }
        }
        public void AddObjects(params MenuObject[] objects)
        {
            this.SafeAddSubobjects(objects);
            if (IsHidden) Hide();
            else Show();
        }
        public MenuTabWrapper myTabWrapper;
    }

    public Tab? activeTab;
    public TabButtonsContainer tabButtonContainer;
    public RoundedRect background;
    public MenuTabWrapper tabWrapper;

    public TabContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        background = new(menu, this, new(0, 0), this.size, true)
        {
            fillAlpha = 0.3f
        };
        background.fillAlpha = 0.3f;
        tabWrapper = new(menu, this);
        tabButtonContainer = new(menu, this);
        subObjects.AddRange([background, tabWrapper, tabButtonContainer]);
    }
    /// <summary>
    /// Objects will not be called for Update/GrafUpdate if they are hidden
    /// </summary>
    public Tab AddTab(string name)
    {
        Tab tab = new(menu, this);
        subObjects.Add(tab);
        tabButtonContainer.AddNewTabButton(name, tab);
        tab.Hide();
        if (activeTab == null) SwitchTab(tab);
        return tab;
    }
    public void RemoveTab(params Tab[] tabsToRemove)
    {
        for (int i = 0; i < tabsToRemove.Length; i++)
        {
            tabButtonContainer.RemoveTabButton(tabsToRemove[i]);
            this.ClearMenuObject(tabsToRemove[i]);
        }
    }
    public void SwitchTab(Tab tab)
    {
        activeTab?.Hide();
        activeTab = tab;
        activeTab.Show();
    }
}
