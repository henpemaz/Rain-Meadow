// here be dragons
// HACK this is all to be replaced by a commit with a 2000 line diff with the message "improved bad code"
// maybe make this more generic and have lobby select menu handle lobby card stuff or smth idk figure it out later Timbits

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace RainMeadow;
public class LobbyCardsList : RectangularMenuObject, Slider.ISliderOwner
{
    /// <summary>
    /// A SimplerButton containing and displaying LobbyInfo.
    /// </summary>
    public class LobbyCard : SimplerButton
    {
        /// <summary>
        /// The LobbyInfo of a LobbyCard. Used for displaying lobby info on the card and joining lobby on click.
        /// </summary>
        public LobbyInfo lobbyInfo;
        /// <summary>
        /// A float between 1 and 0, 1 meaning the card is not faded and 0 meaning the card is fully faded
        /// </summary>
        public float fade;
        public override bool CurrentlySelectableMouse
        {
            get
            {
                if (fade < 1) return false;
                return base.CurrentlySelectableMouse;
            }
        }
        public override bool CurrentlySelectableNonMouse
        {
            get
            {
                if (fade < 1) return false;
                return base.CurrentlySelectableNonMouse;
            }
        }

        public LobbyCard(Menu.Menu menu, MenuObject owner, LobbyInfo lobbyInfo) : base(menu, owner, "", new Vector2(0, 0), new Vector2(300f, 60f), Utils.Translate("Click to join") + " " + lobbyInfo.name)
        {
            this.fade = 1f;
            this.lobbyInfo = lobbyInfo;

            this.menuLabel.RemoveSprites();
            this.RemoveSubObject(menuLabel);
            this.menuLabel = new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.name, new Vector2(5f, 30f), new(10f, 50f), true);
            subObjects.Add(menuLabel);

            if (lobbyInfo.hasPassword) subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, Utils.Translate("Private"), new(256, 20), new(10, 50), false));
            subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, $"{lobbyInfo.maxPlayerCount} {Utils.Translate("max")}", new(256, 5), new(10, 50), false));
            subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, Utils.Translate(lobbyInfo.mode), new(5, 20), new(10, 50), false));

            var playerWord = lobbyInfo.playerCount == 1 ? Utils.Translate("player") : Utils.Translate("players");
            subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.playerCount + " " + playerWord, new(5, 5), new(10, 50), false));

            OnClick += (obj) => (menu as LobbySelectMenu).Play(lobbyInfo);
        }

        public override void Update()
        {
            base.Update();

            if (owner is not LobbyCardsList list) return;

            fade = list.PercentageOverYBound(pos.y);

            for (int i = 0; i < subObjects.Count; i++)
            {
                if (subObjects[i] is not MenuLabel label) continue;
                label.label.alpha = fade;
            }

            for (int i = 0; i < roundedRect.sprites.Length; i++)
            {
                this.roundedRect.sprites[i].alpha = fade;
                this.roundedRect.fillAlpha = fade / 2;
            }
        }
    }

    /// <summary>
    /// A class representing the filter settings and ordering of lobby cards within the lobby select menu
    /// </summary>
    public class LobbyCardsFilter
    {
        public bool enabled;
        public string lobbyName;
        public string sortingOrder;
        public string gameMode;
        public string requiredMods;
        public bool publicLobby;

        public enum SortingOrder
        {
            [Description("Sort by Ping")]
            Ping,
            [Description("Sort A-Z")]
            AtoZ,
            [Description("Sort Z-A")]
            ZtoA,
            [Description("Sort by Fullest Lobby")]
            FullestLobby,
            [Description("Sort by Emptiest Lobby")]
            EmptiestLobby
        }

        public enum GameModeFilter
        {
            All,
            Meadow,
            Story,
            ArenaCompetitive
        }

        public LobbyCardsFilter()
        {
            enabled = true;
            lobbyName = "";
            sortingOrder = "Ping";
            gameMode = "All";
            requiredMods = "Any";
            publicLobby = false;
        }

        public void CycleSortOrder()
        {
            sortingOrder = sortingOrder switch
            {
                "Ping" => "AtoZ",
                "AtoZ" => "ZtoA",
                "ZtoA" => "FullestLobby",
                "FullestLobby" => "EmptiestLobby",
                _ => "Ping"
            };
        }

        public string GetFormattedSortingOrderName()
        {
            return sortingOrder switch
            {
                "Ping" => "Sort by Ping",
                "AtoZ" => "Sort A to Z",
                "ZtoA" => "Sort Z to A",
                "FullestLobby" => "Sort by Fullest Lobby",
                "EmptiestLobby" => "Sort by Emptiest Lobby",
                _ => "Unknown Sorting Method"
            };
        }

        public string GetSortingOrderSymbolName()
        {
            return sortingOrder switch
            {
                "Ping" => "Meadow_Menu_Ping",
                "AtoZ" => "Meadow_Menu_Sort_A-Z",
                "ZtoA" => "Meadow_Menu_Sort_Z-A",
                "FullestLobby" => "Kill_Slugcat",
                "EmptiestLobby" => "GuidanceSlugcat",
                _ => "Sandbox_SmallQuestion"
            };
        }
    }

    public List<LobbyCard> lobbyCards;
    public List<LobbyInfo> filteredLobbies;
    public List<LobbyInfo> allLobbies;
    public LobbyCardsFilter filter;
    public float movementPercentage;
    public EventfulScrollButton scrollUpButton;
    public EventfulScrollButton scrollDownButton;
    public SimplerSymbolButton[] sideButtons;
    public MenuLabel[] sideButtonLabels;
    public float[,] sideButtonLabelsFade;
    public FSprite[] rightLines;
    public VerticalSlider scrollSlider;
    /// <summary>
    /// The fade value of the lobby list into the lobby creation menu
    /// </summary>
    public float floatScrollPos;
    public float floatScrollVel;
    public float sliderValue;
    public float sliderValueCap;
    public bool sliderPulled;
    public int scrollPos;
    public OpTextBox searchBar;
    public int TotalItems => lobbyCards.Count;
    public int MaxVisibleItems => (int)(size.y / CardHeight);
    public int LastPossibleScroll => Math.Max(0, TotalItems - MaxVisibleItems);
    public float CardHeight => 70;
    public SimplerSymbolButton RefreshButton => sideButtons[0];
    //public SimplerSymbolButton FilterButton => sideButtons[1];
    public SimplerSymbolButton OrderButton => sideButtons[1];
    private float LowerBound => 0;
    private float UpperBound => size.y;

    public float ValueOfSlider(Slider slider)
    {
        return 1f - sliderValue;
    }

    public void SliderSetValue(Slider slider, float setValue)

    {
        sliderValue = 1f - setValue;
        sliderPulled = true;
    }

    public void AddScroll(int scrollDir)
    {
        scrollPos += scrollDir;
        ConstrainScroll();
    }

    public void ConstrainScroll()
    {
        if (scrollPos > LastPossibleScroll) scrollPos = LastPossibleScroll;
        if (scrollPos < 0) scrollPos = 0;
    }

    public float StepsDownOfItem(int itemIndex)
    {
        float num = 0f;
        for (int i = 0; i <= Math.Min(itemIndex, lobbyCards.Count - 1); i++)
        {
            num += (i > 0) ? Mathf.Pow(Custom.SCurve(1f, 0.3f), 0.5f) : 1f;
        }

        return num;
    }

    public float IdealYPosForItem(int itemIndex)
    {
        float num = StepsDownOfItem(itemIndex);
        num -= floatScrollPos;
        return size.y - num * CardHeight;
    }

    /// <summary>
    /// Returns a float between 0 and 1 depending on how far the given y pos is from the y boundaries of the list within a distance of CardHeight
    /// </summary>
    public float PercentageOverYBound(float y)
    {
        if (y < LowerBound) return 1 - Math.Min(1, (LowerBound - y) / CardHeight);
        float cardUpperBound = y + CardHeight;
        if (cardUpperBound > UpperBound) return 1 - Math.Min(1, (cardUpperBound - UpperBound) / CardHeight);
        return 1;
    }

    public void ClearLobbies() {
        foreach (var card in lobbyCards)
        {
            if (card == null) continue;
            card.RemoveSprites();
            owner.RemoveSubObject(card);
        }

        allLobbies.Clear();
        filteredLobbies.Clear();
        lobbyCards.Clear();
    }

    public void FilterLobbies()
    {
        filteredLobbies = new List<LobbyInfo>();

        string[] requiredMods = RainMeadowModManager.GetRequiredMods();
        string requiredModsString = RainMeadowModManager.RequiredModsArrayToString(requiredMods); //used for unused "Exact" filter

        foreach (var lobby in allLobbies)
        {
            if (filter.lobbyName != "" && !lobby.name.ToLower().Contains(filter.lobbyName)) continue;
            if (filter.enabled)
            {
                if (filter.gameMode != "All" && lobby.mode != filter.gameMode) continue;
                if (filter.publicLobby && lobby.hasPassword) continue;
                //filter for required mods
                bool missingMod = false;
                switch (filter.requiredMods)
                {
                    case "Any": break;
                    case "Exact": //currently unused filter
                        missingMod = lobby.requiredMods != requiredModsString;
                        break;
                    case "All":
                        string[] lobbyMods = RainMeadowModManager.RequiredModsStringToArray(lobby.requiredMods);
                        if (lobbyMods.Length != requiredMods.Length) { missingMod = true; break; }
                        foreach (string m in lobbyMods)
                        {
                            if (!requiredMods.Contains(m)) { missingMod = true; break; }
                        }
                        break;
                    default: //filter.requiredMods = single mod ID to check for
                        missingMod = !lobby.requiredMods.Contains(filter.requiredMods);
                        break;
                }
                if (missingMod) continue;
            }

            filteredLobbies.Add(lobby);
        }

        CreateCards();
    }

    public void ToggleFilterEnabled(SymbolButton obj)
    {
        //filter.enabled = !filter.enabled;
        movementPercentage = 0;
        FilterLobbies();
    }

    public void UpdateSearchFilter()
    {
        filter.lobbyName = searchBar.value.ToLower();

        FilterLobbies();
    }

    /// <summary>
    /// Reorders filteredLobbies according to filter.sortingOrder and recreates all lobby cards. Call FilterLobbies instead of CreateCards so that filters also apply
    /// </summary>
    // TODO implement sort by ping
    public void CreateCards()
    {
        filteredLobbies = filter.sortingOrder switch
        {
            "ZtoA" => filteredLobbies.OrderByDescending(lobby => lobby.name).ToList(),
            "FullestLobby" => filteredLobbies.OrderByDescending(lobby => lobby.playerCount).ToList(),
            "EmptiestLobby" => filteredLobbies.OrderBy(lobby => lobby.playerCount).ToList(),
            _ => filteredLobbies.OrderBy(lobby => lobby.name).ToList()
        };

        foreach (var card in lobbyCards)
        {
            if (card == null) continue;
            card.RemoveSprites();
            owner.RemoveSubObject(card);
        }

        lobbyCards = new List<LobbyCard>();

        for (int i = 0; i < filteredLobbies.Count; i++)
        {
            var card = new LobbyCard(menu, this, filteredLobbies[i]);

            card.pos.x = 15f;
            card.pos.y = IdealYPosForItem(filteredLobbies.Count);
            lobbyCards.Add(card);
            owner.subObjects.Add(card);
        }

        ConstrainScroll();
    }

    public void CycleSortOrder(SymbolButton obj)
    {
        filter.CycleSortOrder();
        obj.UpdateSymbol(filter.GetSortingOrderSymbolName());
        CreateCards();
    }

    /// <summary>
    /// Chunky initializer for a LobbyCardsList.
    /// Manually add a refresh method to the OnChange event for RefreshButton.
    /// When trying to reorder/refresh the lobby list display, call <c>FilterLobbies</c>
    /// </summary>
    public LobbyCardsList(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
    {
        if (!Futile.atlasManager.DoesContainAtlas("ui_elements"))
        {
            HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/ui_elements").name);
        }
        filter = new LobbyCardsFilter();

        searchBar = new OpTextBox(new Configurable<string>(""), new Vector2(pos.x + 15, pos.y + size.y), 300);
        searchBar.label.text = Utils.Translate("Search Lobbies");
        searchBar.accept = OpTextBox.Accept.StringASCII;
        searchBar.allowSpace = true;
        searchBar.OnChange += UpdateSearchFilter;
        if (menu is SmartMenu smartMenu) new UIelementWrapper(smartMenu.tabWrapper, searchBar);

        myContainer = new FContainer();
        owner.Container.AddChild(myContainer);

        scrollUpButton = new EventfulScrollButton(menu, this, new Vector2(0.01f + size.x / 2f - 50f, size.y + 34f), 0, 24);
        scrollUpButton.size.x = 100f;
        scrollUpButton.roundedRect.size.x = 100f;
        scrollUpButton.OnClick += (_) => AddScroll(-1);
        subObjects.Add(scrollUpButton);
        scrollDownButton = new EventfulScrollButton(menu, this, new Vector2(0.01f + size.x / 2f - 50f, -34f), 2, 24);
        scrollDownButton.size.x = 100f;
        scrollDownButton.roundedRect.size.x = 100f;
        scrollDownButton.OnClick += (_) => AddScroll(1);
        subObjects.Add(scrollDownButton);

        rightLines = new FSprite[3];
        for (int i = 0; i < rightLines.Length; i++)
        {
            rightLines[i] = new FSprite("pixel");
            rightLines[i].anchorX = 0f;
            rightLines[i].anchorY = 0f;
            rightLines[i].scaleX = 2f;
            Container.AddChild(rightLines[i]);
        }

        sideButtons = new SimplerSymbolButton[2];
        sideButtons[0] = new SimplerSymbolButton(menu, this, "Menu_Symbol_Repeats", "", new Vector2(size.x - 8f + 0.01f, 14.01f));
        subObjects.Add(sideButtons[0]);
        //sideButtons[1] = new SimplerSymbolButton(menu, this, "Meadow_Menu_Filter", "", sideButtons[0].pos + new Vector2(0, 30f));
        //sideButtons[1].OnClick += ToggleFilterEnabled;
        //subObjects.Add(sideButtons[1]);
        sideButtons[1] = new SimplerSymbolButton(menu, this, "Meadow_Menu_Ping", "", sideButtons[0].pos + new Vector2(0, 30f));
        sideButtons[1].OnClick += CycleSortOrder;
        subObjects.Add(sideButtons[1]);

        sideButtonLabels = new MenuLabel[1];
        sideButtonLabelsFade = new float[sideButtonLabels.Length, 2];
        for (int j = 0; j < sideButtonLabels.Length; j++)
        {
            sideButtonLabels[j] = new MenuLabel(menu, this, "", sideButtons[j].pos + new Vector2(10f, -3f), new Vector2(50f, 30f), bigText: false);
            sideButtonLabels[j].label.alignment = FLabelAlignment.Left;
            subObjects.Add(sideButtonLabels[j]);
        }
        sideButtonLabels[0].text = menu.Translate("Refresh list");

        scrollSlider = new VerticalSlider(menu, this, "Slider", new Vector2(-16f, 9f), new Vector2(30f, size.y - 40f), Slider.SliderID.LevelsListScroll, subtleSlider: true);
        subObjects.Add(scrollSlider);
        floatScrollPos = scrollPos;

        allLobbies = new List<LobbyInfo>();
        filteredLobbies = new List<LobbyInfo>();
        lobbyCards = new List<LobbyCard>();
    }

    public override void Update()
    {

        if (MouseOver && menu.manager.menuesMouseMode && menu.mouseScrollWheelMovement != 0)
        {
            AddScroll(menu.mouseScrollWheelMovement);
        }

        for (int i = 0; i < lobbyCards.Count; i++)
        {
            lobbyCards[i].pos.y = IdealYPosForItem(i);
        }

        base.Update();

        float num = scrollPos;

        floatScrollPos = Custom.LerpAndTick(floatScrollPos, num, 0.01f, 0.01f);
        floatScrollVel *= Custom.LerpMap(Math.Abs(num - floatScrollPos), 0.25f, 1.5f, 0.45f, 0.99f);
        floatScrollVel += Mathf.Clamp(num - floatScrollPos, -2.5f, 2.5f) / 2.5f * 0.15f;
        floatScrollVel = Mathf.Clamp(floatScrollVel, -1.2f, 1.2f);
        floatScrollPos += floatScrollVel;
        sliderValueCap = Custom.LerpAndTick(sliderValueCap, LastPossibleScroll, 0.02f, (float)lobbyCards.Count / 40f);
        if (LastPossibleScroll == 0)
        {
            sliderValue = Custom.LerpAndTick(sliderValue, 0.5f, 0.02f, 0.05f);
            scrollSlider.buttonBehav.greyedOut = true;
        }
        else
        {

            if (sliderPulled)
            {
                floatScrollPos = Mathf.Lerp(0f, sliderValueCap, sliderValue);
                scrollPos = Custom.IntClamp(Mathf.RoundToInt(floatScrollPos), 0, LastPossibleScroll);
                sliderPulled = false;
            }
            else
            {
                sliderValue = Custom.LerpAndTick(sliderValue, Mathf.InverseLerp(0f, sliderValueCap, floatScrollPos), 0.02f, 0.05f);
            }
        }


        for (int i = 0; i < sideButtonLabels.Length; i++)
        {
            sideButtonLabelsFade[i, 1] = sideButtonLabelsFade[i, 0];
            if (sideButtons[i].Selected)
            {
                sideButtonLabelsFade[i, 0] = Custom.LerpAndTick(sideButtonLabelsFade[i, 0], 0.33f, 0.04f, 1f / 60f);

                switch (i)
                {
                    //case 1:
                    //    sideButtonLabels[1].text = filter.enabled ? "Disable Filters" : "Enable Filters";
                    //    sideButtons[1].UpdateSymbol(filter.enabled ? "Meadow_Menu_Cancel_Filter" : "Meadow_Menu_Filter");
                    //    break;
                    case 1:
                        sideButtonLabels[2].text = filter.GetFormattedSortingOrderName();
                        break;
                }
            }
            else
            {
                sideButtonLabelsFade[i, 0] = Custom.LerpAndTick(sideButtonLabelsFade[i, 0], 0f, 0.04f, 1f / 60f);
            }
        }
    }

    public override void GrafUpdate(float timeStacker)
    {

        base.GrafUpdate(timeStacker);

        for (int i = 0; i < rightLines.Length; i++)
        {
            // rightLines[i].alpha = 1f - creationMenuFade;
            float num = (i != 0) ? (sideButtons[i - 1].DrawY(timeStacker) + sideButtons[i - 1].DrawSize(timeStacker).y + 0.01f) : (DrawY(timeStacker) + 9.01f);
            float num2 = (i != rightLines.Length - 1) ? (sideButtons[i].DrawY(timeStacker) + 0.01f) : (DrawY(timeStacker) + DrawSize(timeStacker).y - 10.99f);
            rightLines[i].x = DrawX(timeStacker) + size.x + 0.01f;
            rightLines[i].y = num;
            rightLines[i].scaleY = num2 - num;
            rightLines[i].color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
        }

        for (int i = 0; i < sideButtonLabels.Length; i++)
        {
            sideButtonLabels[i].label.alpha = Mathf.Lerp(sideButtonLabelsFade[i, 1], sideButtonLabelsFade[i, 0], timeStacker);
        }
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);

        switch (message)
        {
        }
    }
}
