// HACK thrown together in a panic

using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static RainMeadow.RainMeadowModManager;
using static RainMeadow.LANMatchmakingManager;

namespace RainMeadow
{
    public class LobbySelectMenu : SmartMenu
    {
        private SimplerButton createButton;
        private OpComboBox2 filterModsDropDown;
        private OpComboBox2 domainDropDown;
        private OpComboBox2 filterModeDropDown;
        private OpCheckBox filterPublicLobbiesOnly;
        private OpTextBox filterLobbyLimit;
        private LobbyCardsList lobbyList;
        public LobbyInfo lastClickedLobby;
        private MenuDialogBox popupDialog;
        private MenuLabel statisticsLabel;

        public int playerCount;
        public int lobbyCount;

        public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;
        public LobbySelectMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbySelectMenu)
        {
            RainMeadow.DebugMe();

            this.backTarget = ProcessManager.ProcessID.MainMenu;

            lobbyList = new LobbyCardsList(this, mainPage, new Vector2(518, 100f), new Vector2(330f, 490f));
            lobbyList.RefreshButton.OnClick += RefreshLobbyList;
            mainPage.subObjects.Add(lobbyList);

            // title at the top
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "", "MeadowShadow", new Vector2(-2.99f, 265.01f), true, false));
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "", "MeadowTitle", new Vector2(-2.99f, 265.01f), true, false));
            this.scene.flatIllustrations[this.scene.flatIllustrations.Count - 1].sprite.shader = this.manager.rainWorld.Shaders["MenuText"];

            var creditsButton = new SimplerButton(this, mainPage, Translate("Credits"), new Vector2(1056f, 600f), new Vector2(110f, 30f));
            creditsButton.OnClick += (_) =>
            {
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.MeadowCredits);
                PlaySound(SoundID.MENU_Switch_Page_In);
            };
            mainPage.subObjects.Add(creditsButton);


            createButton = new SimplerButton(this, mainPage, Translate("CREATE!"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
            createButton.OnClick += (_) =>
            {
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbyCreateMenu);
                PlaySound(SoundID.MENU_Switch_Page_In);
            };
            mainPage.subObjects.Add(createButton);

            // // refresh button on lower left // P.S. I know we will probably re-align it later, I could not find an exact position that would satisfy my OCD, which usually means the alignment sucks.
            // refreshButton = new SimplerButton(this, mainPage, "REFRESH", new Vector2(315f, 50f), new Vector2(110f, 30f));
            // refreshButton.OnClick += RefreshLobbyList;
            // mainPage.subObjects.Add(refreshButton);

            // // 188 on mock -> 218 -> 768 - 218 = 550 -> 552
            // // misc buttons on topright
            // // currently greyed out since they server no purpose
            // Vector2 where = new Vector2(1056f, 552f);
            // var aboutButton = new SimplerButton(this, mainPage, Translate("ABOUT"), where, new Vector2(110f, 30f));
            // aboutButton.buttonBehav.greyedOut = true;

            // mainPage.subObjects.Add(aboutButton);
            // where.y -= 35;
            // var statsButton = new SimplerButton(this, mainPage, Translate("STATS"), where, new Vector2(110f, 30f));
            // statsButton.buttonBehav.greyedOut = true;
            // mainPage.subObjects.Add(statsButton);
            // where.y -= 35;
            // var unlocksButton = new SimplerButton(this, mainPage, Translate("UNLOCKS"), where, new Vector2(110f, 30f));
            // unlocksButton.buttonBehav.greyedOut = true;
            // mainPage.subObjects.Add(unlocksButton);

            // Status
            statisticsLabel = new MenuLabel(this, pages[0], $"Online: {playerCount} | Lobbies: {lobbyCount}", new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f + 20f, manager.rainWorld.screenSize.y - 768f + 20), new Vector2(200f, 20f), false, null);
            statisticsLabel.size = new Vector2(statisticsLabel.label.textRect.width, statisticsLabel.size.y);
            mainPage.subObjects.Add(statisticsLabel);

            // // display version
            MenuLabel versionLabel = new MenuLabel(this, pages[0], $"Rain Meadow Version: {RainMeadow.MeadowVersionStr}", new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f + 20f, manager.rainWorld.screenSize.y - 768f), new Vector2(200f, 20f), false, null);
            versionLabel.size = new Vector2(versionLabel.label.textRect.width, versionLabel.size.y);
            mainPage.subObjects.Add(versionLabel);

            // filters

            Vector2 where = new Vector2(300f, 400f);

            var filterModeLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Lobby Mode"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterModeLabel);
            where.y -= 27;
            filterModeDropDown = new OpComboBox2(new Configurable<LobbyCardsList.LobbyCardsFilter.GameModeFilter>(LobbyCardsList.LobbyCardsFilter.GameModeFilter.All), where, 160f, OpResourceSelector.GetEnumNames(null, typeof(LobbyCardsList.LobbyCardsFilter.GameModeFilter)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            filterModeDropDown.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, filterModeDropDown);
            where.y -= 30;
            var filterPublicLobbiesOnlyLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Public Lobbies Only"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterPublicLobbiesOnlyLabel);
            where.y -= 27;
            filterPublicLobbiesOnly = new OpCheckBox(new Configurable<bool>(false), where);
            filterPublicLobbiesOnly.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, filterPublicLobbiesOnly);
            where.y -= 30;
            var filterLobbyLimitLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Max Lobby Size"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterLobbyLimitLabel);
            where.y -= 27;
            filterLobbyLimit = new OpTextBox(new Configurable<int>(32), where, 50f);
            filterLobbyLimit.accept = OpTextBox.Accept.Int;
            filterLobbyLimit.maxLength = 2;
            filterLobbyLimit.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, filterLobbyLimit);

            where.y -= 30;
            var filterModsLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Lobby Mods"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterModsLabel);
            where.y -= 27;
            List<ListItem> requiredModsList = [new("Any", Translate("Unfiltered"), 0), new("Exact", Translate("Exact order"), 1), new("All", Translate("Any order"), Int32.MaxValue)];
            string[] requiredModIDs = RainMeadowModManager.GetRequiredMods();
            foreach (string id in requiredModIDs)
            { //adding Rain Meadow is quite redundant, so I'll leave it out.
                if (id != "henpemaz_rainmeadow") requiredModsList.Add(new ListItem(id, "+" + RainMeadowModManager.ModIdToName(id), requiredModsList.Count));
            }
            filterModsDropDown = new OpComboBox2(new Configurable<string>("Any"), where, 160f, requiredModsList) { colorEdge = MenuColorEffect.rgbWhite };
            filterModsDropDown.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, filterModsDropDown);

            //
            where = new Vector2(manager.rainWorld.screenSize.x - 320f , 400f);


            var directConnectButton = new SimplerButton(this, mainPage, Translate("Direct Connect"), new Vector2(where.x, where.y), new Vector2(160f, 30f));
            directConnectButton.OnClick += (_) =>
            {   
                if (MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN)
                {
                    ShowErrorDialog("Direct Connection is only available in the Local Matchmaker");
                    return;
                }
                ShowDirectConnectionDialogue();
            };

            where.y -= 30;
            var domainlabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Lobby Domain"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(domainlabel);
            where.y -= 27;


            mainPage.subObjects.Add(directConnectButton);
            
            domainDropDown = new OpComboBox2(new Configurable<MatchmakingManager.MatchMakingDomain>(
                MatchmakingManager.currentDomain), where, 160f - 35f, 
                MatchmakingManager.supported_matchmakers.Select(x => new ListItem(x.value)).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            domainDropDown.OnChange += () => {
                MatchmakingManager.currentDomain = new MatchmakingManager.MatchMakingDomain(domainDropDown.value, false);
                lobbyList.ClearLobbies();
                lobbyList.CreateCards();
                RefreshLobbyList(null);
            };




            new UIelementWrapper(this.tabWrapper, domainDropDown);
    

            // if (OnlineManager.currentlyJoiningLobby != default)
            // {
            //     ShowLoadingDialog("Joining lobby...");
            // }

            // // Lobby machine go!
            MatchmakingManager.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            MatchmakingManager.OnLobbyJoined += OnlineManager_OnLobbyJoined;
            if (MatchmakingManager.supported_matchmakers.Contains(MatchmakingManager.MatchMakingDomain.Steam)) {
                SteamNetworkingUtils.InitRelayNetworkAccess();
            }   
            
            MatchmakingManager.currentInstance.RequestLobbyList();

            manager.musicPlayer?.MenuRequestsSong("Establish", 1, 0);
        }

        public override void Update()
        {
            base.Update();

            // bool popupVisible = popupDialog != null;

            // for (int i = 0; i < lobbyButtons.Length; i++)
            // {
            //     lobbyButtons[i].buttonBehav.greyedOut = popupVisible;
            // }

            // filterModeDropDown.greyedOut = popupVisible;
            // filterPasswordDropDown.greyedOut = popupVisible;
            // filterLobbyLimit.greyedOut = popupVisible;
            // lobbySearchInputBox.greyedOut = popupVisible;

            // visibilityDropDown.greyedOut = popupVisible;
            // lobbyLimitNumberTextBox.greyedOut = popupVisible;
            // createButton.buttonBehav.greyedOut = popupVisible;
            // refreshButton.buttonBehav.greyedOut = popupVisible;
            // modeDropDown.greyedOut = popupVisible;
            // passwordInputBox.greyedOut = popupVisible;
            // enablePasswordCheckbox.buttonBehav.greyedOut = popupVisible;
            // upButton.buttonBehav.greyedOut = popupVisible;
            // downButton.buttonBehav.greyedOut = popupVisible;

            // if (popupVisible) return;

            // int extraItems = Mathf.Max(lobbies.Length - 4, 0);
            // scrollTo = Mathf.Clamp(scrollTo, -0.5f, extraItems + 0.5f);
            // if (scrollTo < 0) scrollTo = RWCustom.Custom.LerpAndTick(scrollTo, 0, 0.1f, 0.1f);
            // if (scrollTo > extraItems) scrollTo = RWCustom.Custom.LerpAndTick(scrollTo, extraItems, 0.1f, 0.1f);
            // scroll = RWCustom.Custom.LerpAndTick(scroll, scrollTo, 0.1f, 0.1f);

            // passwordInputBox.greyedOut = !setpassword;
            // if (lobbyLimitNumberTextBox.value != "" && !lobbyLimitNumberTextBox.held) ApplyLobbyLimit();

            // Statistics
            if (statisticsLabel != null)
            {
                statisticsLabel.text = $"Online: {playerCount} | Lobbies: {lobbyCount}";
                statisticsLabel.size = new Vector2(statisticsLabel.label.textRect.width, statisticsLabel.size.y);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {

            base.GrafUpdate(timeStacker);
        }

        private void UpdateLobbyFilter()
        {
            lobbyList.filter.gameMode = filterModeDropDown.value;
            lobbyList.filter.requiredMods = filterModsDropDown.value;
            lobbyList.filter.publicLobby = filterPublicLobbiesOnly.GetValueBool();

            lobbyList.FilterLobbies();
        }

        public bool VerifyPlay(LobbyInfo lobbyInfo, bool care_about_lobby_size = true) {
            domainDropDown.greyedOut = true;
            if (ModManager.JollyCoop)
            {
                ShowErrorDialog("Please disable JollyCoop before playing Online");
                return false;
            }
            lastClickedLobby = lobbyInfo;


            if (care_about_lobby_size) {
                MatchmakingManager.MAX_LOBBY = lobbyInfo.maxPlayerCount;
                if (lobbyInfo.playerCount >= lobbyInfo.maxPlayerCount)
                {
                    ShowErrorDialog("Failed to join lobby.<LINE> Lobby is full");
                    return false;
                }
            }

            return true;
        }

        public void Play(LobbyInfo lobbyInfo)
        {
            if (!VerifyPlay(lobbyInfo)) {
                return;
            }

            if (ModManager.JollyCoop)
            {
                ShowErrorDialog("Please disable JollyCoop before playing Online");
                return;
            }
            lastClickedLobby = lobbyInfo;

            if (lobbyInfo is LANMatchmakingManager.LANLobbyInfo) {
                RainMeadow.DebugMe();
                RainMeadow.Debug($"{lobbyInfo.name}, {lobbyInfo.maxPlayerCount}, {lobbyInfo.mode}, {lobbyInfo.playerCount}, {lobbyInfo.hasPassword}");
            }

            if (lobbyInfo.hasPassword)
            {
                ShowPasswordRequestDialog();
            }
            else
            {
                StartJoiningLobby(lobbyInfo);
            }
        }

        private void RefreshLobbyList(SymbolButton obj)
        {
            MatchmakingManager.currentInstance.RequestLobbyList();
        }

        public void StartJoiningLobby(LobbyInfo lobby, string? password = null, bool checkMods = true)
        {
            CheckMods(ModStringToArray(lobby.requiredMods), ModStringToArray(lobby.bannedMods),
                () => {
                    ShowLoadingDialog("Joining lobby...");
                    RequestLobbyJoin(lobby, password);
                }, false, password, (lobby is LANLobbyInfo lanLobby) ? lanLobby.endPoint : null);
        }
        public void RequestLobbyJoin(LobbyInfo lobby, string? password = null)
        {
            RainMeadow.DebugMe();
            MatchmakingManager.currentInstance.RequestJoinLobby(lobby, password);
        }

        private void OnlineManager_OnLobbyListReceived(bool ok, LobbyInfo[] lobbies)
        {
            RainMeadow.Debug(ok);
            if (ok)
            {
                lobbyList.allLobbies = lobbies.ToList();
                lobbyList.FilterLobbies();
                UpdateStats(lobbyList);
            }
        }

        private void UpdateStats(LobbyCardsList lobbyList)
        {
            playerCount = 0;
            lobbyList.allLobbies.ForEach(lobby =>
            {
                playerCount += lobby.playerCount;
            });
            lobbyCount = lobbyList.allLobbies.Count();
        }

        private void OnlineManager_OnLobbyJoined(bool ok, string error)
        {
            RainMeadow.Debug(ok);
            if (!ok)
            {
                ShowErrorDialog($"Failed to join lobby.<LINE>{error}");
            }
        }

        public override void ShutDownProcess()
        {
            MatchmakingManager.OnLobbyListReceived -= OnlineManager_OnLobbyListReceived;
            MatchmakingManager.OnLobbyJoined -= OnlineManager_OnLobbyJoined;
            base.ShutDownProcess();
        }

        public void GreyOutLobbyCards(bool greyedOut)
        {
            domainDropDown.greyedOut = greyedOut;
            lobbyList.lobbyCards.ForEach(card => card.buttonBehav.greyedOut = greyedOut);
        }

        public void ShowPasswordRequestDialog()
        {
            if (popupDialog != null) HideDialog();

            popupDialog = new CustomInputDialogueBox(this, mainPage, "Password Required", "HIDE_PASSWORD", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
            mainPage.subObjects.Add(popupDialog);

            GreyOutLobbyCards(true);
        }


        public void ShowDirectConnectionDialogue()
        {
            if (popupDialog != null) HideDialog();

            popupDialog = new DirectConnectionDialogue(this, mainPage,
                new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), 
                new Vector2(480f, 320f));
            mainPage.subObjects.Add(popupDialog);

            GreyOutLobbyCards(true);
        }

        public void ShowLoadingDialog(string text)
        {
            if (popupDialog != null) HideDialog();

            popupDialog = new DialogBoxAsyncWait(this, mainPage, text, new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
            mainPage.subObjects.Add(popupDialog);
        }

        public void ShowNotLocalDialogue(string text, Action ok)
        {
            if (popupDialog != null) HideDialog();
            
            popupDialog = new NotLocalWarningDialog(this, mainPage,
                new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), 
                new Vector2(480f, 320f), text, false, ok, () => HideDialog());
            mainPage.subObjects.Add(popupDialog);
            GreyOutLobbyCards(true);
        }

        public void ShowErrorDialog(string error)
        {
            if (popupDialog != null) HideDialog();

            popupDialog = new DialogBoxNotify(this, mainPage, error, "HIDE_DIALOG", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
            mainPage.subObjects.Add(popupDialog);
            GreyOutLobbyCards(true);
        }

        public void HideDialog()
        {
            if (popupDialog == null) return;

            mainPage.RemoveSubObject(popupDialog);
            popupDialog.RemoveSprites();
            popupDialog = null;

            GreyOutLobbyCards(false);
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);

            switch (message)
            {
                case "HIDE_DIALOG":
                    HideDialog();
                    break;
                case "HIDE_PASSWORD":
                    var password = (popupDialog as CustomInputDialogueBox).textBox.value;
                    StartJoiningLobby(lastClickedLobby, password);
                    break;
                case "DIRECT_JOIN": 
                    var dialogue = popupDialog as DirectConnectionDialogue;
                    var endpoint = UDPPeerManager.GetEndPointByName(dialogue?.IPBox?.value ?? "");
                    if (endpoint != null) {
                        var fakelobbyinfo = new LANMatchmakingManager.LANLobbyInfo(endpoint, "Direct Connection", "Meadow", 0, true, 2);
                        Action join = () => {
                            GreyOutLobbyCards(true);
                            StartJoiningLobby(fakelobbyinfo,
                                    dialogue.passwordCheckBox.Checked? dialogue.passwordBox.value : null,
                                    false);
                        };
                        
                        if (VerifyPlay(fakelobbyinfo))
                        if (!UDPPeerManager.isEndpointLocal(endpoint)) {
                            ShowNotLocalDialogue(
                                                "This address is possibly not local to your current network." + Environment.NewLine +
                                                "If so, This is very unstable and will most likely NOT work" + Environment.NewLine +
                                                "Are you SURE you know what you're doing?",
                                join);
                            mainPage.subObjects.Add(popupDialog);
                        } else join.Invoke();


                    } else {
                        ShowErrorDialog("Invalid Address, IP Address format should be xxx.xxx.xxx.xxx:port");
                    }
                    break;
            }
        }
    }
}
