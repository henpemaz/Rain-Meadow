using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI.Components.Patched;
using RainMeadow.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            description = Menu.Translate("Click to open <TABNAME> tab").Replace("<TABNAME>", name);

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
            else DefaultTabButtonYSize = 125;
            PopulatePages(CurrentOffset);
            if (tab == container.activeTab) 
                container.SwitchTab(activeTabButtons.Last().myTab);
            else container.activeTab?.BindAllSelectables();
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
            TabButton[] oldTabButtons = [..activeTabButtons];
            ClearVisibleTabButtons();
            CurrentOffset = offset;
            int num = CurrentOffset * PerPage, max = Mathf.Min(registeredTabButtons.Count - num, PerPage);
            for (int i = 0; i < max; i++)
            {
                ValueTuple<Tab, string> registeredTab = registeredTabButtons[i + num];
                float sizeY = DefaultTabButtonYSize, posY = container.size.y - (sizeY + 15) + (-(sizeY + 5) * i);
                TabButton tabButton = new(registeredTab.Item2, registeredTab.Item1, container, tabWrapper, new(0, posY), DefaultTabButtonYSize);
                activeTabButtons.Add(tabButton);
                TabButton? prevBtn = activeTabButtons.GetValueOrDefault(i - 1);
                menu.TryMutualBind(tabButton.wrapper, prevBtn?.wrapper, bottomTop: true);
            }
            if (PagesOn)
                AddPageButtons();
            else RemovePageButtons();
            container.UpdateTabButtonSelectables(this, oldTabButtons);

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
            menu.MutualVerticalButtonBind(activeTabButtons.First().wrapper, topArrowButton);
            menu.MutualVerticalButtonBind(bottomArrowButton, activeTabButtons.Last().wrapper);
        }
        public void RemovePageButtons()
        {
            this.ClearMenuObject(ref topArrowButton);
            this.ClearMenuObject(ref bottomArrowButton);
        }

        private int currentOffset = 0;
        private float tabButtonYSize = 125;
        public MenuTabWrapper tabWrapper;
        public SimplerSymbolButton? topArrowButton, bottomArrowButton;
        public List<ValueTuple<Tab, string>> registeredTabButtons;
        public readonly List<TabButton> activeTabButtons;
        public TabContainer container;
    }
    public class Tab : PositionedMenuObject, IPLEASEUPDATEME //yes for nested tab reasons
    {
        public MenuTabWrapper myTabWrapper;
        public event Action<bool>? BindSelectables;
        public Tab(Menu.Menu menu, MenuObject owner) : base(menu, owner, Vector2.zero)
        {
            myContainer = new();
            (owner?.Container ?? menu.container).AddChild(myContainer);
            myTabWrapper = new(menu, this);
            subObjects.Add(myTabWrapper);
        }
        public bool IsActuallyHidden => IsOwnHidden || IsHidden;
        public bool IsOwnHidden { get; private set; } //sees if itself is hidden
        public bool IsHidden { get; set; } //see if a parent tab forces it to be hidden
        public override void Update()
        {
            if (IsActuallyHidden)
            {
                UpdateHiddenObjects(this);
                return;
            }
            base.Update();
        }
        public override void GrafUpdate(float timeStacker)
        {
            if (IsActuallyHidden)
            {
                GrafUpdateHiddenObjects(this, timeStacker);
                return;
            }
            base.GrafUpdate(timeStacker);
        }
        public void Show()
        {
            myContainer.isVisible = true;
            IsOwnHidden = false;
            for (int i = 0; i < subObjects.Count; i++)
                ShowObject(subObjects[i]);
            BindMySelectables();
        }
        public void Hide()
        {
            myContainer.isVisible = false;
            IsOwnHidden = true;
            RecursiveRemoveSelectables(this);
            for (int i = 0; i < subObjects.Count; i++)
                HideObject(subObjects[i]);
            BindMySelectables();
        }
        public void ShowObject(MenuObject? obj)
        {
            if (obj == null) return;
            if (obj is SelectableMenuObject selectableObj && !obj.page.selectables.Contains(selectableObj)) obj.page.selectables.Add(selectableObj);
            if (obj is IPLEASEUPDATEME updatableObj) updatableObj.IsHidden = false;
            if (obj is Tab tab)
            {
                if (!tab.IsOwnHidden) //we got a nested tab that isnt hidden itself, lets not force add their subobjects, tell tab to readd their selectables on their own
                    tab.Show();
                return;
            }
            for (int i = 0; i < obj.subObjects.Count; i++)
            {
                MenuObject? subObj = obj.subObjects[i];
                ShowObject(subObj);
            }
        }

        /// <summary>
        /// Remember to call RecursiveRemoveSelectables because this doesnt remove selectables
        /// </summary>
        /// <param name="obj"></param>
        public void HideObject(MenuObject? obj)
        {
            //find all subobjects and nested to hide any IPLEASEUPDATEMES
            //dont need to call nested tab.Hide(), we removed all selectables and made this tab's container invisible.
            //so we are telling nested tab that it is being hidden from this tab, not by itself.
            if (obj == null) return;
            if (obj is IPLEASEUPDATEME updatableObj) updatableObj.IsHidden = true;
            if (obj is Tab tab) tab.BindMySelectables();
            for (int i = 0; i < obj.subObjects.Count; i++)
            {
                MenuObject? subObj = obj.subObjects[i];
                HideObject(subObj);
            }
        }
        public void UpdateHiddenObjects(MenuObject obj)
        {
            for (int i = 0; i < obj.subObjects.Count; i++)
            {
                MenuObject subObj = obj.subObjects[i];
                if (subObj is IPLEASEUPDATEME) subObj.Update(); //assuming you adjusted for isHidden as well
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
            for (int i = 0; i < objects.Length; i++)
            {
                MenuObject? obj = objects[i];
                if (obj == null || subObjects.Contains(obj)) continue;
                if (IsActuallyHidden) //dont force show, pretty sure default is show
                {
                    HideObject(obj);
                    RecursiveRemoveSelectables(obj);
                }
                subObjects.Add(obj);
            }
        }

        /// <summary>
        /// When you want to update all selectables in this tab, including all possible nested tabs in it.
        /// </summary>
        public void BindAllSelectables()
        {
            for (int i = 0; i < subObjects.Count; i++)
                BindObjectSelectables(subObjects[i]);
            BindMySelectables();
        }
        public void BindObjectSelectables(MenuObject? obj)
        {
            if (obj == null) return;
            if (obj is Tab tab)
            {
                tab.BindAllSelectables();
                return;
            }           
            for (int i = 0; i < obj.subObjects.Count; i++)
            {
                MenuObject subObj = obj.subObjects[i];
                BindObjectSelectables(subObj);
            }
        }
        public virtual void BindMySelectables()
        {
            BindSelectables?.Invoke(IsActuallyHidden);
        }
    }

    public Tab? activeTab;
    public TabButtonsContainer tabButtonContainer;
    public event Action<TabButtonsContainer, TabButton[]>? OnTabButtonsCreated;
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
    public void UpdateTabButtonSelectables(TabButtonsContainer tabBtnContainer, TabButton[] oldTabButtons)
    {
        OnTabButtonsCreated?.Invoke(tabBtnContainer, oldTabButtons);

    }
    /// <summary>
    /// Objects will not be called for Update/GrafUpdate if they are hidden
    /// </summary>
    public Tab AddTab(string name)
    {
        Tab tab = new(menu, this);
        subObjects.Add(tab);
        tabButtonContainer.AddNewTabButton(name, tab);
        /*tab.Hide();
        if (activeTab == null) SwitchTab(tab);
        else activeTab.BindAllSelectables();*/
        if (activeTab != null) tab.Hide();
        else activeTab = tab;
        activeTab.BindAllSelectables(); //update all binds since all tab buttons are recreated
        return tab;
    }
    /// <summary>
    /// Use this if you want to set UpdateSelectables first. Just make a new Tab and call this method
    /// </summary>
    public void AddTab(Tab tab, string name)
    {
        subObjects.Add(tab);
        tabButtonContainer.AddNewTabButton(name, tab);
        if (activeTab != null) tab.Hide();
        else activeTab = tab;
        activeTab.BindAllSelectables(); //update all binds since all tab buttons are recreated
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
