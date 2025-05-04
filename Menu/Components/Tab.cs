using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class TabContainer : RectangularMenuObject
{
    public class TabButton : OpSimpleButton
    {
        bool Active => container.activeIndex == index;
        public readonly int index;
        TabContainer container;
        public TabButton(string name, TabContainer container, MenuTabWrapper myTabWrapper, Vector2 pos, int tabIndex, float ySize = 125) : base(pos, new(30, ySize))
        {
            this.container = container;
            wrapper = new(myTabWrapper, this);
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
            soundClick = Active? SoundID.MENU_Greyed_Out_Button_Clicked : SoundID.MENU_Button_Standard_Button_Pressed;
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
        public string LongestName => registeredTabButtons.Find(x => x.Length == registeredTabButtons.Max(str => str == null ? 0 : str.Length));
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
                topArrowButton.pos.y = topTabBtn != null? topTabBtn.pos.y + topTabBtn.size.y + 10 : container.size.y;
            }
            if (bottomArrowButton != null)
            {
                bottomArrowButton.GetButtonBehavior.greyedOut = !(CurrentOffset < MaxOffset);
                TabButton? bottomTabBtn = activeTabButtons.Last();
                bottomArrowButton.pos.y = (bottomTabBtn != null? bottomTabBtn.pos.y : 0) - 34;
            }
        }
        public void AddNewTabButton(string name)
        {
            registeredTabButtons.Add(name);
            if (PagesOn)
            {
                DefaultTabButtonYSize = (container.size.y - 5) / registeredTabButtons.Count;
            }
            PopulatePages(CurrentOffset);
        }
        public void GoPrevPage()
        {
            if (CurrentOffset > 0)
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePages(CurrentOffset - 1);
                container.SwitchTab(activeTabButtons.Last().index);
            }
        }
        public void GoNextPage()
        {
            if (CurrentOffset < MaxOffset)
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                PopulatePages(CurrentOffset + 1);
                container.SwitchTab(activeTabButtons.First().index);
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
                TabButton tabButton = new(registeredTabButtons[num], container, tabWrapper, new(0, posY), num, DefaultTabButtonYSize);
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
            for(int i = 0; i < activeTabButtons.Count; i++)
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
                topArrowButton.OnClick += (_) =>
                {
                    GoPrevPage();
                };
                subObjects.Add(topArrowButton);
            }
            if (bottomArrowButton == null)
            {
                bottomArrowButton = new(menu, this, "Menu_Symbol_Arrow", "TabButtons_MoveDown", new(-5, -24));
                bottomArrowButton.symbolSprite.rotation = 180f;
                bottomArrowButton.OnClick += (_) =>
                {
                    GoNextPage();
                };
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
        public List<string> registeredTabButtons;
        private List<TabButton> activeTabButtons;
        public TabContainer container;
    }
    public class Tab(Menu.Menu menu, MenuObject owner) : PositionedMenuObject(menu, owner, Vector2.zero)
    {
        public bool IsHidden { get; private set; }
        public override void Update()
        {
            if (IsHidden)
            {
                return;
            }
            base.Update();
        }
        public override void GrafUpdate(float timeStacker)
        {
            if (IsHidden)
            {
                return;
            }
            base.GrafUpdate(timeStacker);
        }
        public void Show()
        {
            IsHidden = false;
            for (int i = 0; i < subObjects.Count; i++)
            {
                ShowObject(subObjects[i]);
            }
        }
        public void Hide()
        {
            IsHidden = true;
            for (int i = 0; i < subObjects.Count; i++)
            {
                HideObject(subObjects[i]);
            }
        }
        public void ShowObject(MenuObject? obj)
        {
            if (obj is IRestorableMenuObject restorableObj)
            {
                restorableObj.RestoreSprites();
                restorableObj.RestoreSelectables();
            }
            for (int i = 0; i < obj?.subObjects?.Count; i++)
            {
                ShowObject(obj.subObjects[i]);
            }
        }
        public void HideObject(MenuObject? obj)
        {
            if (obj != null)
            {
                obj.RemoveSprites();
                RecursiveRemoveSelectables(obj);
            }
        }
    }

    public int activeIndex = 0;
    public Tab? activeTab;
    public List<Tab> tabs;
    public TabButtonsContainer tabButtonContainer;
    public RoundedRect background;
    public MenuTabWrapper tabWrapper;

    public TabContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        tabs = [];
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
    /// Elements added MUST implement IRestorableMenuObjects, else your menu objects that are not restorable will be invisible forever.
    /// Subobjects will be turned invisible as well, so make sure they are restorable or called explicitly
    /// </summary>
    public void AddTab(string name, List<MenuObject> objects)
    {
        int index = tabs.Count;
        tabButtonContainer.AddNewTabButton(name);
        Tab tab = new(menu, this);
        subObjects.Add(tab);
        tab.subObjects.AddRange(objects);
        tabs.Add(tab);

        tabs[index].Hide();
        // idk why but if this isn't run on every single object that is added everything just breaks so don't exclude the first set of tab elements added
        if (index == 0)
        {
            SwitchTab(0);
        }
    }
    public void SwitchTab(int tabIndex)
    {
        activeTab?.Hide();
        activeIndex = tabIndex;
        activeTab = tabs[activeIndex];
        activeTab.Show();
    }

}
