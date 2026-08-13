using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Base;
using RainMeadow.UI.Components.Patched;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class LobbyCardSelector : ButtonScroller, SelectOneButton.SelectOneButtonOwner
{
    public MenuTabWrapper tabWrapper;
    public OpTextBox searchBar;
    public SimplerSymbolButton refreshButton,
        sortButton;
    public LobbyInfo[] lobbyInfos = [];
    public LobbyCard[] lobbyCards = [];
    public Filter filter = new();
    public SortingOrder sortingOrder;

    public event Action? RefreshLobbies;
    public event Action? OnLobbyCardsUpdated;
    public event Action<LobbyInfo>? OnLobbyCardClicked;

    public int selectedLobbyIndex = -1;

    public LobbyInfo? SelectedLobby
    {
        get
        {
            if (selectedLobbyIndex < 0)
                return null;
            return lobbyCards[selectedLobbyIndex].lobbyInfo;
        }
    }

    public LobbyCardSelector(Menu.Menu menu, MenuObject owner, Vector2 pos)
        : base(
            menu,
            owner,
            pos,
            5,
            550,
            (80, 10),
            sliderSizeYOffset: -40,
            startEndWithSpacing: true
        )
    {
        Futile.atlasManager.LoadAtlas("illustrations/ui_elements");
        Futile.atlasManager.LoadAtlas("illustrations/modtexticons");
        tabWrapper = new MenuTabWrapper(menu, this);

        greyOutWhenNoScroll = true;
        AddScrollUpDownButtons(100, 24);
        CreateSideButtonLines();

        searchBar = new OpTextBox(new Configurable<string>(""), new Vector2(0, size.y - 10), 500)
        {
            accept = OpTextBox.Accept.StringASCII,
            allowSpace = true,
            description = menu.Translate("Search lobbies by name"),
        };
        searchBar.label.text = menu.Translate("Search Lobbies");
        searchBar.OnChange += () =>
        {
            if (filter.lobbyName == searchBar.value)
                return;
            filter.lobbyName = searchBar.value;
            filter.FilterInfos(lobbyInfos);
            UpdateLobbyCards();
        };

        refreshButton = new SimplerSymbolButton(
            menu,
            this,
            "Menu_Symbol_Repeats",
            "",
            new Vector2(534, size.y - 10),
            menu.Translate("Refresh lobbies list")
        );
        refreshButton.OnClick += (btn) => RefreshLobbies?.Invoke();

        sortButton = new SimplerSymbolButton(
            menu,
            this,
            "Meadow_Menu_Sort_A-Z",
            "",
            new Vector2(505, size.y - 10),
            menu.Translate("Sort A to Z")
        );
        sortButton.OnClick += CycleSortingOrder;
        sortingOrder = SortingOrder.AtoZ;

        new PatchedUIelementWrapper(tabWrapper, searchBar);
        this.SafeAddSubobjects(tabWrapper, refreshButton, sortButton);
    }

    public void CycleSortingOrder(SymbolButton btn)
    {
        // ignore dumb casting cases like (SortingOrder)1290467 as a possible branch. Will still create warnings if new
        // entries are added to the SortingOrder enum properly.
#pragma warning disable CS8524
        (SortingOrder order, string iconName, string description) = sortingOrder switch
        {
            SortingOrder.AtoZ => (SortingOrder.ZtoA, "Meadow_Menu_Sort_Z-A", "Sort Z to A"),
            SortingOrder.ZtoA => (SortingOrder.Fullest, "Kill_Slugcat", "Sort by fullest lobby"),
            SortingOrder.Fullest => (
                SortingOrder.Emptiest,
                "GuidanceSlugcat",
                "Sort by emptiest lobby"
            ),
            SortingOrder.Emptiest => (SortingOrder.AtoZ, "Meadow_Menu_Sort_A-Z", "Sort A to Z"),
        };
#pragma warning restore CS8524

        sortingOrder = order;
        sortButton.UpdateSymbol(iconName);
        sortButton.Description = menu.Translate(description);
        UpdateLobbyCards();
    }

    public void ClearLobbyCards()
    {
        selectedLobbyIndex = -1;
        RemoveAllButtons();
    }

    public void UpdateLobbyCards()
    {
        ClearLobbyCards();

        List<LobbyInfo> pinnedCards =
        [
            .. filter.filteredInfos.TakeWhile(lobbyInfo => lobbyInfo.pinned),
        ];
        List<LobbyInfo> regularCards = [.. filter.filteredInfos.Except(pinnedCards)];

#pragma warning disable CS8524
        List<LobbyInfo> SortCards(List<LobbyInfo> cards)
        {
            return
            [
                .. sortingOrder switch
                {
                    SortingOrder.AtoZ => cards.OrderBy(info => info.name),
                    SortingOrder.ZtoA => cards.OrderByDescending(info => info.name),
                    SortingOrder.Fullest => cards.OrderByDescending(info => info.playerCount),
                    SortingOrder.Emptiest => cards.OrderBy(info => info.playerCount),
                },
            ];
        }
#pragma warning restore CS8524

        lobbyCards =
        [
            .. SortCards(pinnedCards)
                .Concat(SortCards(regularCards))
                .Select(
                    (lobbyInfo, index) =>
                    {
                        LobbyCard card = new(menu, this, lobbyInfo, lobbyCards, index);
                        card.OnLobbyCardClicked += OnLobbyCardClicked;
                        return card;
                    }
                ),
        ];

        AddScrollObjects(lobbyCards);
        OnLobbyCardsUpdated?.Invoke();
    }

    public void UpdateLobbyInfos(LobbyInfo[] lobbyInfos)
    {
        this.lobbyInfos = lobbyInfos;
        filter.FilterInfos(lobbyInfos);
        UpdateLobbyCards();
    }

    public void UpdateFilters(
        bool publicOnly,
        int maxPlayerCount,
        string gamemode,
        string requiredMods
    )
    {
        filter.publicOnly = publicOnly;
        filter.maxPlayerCount = maxPlayerCount;
        filter.gamemode = gamemode;
        filter.requiredMods = requiredMods;
        filter.FilterInfos(lobbyInfos);
        UpdateLobbyCards();
    }

    public int GetCurrentlySelectedOfSeries(string series) => selectedLobbyIndex;

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        if (selectedLobbyIndex != -1)
        {
            LobbyCard prevCard = lobbyCards[selectedLobbyIndex];
            prevCard.Description = menu.Translate("Click to view <LOBBY>")
                .Replace("<LOBBY>", prevCard.lobbyInfo.name);
        }

        selectedLobbyIndex = to;
    }

    public enum SortingOrder
    {
        AtoZ,
        ZtoA,
        Fullest,
        Emptiest,
    }

    public class LobbyCard : EventfulSelectOneButton, IPartOfButtonScroller
    {
        public LobbyInfo lobbyInfo;

        public event Action<LobbyInfo>? OnLobbyCardClicked;

        public float Alpha { get; set; } = 1;
        public Vector2 Pos
        {
            get => pos;
            set => pos = value;
        }
        public Vector2 Size
        {
            get => size;
            set => size = value;
        }

        public LobbyCard(
            Menu.Menu menu,
            MenuObject owner,
            LobbyInfo lobbyInfo,
            EventfulSelectOneButton[] lobbyCards,
            int lobbyCardIndex
        )
            : base(
                menu,
                owner,
                "",
                "LOBBY_CARDS",
                Vector2.zero,
                new Vector2(550, 80),
                lobbyCards,
                lobbyCardIndex,
                description: menu.Translate("Click to view <LOBBY>")
                    .Replace("<LOBBY>", lobbyInfo.name)
            )
        {
            this.lobbyInfo = lobbyInfo;
            OnClick += (btn) =>
            {
                Description = menu.Translate("Click to join <LOBBY>")
                    .Replace("<LOBBY>", lobbyInfo.name);
                OnLobbyCardClicked?.Invoke(lobbyInfo);
            };

            menuLabel.RemoveSprites();
            RemoveSubObject(menuLabel);

            string[] splitName = MenuHelpers.SmartSplitIntoStrings(lobbyInfo.name, 500, true);
            string name = splitName[0];
            if (splitName.Length > 1)
                name += "...";

            menuLabel = new ProperlyAlignedMenuLabel(
                menu,
                this,
                name,
                new Vector2(10, 50),
                new Vector2(550, 30),
                true
            );

            ProperlyAlignedMenuLabel gamemodeLabel = new(
                menu,
                this,
                lobbyInfo.mode,
                new Vector2(10, 38),
                Vector2.zero,
                false
            );

            MenuSprite? pinnedIcon = lobbyInfo.pinned
                ? new(menu, this, new FSprite("Meadow_Menu_Pin"), new Vector2(535, 65))
                : null;

            ProperlyAlignedMenuLabel? privateLabel = lobbyInfo.hasPassword
                ? new(
                    menu,
                    this,
                    menu.Translate("Private"),
                    new Vector2(540, 25),
                    Vector2.zero,
                    false
                )
                : null;
            privateLabel?.label.alignment = FLabelAlignment.Right;

            ProperlyAlignedMenuLabel playerCountLabel = new(
                menu,
                this,
                menu.Translate("<PLAYER_COUNT>/<MAX_PLAYERS> Players")
                    .Replace("<PLAYER_COUNT>", lobbyInfo.playerCount.ToString())
                    .Replace("<MAX_PLAYERS>", lobbyInfo.maxPlayerCount.ToString()),
                new Vector2(540, 10),
                Vector2.zero,
                false
            );
            playerCountLabel.label.alignment = FLabelAlignment.Right;

            List<(PositionedMenuObject icon, float xSize)> icons = [];

            if (!lobbyInfo.activeTimeline.IsNullOrWhiteSpace())
                icons.Add(
                    (
                        new PositionedSlugIcon(
                            menu,
                            this,
                            new Vector2(0, 8),
                            lobbyInfo.activeTimeline
                        ),
                        30
                    )
                );

            int accountedModCount = 1; // Rain Meadow :P
            void AddTextIcon(string iconName)
            {
                PositionedSprite sprite = new(
                    menu,
                    this,
                    new Vector2(0, 10),
                    new FSprite(iconName)
                );
                icons.Add((sprite, sprite.Size.x));
                accountedModCount += 1;
            }

            if (lobbyInfo.requiredMods.Contains("moreslugcats"))
            {
                AddTextIcon("msc_dlc_text_icon");
                accountedModCount += 1; // rwremix is a requirement for msc
            }
            else if (lobbyInfo.requiredMods.Contains("rwremix"))
                AddTextIcon("rmx_mod_text_icon");

            if (lobbyInfo.requiredMods.Contains("watcher"))
                AddTextIcon("watcher_dlc_text_icon");

            if (
                lobbyInfo.requiredMods.Length > 0
                && lobbyInfo.requiredMods.Split('\n').Length > accountedModCount
            )
                AddTextIcon("mod_text_icon");

            for (int i = 0; i < icons.Count; i++)
            {
                float xPos = 9;
                for (int j = 0; j < i; j++)
                    xPos += icons[j].xSize;
                icons[i].icon.pos.x = xPos;
            }

            this.SafeAddSubobjects(
                menuLabel,
                gamemodeLabel,
                pinnedIcon,
                privateLabel,
                playerCountLabel
            );
            this.SafeAddSubobjects([.. icons.Select((iconData) => iconData.icon)]);
        }
    }

    public class Filter()
    {
        public List<LobbyInfo> filteredInfos = [];

        public int maxPlayerCount = 99;
        public bool publicOnly = false;
        public string gamemode = "__any",
            lobbyName = "",
            requiredMods = "";

        // TODO: Redo mod filter. I didn't write it and don't understand the logic behind it but am aware it's a bit lacking
        // and could do with a facelift both frontend and backend wise
        public void FilterInfos(LobbyInfo[] lobbyInfos)
        {
            filteredInfos.Clear();

            foreach (LobbyInfo lobbyInfo in lobbyInfos)
            {
                if (publicOnly && lobbyInfo.hasPassword)
                    continue;
                if (maxPlayerCount < lobbyInfo.maxPlayerCount)
                    continue;
                if (gamemode != "__any" && lobbyInfo.mode != gamemode)
                    continue;
                if (lobbyName != "" && !lobbyInfo.name.ToLower().Contains(lobbyName.ToLower()))
                    continue;

                // DLC checks
                var hasMsc = lobbyInfo.requiredMods.Contains("moreslugcats");
                var hasWatcher = lobbyInfo.requiredMods.Contains("watcher");
                //filter for required mods
                bool missingMod = false;
                switch (requiredMods)
                {
                    case "Any":
                        break;
                    // Mod checks
                    case "MSC":
                        missingMod = !hasMsc;
                        break;
                    case "Watcher":
                        missingMod = !hasWatcher;
                        break;
                    case "MSC + Watcher":
                        missingMod = !(hasMsc && hasWatcher);
                        break;
                    case "All":
                        string[] lobbyMods = RainMeadowModManager.ModStringToArray(
                            lobbyInfo.requiredMods
                        );
                        if (lobbyMods.Length != requiredMods.Length)
                        {
                            missingMod = true;
                            break;
                        }
                        foreach (string m in lobbyMods)
                        {
                            if (!requiredMods.Contains(m))
                            {
                                missingMod = true;
                                break;
                            }
                        }
                        break;
                    default: //filter.requiredMods = single mod ID to check for
                        missingMod = !lobbyInfo.requiredMods.Contains(requiredMods);
                        break;
                }
                if (missingMod)
                    continue;

                filteredInfos.Add(lobbyInfo);
            }
        }
    }
}
