using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using RainMeadow.UI.Components.Patched;
using UnityEngine;

namespace RainMeadow.UI.Menus.Panels;

class LobbySelectFiltersPanel : PositionedMenuObject, CheckBox.IOwnCheckBox
{
    public delegate void OnFilterUpdateHandler(
        bool publicOnly,
        int maxPlayers,
        string gamemode,
        string mods
    );

    public MenuTabWrapper tabWrapper;
    public MenuLabel gamemodeFilterLabel,
        sizeFilterLabel,
        modFilterLabel;
    public OpComboBox2 gamemodeFilterComboBox,
        modFilterComboBox;
    public OpUpdown sizeFilterTextBox;
    public RestorableCheckbox publicLobbiesOnlyCheckBox;

    public event OnFilterUpdateHandler? OnFilterUpdated;

    public int maxLobbySize;
    public bool publicOnly;

    public const string PUBLIC_FILTER = "public_lobbies_only";

    public LobbySelectFiltersPanel(Menu.Menu menu, MenuObject owner, Vector2 pos)
        : base(menu, owner, pos)
    {
        tabWrapper = new(menu, this);
        int textSpacing = 130; // I'm not even joking this number might not even matter cause this UI keeps doing whatever it wants
        Vector2 positioner = new(145, 450);

        publicLobbiesOnlyCheckBox = new RestorableCheckbox(
            menu,
            this,
            this,
            positioner + new Vector2(58, 0),
            181,
            menu.Translate("Public Lobbies Only"),
            PUBLIC_FILTER,
            description: menu.Translate("Filter for only public lobbies")
        );

        positioner.y -= 40;

        sizeFilterTextBox = new OpUpdown(
            new Configurable<int>(99, new ConfigAcceptableRange<int>(0, 99)),
            positioner + new Vector2(40, -5),
            60
        )
        {
            description = menu.Translate("Filter for a maximum lobby size"),
            accept = OpTextBox.Accept.Int,
            maxLength = 2,
        };
        sizeFilterTextBox.OnChange += () =>
        {
            if (maxLobbySize == sizeFilterTextBox.valueInt)
                return;
            maxLobbySize = sizeFilterTextBox.valueInt;
            OnFilterChange();
        };
        new PatchedUIelementWrapper(tabWrapper, sizeFilterTextBox);
        maxLobbySize = 99;

        sizeFilterLabel = new MenuLabel(
            menu,
            this,
            menu.Translate("Max Lobby Size"),
            sizeFilterTextBox.pos + new Vector2(textSpacing * -1.5f - 37.5f, 3),
            new Vector2(textSpacing, 20),
            false
        );
        sizeFilterLabel.label.alignment = FLabelAlignment.Left;

        positioner.y -= 40;

        List<ListItem> filterEnumNames =
        [
            .. OpResourceSelector
                .GetEnumNames(null, typeof(OnlineGameMode.OnlineGameModeType))
                .Select(gamemode =>
                {
                    gamemode.displayName = menu.Translate(gamemode.displayName);
                    return gamemode;
                }),
        ];
        filterEnumNames.Add(new ListItem("__any", menu.Translate("Any")));

        gamemodeFilterComboBox = new OpComboBox2(
            new Configurable<string>("__any"),
            positioner,
            140f,
            filterEnumNames
        )
        {
            description = menu.Translate("Filter for a specific lobby gamemode"),
        };
        gamemodeFilterComboBox.OnChange += OnFilterChange;

        gamemodeFilterLabel = new(
            menu,
            this,
            menu.Translate("Lobby Gamemode"),
            gamemodeFilterComboBox.pos + new Vector2(textSpacing * -1.5f + 7.5f, 3),
            new Vector2(textSpacing, 20),
            false
        );
        gamemodeFilterLabel.label.alignment = FLabelAlignment.Left;

        positioner.y -= 40;

        List<ListItem> requiredModsList =
        [
            new("Any", menu.Translate("Unfiltered"), 0),
            new("MSC", menu.Translate("MSC"), 1),
            new("Watcher", menu.Translate("Watcher"), 2),
            new("MSC + Watcher", menu.Translate("MSC + Watcher"), 3),
            new("Exact", menu.Translate("Exact order"), 4),
            new("All", menu.Translate("Any order"), int.MaxValue),
        ];

        string[] requiredModIDs = RainMeadowModManager.GetRequiredMods();
        foreach (string id in requiredModIDs)
        {
            if (id == "henpemaz_rainmeadow")
                continue;
            requiredModsList.Add(
                new ListItem(id, "+" + RainMeadowModManager.ModIdToName(id), requiredModsList.Count)
            );
        }

        modFilterComboBox = new OpComboBox2(
            new Configurable<string>("Any"),
            positioner,
            140f,
            requiredModsList
        )
        {
            description = menu.Translate("Filter lobbies for specific mods"),
        };
        modFilterComboBox.OnChange += OnFilterChange;

        modFilterLabel = new MenuLabel(
            menu,
            this,
            menu.Translate("Lobby Mods"),
            modFilterComboBox.pos + new Vector2(-textSpacing - 25, 3), // yeah idk what this positioning is anymore
            new Vector2(textSpacing, 20),
            false
        );
        sizeFilterLabel.label.alignment = FLabelAlignment.Left;

        new PatchedUIelementWrapper(tabWrapper, gamemodeFilterComboBox);
        new PatchedUIelementWrapper(tabWrapper, modFilterComboBox);
        this.SafeAddSubobjects(
            tabWrapper,
            publicLobbiesOnlyCheckBox,
            gamemodeFilterLabel,
            sizeFilterLabel,
            modFilterLabel
        );
    }

    public void OnFilterChange() =>
        OnFilterUpdated?.Invoke(
            publicOnly,
            sizeFilterTextBox.valueInt,
            gamemodeFilterComboBox.value,
            modFilterComboBox.value
        );

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        gamemodeFilterLabel.label.color = gamemodeFilterComboBox.colorEdge;
        sizeFilterLabel.label.color = sizeFilterTextBox.rect.colorEdge;
        modFilterLabel.label.color = modFilterComboBox.colorEdge;
    }

    public bool GetChecked(CheckBox box)
    {
        if (box.IDString == PUBLIC_FILTER)
            return publicOnly;
        else
            return false;
    }

    public void SetChecked(CheckBox box, bool c)
    {
        if (box.IDString == PUBLIC_FILTER)
            publicOnly = c;
        OnFilterChange();
    }
}
