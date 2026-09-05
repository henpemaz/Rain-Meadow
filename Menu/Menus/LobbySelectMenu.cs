using System.Net;
using Menu;
using RainMeadow.UI.Components;
using RainMeadow.UI.Dialogs;
using RainMeadow.UI.Menus.Panels;
using Steamworks;
using UnityEngine;

namespace RainMeadow.UI.Menus;

public class LobbySelectMenu : SmartMenu
{
    public LobbySelectMetadataPanel metadataPanel;
    public ProperlyAlignedMenuLabel statisticsLabel;
    public LobbyCardSelector lobbyCardSelector;
    public SimplerButton joinButton;
    public DialogAsyncWaitCancellable? joiningDialog;
    public LobbyInfo? lastSelectedLobbyInfo;

    private int joiningTimeoutCount = 0;
    private const int FastTimeoutCount = 200;
    private int TimeoutTicks = RainMeadow.rainMeadowOptions.JoiningTimeout.Value * 40;
    private const string ERROR_Unexpected = "Something went wrong...";
    private const string ERROR_Cancelled = "Cancelled";

    public override MenuScene.SceneID GetScene =>
        ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;

    public LobbySelectMenu(ProcessManager manager)
        : base(manager, RainMeadow.Ext_ProcessID.LobbySelectMenu)
    {
        RainMeadow.DebugMe();

        backTarget = ProcessManager.ProcessID.MainMenu;

        scene.AddIllustration(
            new MenuIllustration(
                this,
                scene,
                "illustrations/rainmeadowtitle",
                Utils.GetMeadowTitleFileName(true),
                new Vector2(-2.99f, 265.01f),
                true,
                false
            )
        );
        scene.AddIllustration(
            new MenuIllustration(
                this,
                scene,
                "illustrations/rainmeadowtitle",
                Utils.GetMeadowTitleFileName(false),
                new Vector2(-2.99f, 265.01f),
                true,
                false
            )
        );
        scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.shader = manager
            .rainWorld
            .Shaders["MenuText"];

        SimplerButton creditsButton = new(
            this,
            mainPage,
            Translate("CREDITS"),
            new Vector2(200, 660),
            new Vector2(110, 30),
            Translate("View Rain Meadow credits")
        );
        creditsButton.OnClick += (btn) =>
            manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.MeadowCredits);

        statisticsLabel = new ProperlyAlignedMenuLabel(
            this,
            mainPage,
            $"{Translate("Online:")} 0 | {Translate("Lobbies:")} 0",
            new Vector2(
                (1366f - manager.rainWorld.screenSize.x) / 2f + 5f,
                manager.rainWorld.screenSize.y - 768f + 20f
            ),
            Vector2.zero,
            false
        );

        ProperlyAlignedMenuLabel versionLabel = new(
            this,
            mainPage,
            $"{Translate("Rain Meadow Version:")} {RainMeadow.MeadowVersionStr}",
            new Vector2(
                (1366f - manager.rainWorld.screenSize.x) / 2f + 5f,
                manager.rainWorld.screenSize.y - 768f
            ),
            Vector2.zero,
            false
        );

        lobbyCardSelector = new LobbyCardSelector(this, mainPage, new Vector2(225, 115));
#pragma warning disable IDE0200
        // this forces RefreshLobbies to fetch the actual currentInstance instead of just forcefully pointing at the original currentInstance
        lobbyCardSelector.RefreshLobbies += () =>
            MatchmakingManager.currentInstance.RequestLobbyList();
#pragma warning restore IDE0200
        lobbyCardSelector.OnLobbyCardsUpdated += () =>
        {
            metadataPanel?.ClearLobbyInfo();
            lastSelectedLobbyInfo = null;
        };
        lobbyCardSelector.OnLobbyCardClicked += (lobbyInfo) =>
        {
            if (lastSelectedLobbyInfo == lobbyInfo)
                JoinLobbyChecks(lobbyInfo);
            metadataPanel?.UpdateLobbyInfo(lobbyInfo);
            lastSelectedLobbyInfo = lobbyInfo;
        };

        joinButton = new SimplerButton(
            this,
            mainPage,
            Translate("JOIN"),
            new Vector2(1056, 50),
            new Vector2(110, 30),
            Translate("Join selected lobby")
        );
        joinButton.buttonBehav.greyedOut = true;
        // button can't be clicked if SelectedLobby is null (see Update())
        joinButton.OnClick += (btn) => JoinLobbyChecks(lobbyCardSelector.SelectedLobby!);

        SimplerButton createButton = new(
            this,
            mainPage,
            Translate("CREATE!"),
            new Vector2(936, 50),
            new Vector2(110, 30),
            Translate("Create a new lobby")
        );
        createButton.OnClick += (btn) =>
        {
            manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbyCreateMenu);
            PlaySound(SoundID.MENU_Switch_Page_In);
        };

        TabContainer tabContainer = new(
            this,
            mainPage,
            new Vector2(830, 110),
            new Vector2(300, 500),
            true
        );

        TabContainer.Tab metadataTab = new(this, tabContainer);
        metadataPanel = new LobbySelectMetadataPanel(this, metadataTab, Vector2.zero);
        metadataTab.AddObjects(metadataPanel);
        tabContainer.AddTab(metadataTab, Translate("Selected Lobby"));

        TabContainer.Tab filterTab = new(this, tabContainer);
        LobbySelectFiltersPanel filtersPanel = new(this, filterTab, Vector2.zero);
        filtersPanel.OnFilterUpdated += lobbyCardSelector.UpdateFilters;
        filterTab.AddObjects(filtersPanel);
        tabContainer.AddTab(filterTab, Translate("Filters"));

        TabContainer.Tab networkTab = new(this, tabContainer);
        LobbySelectNetworkPanel networkPanel = new(this, networkTab, Vector2.zero);
        networkPanel.OnDirectConnectButtonClick += () =>
        {
            DirectConnectionDialog directConnectionDialog = new(manager, UIUtils.DIALOG_SIZE);
            directConnectionDialog.OnDirectConnectConfirm += DirectConnect;
            manager.ShowDialog(directConnectionDialog);
        };
        networkPanel.OnDomainChanged += () =>
        {
            lobbyCardSelector.ClearLobbyCards();
            MatchmakingManager.currentInstance.RequestLobbyList();
        };
        networkTab.AddObjects(networkPanel);
        tabContainer.AddTab(networkTab, Translate("Network"));

        mainPage.subObjects.AddRange([
            creditsButton,
            statisticsLabel,
            versionLabel,
            joinButton,
            createButton,
            lobbyCardSelector,
            tabContainer,
        ]);

        if (
            RainMeadow.rainMeadowOptions.GetLobbyMusic(out string song)
            && !string.IsNullOrEmpty(song)
        )
            manager.musicPlayer?.MenuRequestsSong(song, 1, 0);

        // Lobby machine go!
        MatchmakingManager.OnLobbyListReceived += OnLobbyListReceived;
        MatchmakingManager.OnLobbyJoined += OnLobbyJoined;

        if (
            MatchmakingManager.supported_matchmakers.Contains(
                MatchmakingManager.MatchMakingDomain.Steam
            )
        )
            SteamNetworkingUtils.InitRelayNetworkAccess();
        MatchmakingManager.currentInstance.RequestLobbyList();

        if (!string.IsNullOrEmpty(RainMeadow.NewVersionAvailable))
            manager.ShowDialog(new UpdateDialog(manager));
    }

    public void UpdateStats()
    {
        int playerCount = 0;
        foreach (LobbyInfo lobbyInfo in lobbyCardSelector.lobbyInfos)
            playerCount += lobbyInfo.playerCount;
        statisticsLabel.text =
            $"{Translate("Online:")} {playerCount} | {Translate("Lobbies:")} {lobbyCardSelector.lobbyInfos.Length}";
    }

    public void JoinLobbyChecks(LobbyInfo lobbyInfo)
    {
        MatchmakingManager.MAX_LOBBY = lobbyInfo.maxPlayerCount;
        if (lobbyInfo.maxPlayerCount <= lobbyInfo.playerCount)
        {
            manager.ShowDialog(
                new NotifyDialog(
                    manager,
                    "Failed to join lobby.<LINE>Lobby is full",
                    UIUtils.DIALOG_SIZE
                )
            );
            return;
        }

        if (lobbyInfo.hasPassword)
        {
            InputDialog passwordDialog = new(manager, "Password Required", UIUtils.DIALOG_SIZE);
            passwordDialog.OnConfirm += (password) => StartJoiningLobby(lobbyInfo, password);
            manager.ShowDialog(passwordDialog);
        }
        else
            StartJoiningLobby(lobbyInfo);
    }

    public void StartJoiningLobby(LobbyInfo lobbyInfo, string? password = null)
    {
        RainMeadowModManager.CheckMods(
            lobbyInfo.requiredMods.Split('\n'),
            lobbyInfo.bannedMods.Split('\n'),
            () => RequestJoinLobby(lobbyInfo, password),
            false,
            lobbyInfo.GetLobbyJoinCode(password)
        );
    }

    public void RequestJoinLobby(LobbyInfo lobbyInfo, string? password)
    {
        manager.ShowDialog(
            joiningDialog = new DialogAsyncWaitCancellable(
                this,
                Translate("Joining lobby..."),
                UIUtils.DIALOG_SIZE,
                (_) => MatchmakingManager.currentInstance.JoinLobby(false, ERROR_Cancelled)
            )
        );
        joiningTimeoutCount = 0;
        MatchmakingManager.currentInstance.RequestJoinLobby(lobbyInfo, password);
    }

    public void DirectConnect(string ip, string? password)
    {
        IPEndPoint? endpoint = UDPPeerManager.GetEndPointByName(ip);
        if (endpoint == null)
        {
            manager.ShowDialog(
                new NotifyDialog(
                    manager,
                    "Invalid Address, IP Address format should be xxx.xxx.xxx.xxx:port",
                    UIUtils.DIALOG_SIZE
                )
            );
            return;
        }

        LANMatchmakingManager.LANLobbyInfo fakeLobbyInfo = new(
            endpoint,
            "Direct Connection",
            "Meadow",
            0,
            true,
            2
        );

        if (UDPPeerManager.isEndpointLocal(endpoint))
        {
            RequestJoinLobby(fakeLobbyInfo, password);
            return;
        }

        NotLocalDialog notLocalDialog = new(manager);
        notLocalDialog.OnConfirm += () => RequestJoinLobby(fakeLobbyInfo, password);
        manager.ShowDialog(notLocalDialog);
    }

    public void OnLobbyListReceived(bool ok, LobbyInfo[] lobbies)
    {
        if (!ok)
        {
            RainMeadow.Warn("Lobby IO failure");
            return;
        }

        lobbyCardSelector.UpdateLobbyInfos(lobbies);
        UpdateStats();
    }

    public void OnLobbyJoined(bool ok, string error)
    {
        if (joiningDialog != null)
            manager.StopSideProcess(joiningDialog);

        if (ok)
            return;

        string errorMessage = "Failed to join lobby:<LINE>" + error;
        if (error != ERROR_Cancelled) manager.ShowDialog(new NotifyDialog(manager, errorMessage, UIUtils.DIALOG_SIZE));
        RainMeadow.Error(errorMessage);

        // Stop any process/menu switch when an error occur 
        if (OnlineManager.instance.manager._processSwitchQueue.Count > 0)
        {
            RainMeadow.Warn("Found process(es) in queue, clearing it!");
            OnlineManager.instance.manager._processSwitchQueue.Clear();
        }
    }

    public override void Update()
    {
        base.Update();
        joinButton.buttonBehav.greyedOut = lobbyCardSelector.SelectedLobby == null;
        if (joiningDialog is not null && OnlineManager.lobby is not null)
        {
            if (RainMeadow.rainMeadowOptions.JoiningExtraInfo.Value)
            {
                int attempts = OnlineManager.lobby.enumsChecked 
                    ? OnlineManager.lobby.joiningAttempts 
                    : OnlineManager.lobby.enumSyncAttempts;
                int step = !OnlineManager.lobby.enumsChecked
                    ? 0
                    : OnlineManager.lobby.isRequesting
                        ? 1
                        : 2;

                string text = Translate("Joining lobby...") + $" {step}/2";
                if (attempts > 2) text += "\n" + Translate("(Attempt <NUM>)").Replace("<NUM>", attempts.ToString());
                joiningDialog.SetText(text);
            }

            if (TimeoutTicks > 0)
            {
                if (OnlineManager.lobby.enumsChecked 
                && !OnlineManager.lobby.isRequesting 
                && TimeoutTicks - joiningTimeoutCount > FastTimeoutCount) // Something went wrong, you should've joined by now
                {
                    joiningTimeoutCount = TimeoutTicks - FastTimeoutCount; // Making the timeout shorter
                }

                if (++joiningTimeoutCount >= TimeoutTicks)
                {
                    if (OnlineManager.lobby.joiningEvent is not null)
                    {
                        RainMeadow.Warn("Joining process it taking too long, timing it out!");
                        OnlineManager.lobby.joiningEvent.Abort(); // safely timeout
                    }
                    else
                    {
                        RainMeadow.Error($"No process running? What's happening? Timing it out! (enumChecked <{OnlineManager.lobby.enumsChecked}>, isRequesting <{OnlineManager.lobby.isRequesting}>, isAvailable <{OnlineManager.lobby.isAvailable}>)");
                        MatchmakingManager.currentInstance.JoinLobby(false, ERROR_Unexpected);
                    }
                }
            }
        }
    }

    public override void ShutDownProcess()
    {
        RainMeadow.DebugMe();
        MatchmakingManager.OnLobbyListReceived -= OnLobbyListReceived;
        MatchmakingManager.OnLobbyJoined -= OnLobbyJoined;
        base.ShutDownProcess();
    }
}
