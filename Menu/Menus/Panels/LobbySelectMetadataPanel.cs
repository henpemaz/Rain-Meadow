using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix;
using RainMeadow.UI.Components;
using RainMeadow.UI.Components.Base;
using UnityEngine;

namespace RainMeadow.UI.Menus.Panels;

public class LobbySelectMetadataPanel : PositionedMenuObject
{
    public MenuTabWrapper tabWrapper;
    public ProperlyAlignedMenuLabel lobbyNameLabel,
        gamemodeLabel,
        hasPasswordLabel,
        playerCountLabel;
    public TextScroller highImpactModsTextScroller;
    public PositionedSprite separator;
    public PositionedSlugIcon timelineIcon;

    public LobbySelectMetadataPanel(Menu.Menu menu, MenuObject owner, Vector2 pos)
        : base(menu, owner, pos)
    {
        ProperlyAlignedMenuLabel EmptyLabel() =>
            new(menu, this, "", Vector2.zero, Vector2.zero, false);

        tabWrapper = new MenuTabWrapper(menu, this);

        highImpactModsTextScroller = new TextScroller(
            menu,
            this,
            Vector2.zero,
            new Vector2(250, 198),
            sliderSizeYOffset: -5
        )
        {
            greyOutWhenNoScroll = true,
        };
        highImpactModsTextScroller.Container.isVisible = false;

        lobbyNameLabel = new ProperlyAlignedMenuLabel(
            menu,
            this,
            menu.Translate("No Lobby Selected"),
            new Vector2(20, 460),
            Vector2.zero,
            true
        );
        gamemodeLabel = EmptyLabel();
        hasPasswordLabel = EmptyLabel();
        playerCountLabel = EmptyLabel();

        timelineIcon = new PositionedSlugIcon(menu, this, Vector2.zero, "");

        separator = new PositionedSprite(
            menu,
            this,
            new Vector2(10, 405),
            new FSprite("pixel")
            {
                alpha = 0,
                color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey),
                scaleY = 2,
                scaleX = 280,
            }
        );

        subObjects.AddRange([
            tabWrapper,
            lobbyNameLabel,
            gamemodeLabel,
            hasPasswordLabel,
            playerCountLabel,
            timelineIcon,
            separator,
            highImpactModsTextScroller,
        ]);
    }

    public void UpdateLobbyInfo(LobbyInfo lobbyInfo)
    {
        string[] splitName = MenuHelpers.SmartSplitIntoStrings(lobbyInfo.name, 260, true);
        int nameLineCountSpacingMultiplier = Math.Min(splitName.Length, 3) - 1;
        string name = "";
        for (int i = 0; i <= nameLineCountSpacingMultiplier; i++)
        {
            if (i == 2 && splitName.Length > 3)
                name += splitName[i] + "...";
            else
                name += splitName[i] + "\n";
        }

        Vector2 positioner = new(20, 460 - 30 * nameLineCountSpacingMultiplier);
        Vector2 spacing = new(0, -15);

        lobbyNameLabel.text = name;
        lobbyNameLabel.pos = positioner;

        gamemodeLabel.text = lobbyInfo.mode;
        if (lobbyInfo.activeTimeline != "")
            gamemodeLabel.text +=
                $" - {SlugcatStats.getSlugcatName(new SlugcatStats.Name(lobbyInfo.activeTimeline))}";
        gamemodeLabel.pos = positioner + spacing;

        hasPasswordLabel.text = menu.Translate(lobbyInfo.hasPassword ? "Private" : "Public");
        hasPasswordLabel.pos = positioner + spacing * 2;

        playerCountLabel.text = menu.Translate("<PLAYER_COUNT>/<MAX_PLAYERS> Players")
            .Replace("<PLAYER_COUNT>", lobbyInfo.playerCount.ToString())
            .Replace("<MAX_PLAYERS>", lobbyInfo.maxPlayerCount.ToString());
        playerCountLabel.pos = positioner + spacing * 3;

        timelineIcon.pos = positioner + spacing * 3 + new Vector2(235, 0);
        timelineIcon.DrawScugSprites(lobbyInfo.activeTimeline);

        separator.pos = new Vector2(10, 405 - 30 * nameLineCountSpacingMultiplier);
        separator.Sprite.alpha = 1;

        highImpactModsTextScroller.pos = new Vector2(30, 180 - 30 * nameLineCountSpacingMultiplier);

        UpdateModTextSections(lobbyInfo.requiredMods.Split('\n'), lobbyInfo.bannedMods.Split('\n'));
    }

    // NOTE: Taken from RainMeadow.RainMeadowModManager.CheckMods
    // I (Timbits) tried my own implementation but it always had some inaccuracy/bug in it.
    // I don't have the time or will to figure out why since the bug is so random and this works so whatever.
    // This has been refactored and trimmed, leaving the logic completely intact and untouched as far as I can tell.
    public void UpdateModTextSections(string[] requiredMods, string[] bannedMods)
    {
        highImpactModsTextScroller.RemoveAllButtons();

        if (requiredMods.Length == 1 && requiredMods.Contains("henpemaz_rainmeadow"))
        {
            highImpactModsTextScroller.Container.isVisible = false;
            return;
        }

        highImpactModsTextScroller.Container.isVisible = true;

        List<string> enabledModIds = ModManager.ActiveMods.ConvertAll(mod => mod.id);
        List<string> modIdsToEnable = [.. requiredMods.Except(enabledModIds)];
        List<string> modIdsToDisable =
        [
            .. RainMeadowModManager
                .GetRequiredMods()
                .Union(bannedMods)
                .Except(requiredMods)
                .Intersect(enabledModIds),
        ];

        modIdsToEnable.RemoveAll(string.IsNullOrEmpty);
        modIdsToDisable.RemoveAll(string.IsNullOrEmpty);

        List<bool> modsEnabledByIndex = ModManager.InstalledMods.ConvertAll(mod => mod.enabled);
        List<string> modNamesToEnable = [],
            modNamesToDisable = [],
            modsToInstall = [],
            missingDLCs = [];

        foreach (string id in modIdsToEnable)
        {
            int index = ModManager.InstalledMods.FindIndex(mod => mod.id == id);
            if (index == -1)
            {
                modsToInstall.Add(id);
                continue;
            }
            modsEnabledByIndex[index] = true;
            modNamesToEnable.Add(ModManager.InstalledMods[index].LocalizedName);
        }

        foreach (ModManager.Mod mod in ModManager.ActiveMods)
        {
            if (modIdsToDisable.Contains(mod.id))
                continue;
            if (modIdsToDisable.Exists(id => mod.requirements.Contains(id)))
                modIdsToDisable.Add(mod.id);
        }

        // this has to be paranoia
        modIdsToDisable.RemoveAll(string.IsNullOrEmpty);

        foreach (string id in modIdsToDisable)
        {
            int index = ModManager.InstalledMods.FindIndex(mod => mod.id == id);
            if (index == -1)
            {
                RainMeadow.Debug($"Couldn't find instance of {id} in InstalledMods??");
                continue;
            }

            modsEnabledByIndex[index] = false;
            modNamesToDisable.Add(ModManager.InstalledMods[index].LocalizedName);
        }

        // there's checking, there's redundancy, and then there's whatever this is
        modNamesToEnable.RemoveAll(string.IsNullOrEmpty);
        modNamesToDisable.RemoveAll(string.IsNullOrEmpty);
        modsToInstall.RemoveAll(string.IsNullOrEmpty);

        for (int i = 0; i < modsEnabledByIndex.Count; i++)
            if (modsEnabledByIndex[i] && ModManager.InstalledMods[i].DLCMissing)
                missingDLCs.Add(ModManager.InstalledMods[i].LocalizedName);

        void AddModTextSection(List<string> modNames, string headerText)
        {
            if (modNames.Count == 0)
                return;
            highImpactModsTextScroller.AddText(menu.Translate(headerText), true);
            highImpactModsTextScroller.AddText(modNames);
            highImpactModsTextScroller.AddBlankLine();
        }

        AddModTextSection(missingDLCs, "Missing DLCs");
        AddModTextSection(modsToInstall, "Mods to Install");
        AddModTextSection(modNamesToEnable, "Mods to Enable");
        AddModTextSection(modNamesToDisable, "Mods to Disable");

        List<string> installedModIds = ModManager.InstalledMods.ConvertAll(mod => mod.id);

        highImpactModsTextScroller.AddText(menu.Translate("Lobby Mods"), true);
        highImpactModsTextScroller.AddText(
            requiredMods
                .SkipWhile(string.IsNullOrEmpty)
                .Select(id =>
                    ModManager.InstalledMods.FirstOrDefault(mod => mod.id == id)?.LocalizedName
                    ?? id
                )
        );
    }

    public void ClearLobbyInfo()
    {
        lobbyNameLabel.text = menu.Translate("No Lobby Selected");
        lobbyNameLabel.pos.y = 460;
        gamemodeLabel.text = "";
        hasPasswordLabel.text = "";
        playerCountLabel.text = "";
        timelineIcon.ClearSprites();
        separator.Sprite.alpha = 0;
        highImpactModsTextScroller.RemoveAllButtons();
        highImpactModsTextScroller.Container.isVisible = false;
    }
}
