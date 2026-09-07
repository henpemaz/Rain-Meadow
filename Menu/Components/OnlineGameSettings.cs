using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Configurables;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;

namespace RainMeadow.UI.Components;

public class GameSettings : OnlineSlugcatSettings<GameSettings>
{
    public const string SCORING = "Scoring", DENS = "Dens", IMPORTEXPORT = "Import & Export";
    public override string Name => "Game Settings";

    private OnlineSettingIntValue? spearHitScoreSetting;
    private OnlineSettingButtons? playlistButtons;
    private OnlineSettingButtons? settingsButtons;

    static GameSettings()
    {
        AddSlugcatSettingsConfigurable(new(
            "Food Score",
            SCORING,
            RainMeadow.rainMeadowOptions.ArenaFoodScore,
            nameof(ArenaOnlineGameMode.foodScore),
            "Food points multiplier")
        );
        AddSlugcatSettingsConfigurable(new(
            "Spear Hit Score",
            SCORING,
            RainMeadow.rainMeadowOptions.ArenaSpearHitScore,
            nameof(ArenaOnlineGameMode.spearHitScore),
            "Points a spear is worth (non-lethal)")
        );
        AddSlugcatSettingsConfigurable(new(
            "Kill Score",
            SCORING,
            RainMeadow.rainMeadowOptions.ArenaKillScore,
            nameof(ArenaOnlineGameMode.killScore),
            "Points a kill is worth")
        );
        AddSlugcatSettingsConfigurable(new(
            "Survival Score",
            SCORING,
            RainMeadow.rainMeadowOptions.ArenaSurvivalScore,
            nameof(ArenaOnlineGameMode.survivalScore),
            "Points for surviving inside the shelter")
        );
        AddSlugcatSettingsConfigurable(new(
            "Empty Death Score",
            SCORING,
            RainMeadow.rainMeadowOptions.ArenaEmptyDeathScore,
            nameof(ArenaOnlineGameMode.emptyDeathScore),
            "Points lost from self-inflicted death")
        );
        AddSlugcatSettingsConfigurable(new(
            "Unlock Dens",
            DENS,
            RainMeadow.rainMeadowOptions.ArenaDenScore,
            nameof(ArenaOnlineGameMode.denScore),
            "Points required to unlock dens")
        );
        AddSlugcatSettingsConfigurable(new(
            "Den Entry",
            DENS,
            RainMeadow.rainMeadowOptions.ArenaDenType,
            nameof(ArenaOnlineGameMode.denEntryRule),
            "Den entry behavior")
        );
        AddSlugcatSettingsConfigurable(new(
            "Den Ejection",
            DENS,
            RainMeadow.rainMeadowOptions.ChallengeDenEjection,
            nameof(ArenaOnlineGameMode.challengeDenEjection),
            "Dens eject and block players after some time")
        );

        AddSlugcatSettingsTab(new(IMPORTEXPORT, Color.gray));
    }

    public GameSettings(Menu.Menu menu, MenuObject owner) : base(menu, owner, 2f)
    {
        ConfigurableBase[] intConfigurables =
        [
            RainMeadow.rainMeadowOptions.ArenaFoodScore,
            RainMeadow.rainMeadowOptions.ArenaSpearHitScore,
            RainMeadow.rainMeadowOptions.ArenaKillScore,
            RainMeadow.rainMeadowOptions.ArenaSurvivalScore,
            RainMeadow.rainMeadowOptions.ArenaEmptyDeathScore,
            RainMeadow.rainMeadowOptions.ArenaDenScore,
        ];
        foreach (ConfigurableBase intConfigurable in intConfigurables)
        {
            if (GetSettingParameter(intConfigurable) is OnlineSettingIntValue intValue)
            {
                intValue.textBox.OnValueUpdate += (config, value, oldValue) =>
                {
                    if (intValue.textBox.valueInt < 0) intValue.textBox.valueInt = 0;
                };
            }
        }

        spearHitScoreSetting = GetSettingParameter(RainMeadow.rainMeadowOptions.ArenaSpearHitScore) as OnlineSettingIntValue;

        if (GetSettingParameter(RainMeadow.rainMeadowOptions.ChallengeDenEjection) is OnlineSettingCheckBox denEjection)
        {
            denEjection.altDescription = menu.Translate("Normal den behavior");
        }

        AddImportExportSection();
    }

    private void AddImportExportSection()
    {
        OnlineSettingTab? tab = GetSettingTab(IMPORTEXPORT);

        playlistButtons = new OnlineSettingButtons(menu, this, tab, "Playlist:",
            new("Copy", "Copy the level playlist to your clipboard", false, ExportPlaylist),
            new("Import", "Load a level playlist from your clipboard", true, ImportPlaylist));

        settingsButtons = new OnlineSettingButtons(menu, this, tab, "Settings:",
            new("Copy", "Copy the match settings to your clipboard", false, ExportSettings),
            new("Import", "Load match settings from your clipboard", true, ImportSettings));

        int insertAt = tab is not null ? elements.IndexOf(tab) + 1 : elements.Count;
        elements.Insert(insertAt, playlistButtons);
        elements.Insert(insertAt + 1, settingsButtons);
        this.SafeAddSubobjects(playlistButtons, settingsButtons);

        UpdateElementsPosition();
        playlistButtons.HardSetPosition(playlistButtons.WantedPosition);
        settingsButtons.HardSetPosition(settingsButtons.WantedPosition);
    }

    private void ExportPlaylist(OnlineSettingButtons row)
    {
        try
        {
            var arenaMenu = menu as ArenaOnlineLobbyMenu;
            string result = OnlineArenaBaseGameModeTab.EncodePlaylist(arenaMenu?.arenaMainLobbyPage?.levelSelector?.SelectedPlayList);
            if (string.IsNullOrEmpty(result))
            {
                row.ShowMessage(menu.Translate("Failed"), Color.red);
                return;
            }
            GUIUtility.systemCopyBuffer = result;
            row.ShowMessage(menu.Translate("Copied"), Color.green);
        }
        catch (Exception e)
        {
            RainMeadow.Error(e);
            row.ShowMessage(menu.Translate("Failed"), Color.red);
        }
    }

    private void ImportPlaylist(OnlineSettingButtons row)
    {
        try
        {
            var arenaMenu = menu as ArenaOnlineLobbyMenu;
            ArenaLevelSelector? levelSelector = arenaMenu?.arenaMainLobbyPage?.levelSelector;
            string clipboardText = GUIUtility.systemCopyBuffer;

            if (string.IsNullOrEmpty(clipboardText) || levelSelector == null)
            {
                row.ShowMessage(menu.Translate("Failed"), Color.red);
                return;
            }

            List<string> playlist = OnlineArenaBaseGameModeTab.DecodePlaylist(clipboardText)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            if (playlist.Count == 0 || playlist.Any(name => name.Contains("=") || name.Contains("|")))
            {
                row.ShowMessage(menu.Translate("Failed"), Color.red);
                return;
            }

            List<string> knownLevels = playlist.Where(levelSelector.allLevels.Contains).ToList();
            if (knownLevels.Count == 0)
            {
                row.ShowMessage(menu.Translate("Failed"), Color.red);
                return;
            }

            levelSelector.LoadNewPlaylist(knownLevels, true);
            levelSelector.selectedLevelsPlaylist.ResolvePlaylistMismatch();
            menu.PlaySound(SoundID.MENU_Add_Level);

            bool importedAll = knownLevels.Count == playlist.Count;
            row.ShowMessage(menu.Translate(importedAll ? "Imported" : "Missing levels"), importedAll ? Color.green : Color.yellow);
        }
        catch (Exception e)
        {
            RainMeadow.Error(e);
            row.ShowMessage(menu.Translate("Failed import"), Color.red);
        }
    }

    private void ExportSettings(OnlineSettingButtons row)
    {
        try
        {
            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arena)) return;
            string result = arena.externalArenaGameMode.ExportLocalSettings(arena);
            GUIUtility.systemCopyBuffer = result;
            row.ShowMessage(menu.Translate("Copied"), Color.green);
        }
        catch (Exception e)
        {
            RainMeadow.Error(e);
            row.ShowMessage(menu.Translate("Failed"), Color.red);
        }
    }

    private void ImportSettings(OnlineSettingButtons row)
    {
        try
        {
            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arena)) return;
            var arenaMenu = menu as ArenaOnlineLobbyMenu;
            string clipboardText = GUIUtility.systemCopyBuffer;

            if (string.IsNullOrEmpty(clipboardText)) return;

            bool success = arena.externalArenaGameMode.ImportLocalSettings(arena, clipboardText);
            if (!success)
            {
                row.ShowMessage(menu.Translate("Failed"), Color.red);
                return;
            }

            var settingsInterface = arenaMenu?.arenaMainLobbyPage?.arenaSettingsInterface;
            if (settingsInterface != null)
            {
                settingsInterface.countdownTimerTextBox.valueInt = arena.setupTime;
                settingsInterface.CallForSync();
            }

            row.ShowMessage(menu.Translate("Imported"), Color.green);
        }
        catch (Exception e)
        {
            RainMeadow.Error(e);
            row.ShowMessage(menu.Translate("Failed"), Color.red);
        }
    }

    public override void Update()
    {
        base.Update();

        if (!ModManager.MSC) spearHitScoreSetting?.grayedOut = true;
    }

    public override void SelectAndCreateBackButtons(SettingsPage? previousSettingPage, bool forceSelectedObject)
    {
        if (resetButton is null)
        {
            resetButton = new(menu, this, menu.Translate("RESET"), new(settingsBoxSize.x - 40, 20), new(80, 30));
            resetButton.OnClick += (b) => ResetSettings();
            AddObjects(resetButton);
        }

        BindSettingsButtons(IsActuallyHidden);
        if (forceSelectedObject) menu.selectedObject = elements.FirstOrDefault()?.selectable ?? resetButton;
    }
}
