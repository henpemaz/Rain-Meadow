using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;

#if !LOCAL_P2P
using Steamworks;
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class LobbySelectMenu : SmartMenu, CheckBox.IOwnCheckBox
    {
        private List<FSprite> sprites;
        private LobbyInfoCard[] lobbyButtons;
        private LobbyInfoCard lastClickedLobbyButton;
        private LobbyInfo[] lobbies;
        private LobbyInfo[] displayLobbies;
        private LobbyFilter filter;
        private OpComboBox2 filterModeDropDown;
        private OpComboBox2 filterPasswordDropDown;
        private OpTextBox filterLobbyLimit;
        private OpTextBox lobbySearchInputBox;
        private float scroll;
        private float scrollTo;
        private OpComboBox2 visibilityDropDown;
        private OpTextBox lobbyLimitNumberTextBox;
        private SimplerButton createButton;
        private SimplerButton refreshButton;
        private OpComboBox2 modeDropDown;
        private ProperlyAlignedMenuLabel modeDescriptionLabel;
        private MenuDialogBox popupDialog;
        private bool setpassword;
        private OpTextBox passwordInputBox;
        private CheckBox enablePasswordCheckbox;
        private int maxPlayerCount;
        private EventfulScrollButton upButton;
        private EventfulScrollButton downButton;

        public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;
        public LobbySelectMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbySelectMenu)
        {
            RainMeadow.DebugMe();

            Vector2 buttonSize = new(130f, 30f);
            this.backTarget = ProcessManager.ProcessID.MainMenu;

            // title at the top
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "", "MeadowShadow", new Vector2(-2.99f, 265.01f), true, false));
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "", "MeadowTitle", new Vector2(-2.99f, 265.01f), true, false));
            this.scene.flatIllustrations[this.scene.flatIllustrations.Count - 1].sprite.shader = this.manager.rainWorld.Shaders["MenuText"];

            // 690 on mock -> 720 -> 768 - 720 = 48, placed at 50 so my mock has a +2 offset
            // play button at lower right
            createButton = new SimplerButton(this, mainPage, Translate("CREATE!"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
            createButton.OnClick += CreateLobby;
            mainPage.subObjects.Add(createButton);

            // refresh button on lower left // P.S. I know we will probably re-align it later, I could not find an exact position that would satisfy my OCD, which usually means the alignment sucks.
            refreshButton = new SimplerButton(this, mainPage, "REFRESH", new Vector2(315f, 50f), new Vector2(110f, 30f));
            refreshButton.OnClick += RefreshLobbyList;
            mainPage.subObjects.Add(refreshButton);

            // 188 on mock -> 218 -> 768 - 218 = 550 -> 552
            // misc buttons on topright
            // currently greyed out since they server no purpose
            Vector2 where = new Vector2(1056f, 552f);
            var aboutButton = new SimplerButton(this, mainPage, Translate("ABOUT"), where, new Vector2(110f, 30f));
            aboutButton.buttonBehav.greyedOut = true;

            mainPage.subObjects.Add(aboutButton);
            where.y -= 35;
            var statsButton = new SimplerButton(this, mainPage, Translate("STATS"), where, new Vector2(110f, 30f));
            statsButton.buttonBehav.greyedOut = true;
            mainPage.subObjects.Add(statsButton);
            where.y -= 35;
            var unlocksButton = new SimplerButton(this, mainPage, Translate("UNLOCKS"), where, new Vector2(110f, 30f));
            unlocksButton.buttonBehav.greyedOut = true;
            mainPage.subObjects.Add(unlocksButton);

            // center description
            where = new Vector2(555f, 557f);
            var modeLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Mode:"), where, new Vector2(200, 20f), false);
            mainPage.subObjects.Add(modeLabel);
            where.x += 80;
            modeDropDown = new OpComboBox2(new Configurable<OnlineGameMode.OnlineGameModeType>(OnlineGameMode.OnlineGameModeType.Meadow), where, 160, OpResourceSelector.GetEnumNames(null, typeof(OnlineGameMode.OnlineGameModeType)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            modeDropDown.OnChanged += UpdateModeDescription;
            new UIelementWrapper(this.tabWrapper, modeDropDown);
            where.x -= 80;

            where.y -= 35;
            modeDescriptionLabel = new ProperlyAlignedMenuLabel(this, mainPage, "", where, new Vector2(0, 20f), false);
            mainPage.subObjects.Add(modeDescriptionLabel);
            UpdateModeDescription();

            // center-low settings
            where.y -= 45;
            var visibilityLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Visibility:"), where, new Vector2(200, 20f), false);
            mainPage.subObjects.Add(visibilityLabel);
            where.x += 80;
            visibilityDropDown = new OpComboBox2(new Configurable<MatchmakingManager.LobbyVisibility>(MatchmakingManager.LobbyVisibility.Public), where, 160, OpResourceSelector.GetEnumNames(null, typeof(MatchmakingManager.LobbyVisibility)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            new UIelementWrapper(this.tabWrapper, visibilityDropDown);
            where.x -= 80;

            where.y -= 45;
            enablePasswordCheckbox = new CheckBox(this, mainPage, this, where, 60f, Translate("Enable Password:"), "SETPASSWORD", true);
            mainPage.subObjects.Add(enablePasswordCheckbox);
            // password setting
            where.x += 160;
            passwordInputBox = new OpTextBox(new Configurable<string>(""), where, 160f);
            passwordInputBox.accept = OpTextBox.Accept.StringASCII;
            passwordInputBox.description = "Lobby Password";
            passwordInputBox.label.text = "Password";
            new UIelementWrapper(this.tabWrapper, passwordInputBox);

            // textbox lobby limit option
            where.x -= 160;
            where.y -= 45;
            var limitNumberLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Player max:"), where, new Vector2(400, 20f), false);
            mainPage.subObjects.Add(limitNumberLabel);
            where.x += 80;
            where.y -= 5;
            lobbyLimitNumberTextBox = new OpTextBox(new Configurable<int>(maxPlayerCount = 4), where, 160f);
            lobbyLimitNumberTextBox.accept = OpTextBox.Accept.Int;
            lobbyLimitNumberTextBox.maxLength = 2;
            lobbyLimitNumberTextBox.description = "The Max of the Players for the Lobby (up to 32)";
            new UIelementWrapper(this.tabWrapper, lobbyLimitNumberTextBox);
            where.y += 5;

            // display version
            MenuLabel versionLabel = new MenuLabel(this, pages[0], $"Rain Meadow Version: {RainMeadow.MeadowVersionStr}", new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f + 20f, manager.rainWorld.screenSize.y - 768f), new Vector2(200f, 20f), false, null);
            versionLabel.size = new Vector2(versionLabel.label.textRect.width, versionLabel.size.y);
            mainPage.subObjects.Add(versionLabel);

            // filters
            filter = new LobbyFilter();
            var filterLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Filters"), new Vector2(600f, 305f), new Vector2(200f, 20f), true);
            mainPage.subObjects.Add(filterLabel);

            where = new Vector2(550f, 280f);
            var filterModeLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Lobby Mode"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterModeLabel);
            where.y -= 27;
            filterModeDropDown = new OpComboBox2(new Configurable<LobbyFilter.ModeFilter>(LobbyFilter.ModeFilter.All), where, 160f, OpResourceSelector.GetEnumNames(null, typeof(LobbyFilter.ModeFilter)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            filterModeDropDown.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, filterModeDropDown);
            where.y -= 30;
            var filterPasswordLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Requires Password"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterPasswordLabel);
            where.y -= 27;
            filterPasswordDropDown = new OpComboBox2(new Configurable<LobbyFilter.PasswordFilter>(LobbyFilter.PasswordFilter.All), where, 160f, OpResourceSelector.GetEnumNames(null, typeof(LobbyFilter.PasswordFilter)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            filterPasswordDropDown.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, filterPasswordDropDown);
            where.y -= 30;
            var filterLobbyLimitLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Max Lobby Size"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterLobbyLimitLabel);
            where.y -= 27;
            filterLobbyLimit = new OpTextBox(new Configurable<int>(32), where, 50f);
            filterLobbyLimit.accept = OpTextBox.Accept.Int;
            filterLobbyLimit.maxLength = 2;
            filterLobbyLimit.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, filterLobbyLimit);

            // left lobby selector
            // bg
            sprites = new();
            FSprite sprite = new FSprite("pixel") { x = 204 - (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, y = 137, anchorY = 0, scaleY = 444, color = MenuRGB(MenuColors.MediumGrey) };
            mainPage.Container.AddChild(sprite);
            sprites.Add(sprite);
            sprite = new FSprite("pixel") { x = 528 - (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, y = 137, anchorY = 0, scaleY = 444, color = MenuRGB(MenuColors.MediumGrey) };
            mainPage.Container.AddChild(sprite);
            sprites.Add(sprite);

            // buttons
            upButton = new EventfulScrollButton(this, mainPage, new(316, 581), 0, 100);
            mainPage.subObjects.Add(upButton);
            upButton.OnClick += (_) => scrollTo -= 1f;
            downButton = new EventfulScrollButton(this, mainPage, new(316, 113), 2, 100);
            mainPage.subObjects.Add(downButton);
            downButton.OnClick += (_) => scrollTo += 1f;

            // searchbar
            lobbySearchInputBox = new OpTextBox(new Configurable<string>(""), new Vector2(214f, 550f), 304f);
            lobbySearchInputBox.label.text = "Search Lobbies";
            lobbySearchInputBox.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, lobbySearchInputBox);

            // cards
            lobbies = new LobbyInfo[0];
            lobbyButtons = new LobbyInfoCard[1];
            CreateLobbyCards();
            // waiting for lobby data!

            if (OnlineManager.currentlyJoiningLobby != default)
            {
                ShowLoadingDialog("Joining lobby...");
            }

            // Lobby machine go!
            MatchmakingManager.instance.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            MatchmakingManager.instance.OnLobbyJoined += OnlineManager_OnLobbyJoined;
#if !LOCAL_P2P
            SteamNetworkingUtils.InitRelayNetworkAccess();
#endif
            MatchmakingManager.instance.RequestLobbyList();
        }

        private void UpdateModeDescription()
        {
            modeDescriptionLabel.text = Translate(OnlineGameMode.OnlineGameModeType.descriptions[new OnlineGameMode.OnlineGameModeType(modeDropDown.value)]);
        }

        public override void Update()
        {
            base.Update();

            bool popupVisible = popupDialog != null;

            for (int i = 0; i < lobbyButtons.Length; i++)
            {
                lobbyButtons[i].buttonBehav.greyedOut = popupVisible;
            }

            filterModeDropDown.greyedOut = popupVisible;
            filterPasswordDropDown.greyedOut = popupVisible;
            filterLobbyLimit.greyedOut = popupVisible;
            lobbySearchInputBox.greyedOut = popupVisible;

            visibilityDropDown.greyedOut = popupVisible;
            lobbyLimitNumberTextBox.greyedOut = popupVisible;
            createButton.buttonBehav.greyedOut = popupVisible;
            refreshButton.buttonBehav.greyedOut = popupVisible;
            modeDropDown.greyedOut = popupVisible;
            passwordInputBox.greyedOut = popupVisible;
            enablePasswordCheckbox.buttonBehav.greyedOut = popupVisible;
            upButton.buttonBehav.greyedOut = popupVisible;
            downButton.buttonBehav.greyedOut = popupVisible;

            if (popupVisible) return;

            int extraItems = Mathf.Max(lobbies.Length - 4, 0);
            scrollTo = Mathf.Clamp(scrollTo, -0.5f, extraItems + 0.5f);
            if (scrollTo < 0) scrollTo = RWCustom.Custom.LerpAndTick(scrollTo, 0, 0.1f, 0.1f);
            if (scrollTo > extraItems) scrollTo = RWCustom.Custom.LerpAndTick(scrollTo, extraItems, 0.1f, 0.1f);
            scroll = RWCustom.Custom.LerpAndTick(scroll, scrollTo, 0.1f, 0.1f);

            passwordInputBox.greyedOut = !setpassword;
            if (lobbyLimitNumberTextBox.value != "" && !lobbyLimitNumberTextBox.held) ApplyLobbyLimit();
        }

        private void ApplyLobbyLimit()
        {
            maxPlayerCount = lobbyLimitNumberTextBox.valueInt;
            if (lobbyLimitNumberTextBox.valueInt > 32) lobbyLimitNumberTextBox.valueInt = 32;
            if (lobbyLimitNumberTextBox.valueInt < 2) lobbyLimitNumberTextBox.valueInt = 2;
        }

        private void CreateLobbyCards()
        {
            foreach (var btn in lobbyButtons)
            {
                if (btn == null) continue;
                btn.RemoveSprites();
                mainPage.RemoveSubObject(btn);
            }

            FilterLobbyCards();

            lobbyButtons = new LobbyInfoCard[displayLobbies.Length];

            for (int i = 0; i < displayLobbies.Length; i++)
            {
                var lobby = displayLobbies[i];

                var btn = new LobbyInfoCard(this, mainPage, CardPosition(i), new Vector2(304f, 60f), i, lobby, $"Click to join {lobby.name}");
                btn.OnClick += Play;
                mainPage.subObjects.Add(btn);
                lobbyButtons[i] = btn;
            }
        }

        private Vector2 CardPosition(int i)
        {
            Vector2 rootPos = new(214f, 412f);
            Vector2 offset = new(0, 70f);
            return rootPos - (scroll + i - 1) * offset;
        }

        private class LobbyInfoCard : SimplerButton
        {
            public LobbyInfo lobbyInfo;
            public int buttonArrayIndex;
            public LobbyInfoCard(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, int buttonArrayIndex, LobbyInfo lobbyInfo, string description = "") : base(menu, owner, "", pos, size, description)
            {
                this.lobbyInfo = lobbyInfo;
                this.buttonArrayIndex = buttonArrayIndex;
                this.menuLabel.RemoveSprites();
                this.RemoveSubObject(menuLabel);
                this.menuLabel = new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.name, new(5, 30), new(10, 50), true);
                this.subObjects.Add(this.menuLabel);
                if (lobbyInfo.hasPassword)
                {
                    this.subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, "Private", new(260, 20), new(10, 50), false));
                }
                this.subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, $"{lobbyInfo.maxPlayerCount} max", new(260, 5), new(10, 50), false));
                RainMeadow.Debug($"{lobbyInfo.name} card has {lobbyInfo.maxPlayerCount} max");
                this.subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.mode, new(5, 20), new(10, 50), false));
                this.subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.playerCount + " player" + (lobbyInfo.playerCount == 1 ? "" : "s"), new(5, 5), new(10, 50), false));
            }

            public override void Update()
            {
                base.Update();
                pos = (menu as LobbySelectMenu).CardPosition(this.buttonArrayIndex);
            }
        }

        private void FilterLobbyCards()
        {
            var targetLobbies = new List<LobbyInfo>();

            for (int i = 0; i < lobbies.Length; i++)
            {
                var lobby = lobbies[i];

                if (filter.lobbyName != "" && !lobby.name.ToLower().Contains(filter.lobbyName)) continue;
                if (filter.mode != "All" && lobby.mode != filter.mode) continue;
                if (filter.password == "NoPassword" && lobby.hasPassword) continue;
                if (filter.password == "Password" && !lobby.hasPassword) continue;
                if (filter.playerLimit < lobby.maxPlayerCount) continue;

                targetLobbies.Add(lobby);
            }

            displayLobbies = targetLobbies.ToArray();
        }

        private void UpdateLobbyFilter()
        {
            filter.mode = filterModeDropDown.value;
            filter.password = filterPasswordDropDown.value;
            filter.playerLimit = filterLobbyLimit.valueInt;
            filter.lobbyName = lobbySearchInputBox.value.ToLower();

            CreateLobbyCards();
        }

        private class LobbyFilter
        {
            public bool enabled;
            public string mode;
            public string password;
            public int playerLimit;
            public string lobbyName;
            public enum ModeFilter
            {
                All,
                Meadow,
                Story,
                ArenaCompetitive
            };
            public enum PasswordFilter
            {
                [Description("Show All")]
                All,
                [Description("No")]
                NoPassword,
                [Description("Yes")]
                Password,
            }

            public LobbyFilter()
            {
                this.enabled = false;
                this.mode = "All";
                this.password = "All";
                this.playerLimit = 32;
                this.lobbyName = "";
            }
        }

        private void Play(SimplerButton obj)
        {
            if (obj is not LobbyInfoCard infoCard)
            {
                ShowErrorDialog("Play was called by something other than a lobby card");
                return;
            }
            if (ModManager.JollyCoop)
            {
                ShowErrorDialog("Please disable JollyCoop before playing Online");
                return;
            }


            lastClickedLobbyButton = infoCard;

            var lobbyInfo = infoCard.lobbyInfo;
            MatchmakingManager.MAX_LOBBY = lobbyInfo.maxPlayerCount;
            if (lobbyInfo.playerCount >= lobbyInfo.maxPlayerCount)
            {
                ShowErrorDialog("Failed to join lobby.<LINE> Lobby is full");
            }
            else
            {
                if (lobbyInfo.hasPassword)
                {
                    ShowPasswordRequestDialog();
                }
                else
                {
                    ShowLoadingDialog("Joining lobby...");
                    RequestLobbyJoin(lobbyInfo);
                }
            }
        }

        private void CreateLobby(SimplerButton obj)
        {
            if (ModManager.JollyCoop)
            {
                ShowErrorDialog("Please disable JollyCoop before playing Online");
                return;
            }

            ShowLoadingDialog("Creating lobby...");
            ApplyLobbyLimit();
            RainMeadow.Debug($"Creating a lobby with a max player limit of {maxPlayerCount}");
            RequestLobbyCreate();
        }

        private void RefreshLobbyList(SimplerButton obj)
        {
            MatchmakingManager.instance.RequestLobbyList();
        }

        private void RequestLobbyCreate()
        {
            RainMeadow.DebugMe();
            Enum.TryParse<MatchmakingManager.LobbyVisibility>(visibilityDropDown.value, out var value);
            MatchmakingManager.instance.CreateLobby(value, modeDropDown.value, setpassword ? passwordInputBox.value : null, maxPlayerCount);
        }

        private void RequestLobbyJoin(LobbyInfo lobby, string? password = null)
        {
            RainMeadow.DebugMe();
            maxPlayerCount = lobby.maxPlayerCount;
            MatchmakingManager.instance.RequestJoinLobby(lobby, password);
        }

        private void OnlineManager_OnLobbyListReceived(bool ok, LobbyInfo[] lobbies)
        {
            RainMeadow.Debug(ok);
            if (ok)
            {
                this.lobbies = lobbies;
                CreateLobbyCards();
            }
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
            MatchmakingManager.instance.OnLobbyListReceived -= OnlineManager_OnLobbyListReceived;
            MatchmakingManager.instance.OnLobbyJoined -= OnlineManager_OnLobbyJoined;
            base.ShutDownProcess();
        }

        public void ShowPasswordRequestDialog()
        {
            if (popupDialog != null) HideDialog();

            popupDialog = new CustomInputDialogueBox(this, mainPage, "Password Required", "HIDE_PASSWORD", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
            mainPage.subObjects.Add(popupDialog);
        }

        public void ShowLoadingDialog(string text)
        {
            if (popupDialog != null) HideDialog();

            popupDialog = new DialogBoxAsyncWait(this, mainPage, text, new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
            mainPage.subObjects.Add(popupDialog);
        }

        public void ShowErrorDialog(string error)
        {
            if (popupDialog != null) HideDialog();

            popupDialog = new DialogBoxNotify(this, mainPage, error, "HIDE_DIALOG", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
            mainPage.subObjects.Add(popupDialog);
        }

        public void HideDialog()
        {
            if (popupDialog == null) return;

            mainPage.RemoveSubObject(popupDialog);
            popupDialog.RemoveSprites();
            popupDialog = null;
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
                    ShowLoadingDialog("Joining lobby...");
                    RequestLobbyJoin(lastClickedLobbyButton.lobbyInfo, password);
                    break;
                case "CLOSE_DIALOG":
                    HideDialog();
                    break;
            }
        }

        public bool GetChecked(CheckBox box)
        {
            switch (box.IDString)
            {
                case "SETPASSWORD":
                    return setpassword;
                default:
                    break;
            }
            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            switch (box.IDString)
            {
                case "SETPASSWORD":
                    setpassword = !setpassword;
                    break;
                default:
                    break;
            }
        }
    }
}
