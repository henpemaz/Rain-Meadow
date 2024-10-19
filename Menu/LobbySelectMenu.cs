using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;

#if !LOCAL_P2P
using Steamworks;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class LobbySelectMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner, CheckBox.IOwnCheckBox
    {
        private List<FSprite> sprites;
        private EventfulSelectOneButton[] lobbyButtons;
        private LobbyInfo[] lobbies;
        private float scroll;
        private float scrollTo;
        private int currentlySelectedCard;
        private OpComboBox2 visibilityDropDown;
        private OpTextBox lobbyLimitNumberTextBox;
        private SimplerButton playButton;
        private SimplerButton refreshButton;
        private OpComboBox2 modeDropDown;
        private ProperlyAlignedMenuLabel modeDescriptionLabel;
        private MenuDialogBox popupDialog;
        private bool setpassword;
        private OpTextBox passwordInputBox;
        private CheckBox enablePasswordCheckbox;
        private int maxPlayerCount;

        public override MenuScene.SceneID GetScene => MenuScene.SceneID.Landscape_CC;
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
            playButton = new SimplerButton(this, mainPage, Translate("PLAY!"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
            playButton.OnClick += Play;
            mainPage.subObjects.Add(playButton);

            // refresh button on lower left // P.S. I know we will probably re-align it later, I could not find an exact position that would satisfy my OCD, which usually means the alignment sucks.
            refreshButton = new SimplerButton(this, mainPage, "REFRESH", new Vector2(315f, 50f), new Vector2(110f, 30f));
            refreshButton.OnClick += RefreshLobbyList;
            mainPage.subObjects.Add(refreshButton);

            // 188 on mock -> 218 -> 768 - 218 = 550 -> 552
            // misc buttons on topright
            Vector2 where = new Vector2(1056f, 552f);
            var aboutButton = new SimplerButton(this, mainPage, Translate("ABOUT"), where, new Vector2(110f, 30f));

            mainPage.subObjects.Add(aboutButton);
            where.y -= 35;
            var statsButton = new SimplerButton(this, mainPage, Translate("STATS"), where, new Vector2(110f, 30f));
            mainPage.subObjects.Add(statsButton);
            where.y -= 35;
            var unlocksButton = new SimplerButton(this, mainPage, Translate("UNLOCKS"), where, new Vector2(110f, 30f));
            mainPage.subObjects.Add(unlocksButton);

            // center description
            where = new Vector2(555f, 557f);
            var modeLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Mode:"), where, new Vector2(200, 20f), false, null);
            mainPage.subObjects.Add(modeLabel);
            where.x += 80;
            modeDropDown = new OpComboBox2(new Configurable<OnlineGameMode.OnlineGameModeType>(OnlineGameMode.OnlineGameModeType.Meadow), where, 160, OpResourceSelector.GetEnumNames(null, typeof(OnlineGameMode.OnlineGameModeType)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            modeDropDown.OnChanged += UpdateModeDescription;
            new UIelementWrapper(this.tabWrapper, modeDropDown);
            where.x -= 80;

            where.y -= 35;
            modeDescriptionLabel = new ProperlyAlignedMenuLabel(this, mainPage, "", where, new Vector2(0, 20f), false, null);
            mainPage.subObjects.Add(modeDescriptionLabel);
            UpdateModeDescription();

            // center-low settings
            where.y -= 45;
            var visibilityLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Visibility:"), where, new Vector2(200, 20f), false, null);
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
            var limitNumberLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Player max:"), where, new Vector2(400, 20f), false, null);
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
            var upButton = new EventfulScrollButton(this, mainPage, new(316, 581), 0, 100);
            mainPage.subObjects.Add(upButton);
            upButton.OnClick += (_) => scrollTo -= 1f;
            var downButton = new EventfulScrollButton(this, mainPage, new(316, 113), 2, 100);
            mainPage.subObjects.Add(downButton);
            downButton.OnClick += (_) => scrollTo += 1f;

            // cards
            lobbies = new LobbyInfo[0];
            lobbyButtons = new EventfulSelectOneButton[1];
            lobbyButtons[0] = new EventfulSelectOneButton(this, mainPage, Translate("CREATE NEW LOBBY"), "lobbyCards", new(214, 530), new(304, 40), lobbyButtons, 0);
            lobbyButtons[0].OnClick += BumpPlayButton;
            mainPage.subObjects.Add(lobbyButtons[0]);
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
            int extraItems = Mathf.Max(lobbies.Length - 4, 0);
            scrollTo = Mathf.Clamp(scrollTo, -0.5f, extraItems + 0.5f);
            if (scrollTo < 0) scrollTo = RWCustom.Custom.LerpAndTick(scrollTo, 0, 0.1f, 0.1f);
            if (scrollTo > extraItems) scrollTo = RWCustom.Custom.LerpAndTick(scrollTo, extraItems, 0.1f, 0.1f);
            scroll = RWCustom.Custom.LerpAndTick(scroll, scrollTo, 0.1f, 0.1f);

            modeDropDown.greyedOut = this.currentlySelectedCard != 0;
            visibilityDropDown.greyedOut = this.currentlySelectedCard != 0;
            passwordInputBox.greyedOut = !setpassword || this.currentlySelectedCard != 0;
            enablePasswordCheckbox.buttonBehav.greyedOut = this.currentlySelectedCard != 0;
            lobbyLimitNumberTextBox.greyedOut = this.currentlySelectedCard != 0;
            playButton.menuLabel.text = (this.currentlySelectedCard == 0) ? "CREATE!" : "JOIN!";
            if (lobbyLimitNumberTextBox.value != "" && !lobbyLimitNumberTextBox.held) ApplyLobbyLimit();
        }

        private void ApplyLobbyLimit()
        {
            maxPlayerCount = lobbyLimitNumberTextBox.valueInt;
            if (lobbyLimitNumberTextBox.valueInt > 32) lobbyLimitNumberTextBox.valueInt = 32;
            if (lobbyLimitNumberTextBox.valueInt < 2) lobbyLimitNumberTextBox.valueInt = 2;
        }

        private void BumpPlayButton(EventfulSelectOneButton obj)
        {
            playButton.buttonBehav.flash = 1f;
            playButton.buttonBehav.col = 1f;
            playButton.buttonBehav.sizeBump = 1f;
        }

        private void CreateLobbyCards()
        {
            var oldLobbyButtons = lobbyButtons;
            for (int i = 1; i < oldLobbyButtons.Length; i++) // skips newlobby
            {
                var btn = oldLobbyButtons[i];
                btn.RemoveSprites();
                mainPage.RemoveSubObject(btn);
            }

            lobbyButtons = new EventfulSelectOneButton[1 + lobbies.Length];
            lobbyButtons[0] = oldLobbyButtons[0];

            for (int i = 0; i < lobbies.Length; i++)
            {
                var lobby = lobbies[i];
                var btn = new LobbyInfoCard(this, mainPage, CardPosition(i + 1), new(304, 60), lobbyButtons, i + 1, lobby);
                btn.OnClick += BumpPlayButton;
                mainPage.subObjects.Add(btn);
                lobbyButtons[i + 1] = btn;
            }
        }

        private Vector2 CardPosition(int i)
        {
            Vector2 rootPos = new(214, 460);
            Vector2 offset = new(0, 70);
            return rootPos - (scroll + i - 1) * offset;
        }

        private class LobbyInfoCard : EventfulSelectOneButton
        {
            public LobbyInfo lobbyInfo;
            public LobbyInfoCard(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, SelectOneButton[] buttonArray, int buttonArrayIndex, LobbyInfo lobbyInfo) : base(menu, owner, "", "lobbyCards", pos, size, buttonArray, buttonArrayIndex)
            {
                this.lobbyInfo = lobbyInfo;
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

            // todo out of view
        }

        private void Play(SimplerButton obj)
        {
            if (ModManager.JollyCoop)
            {
                ShowErrorDialog("Please disable JollyCoop before playing Online");
                return;

            }

            if (currentlySelectedCard == 0)
            {
                ShowLoadingDialog("Creating lobby...");
                ApplyLobbyLimit();
                RainMeadow.Debug($"Creating a lobby with a max player limit of {maxPlayerCount}");
                RequestLobbyCreate();
            }
            else
            {
                var lobbyInfo = (lobbyButtons[currentlySelectedCard] as LobbyInfoCard).lobbyInfo;
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
            var lobbyInfo = (lobbyButtons[currentlySelectedCard] as LobbyInfoCard).lobbyInfo;
            RainMeadow.DebugMe();
            maxPlayerCount = lobbyInfo.maxPlayerCount;
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

        // SelectOneButton.SelectOneButtonOwner
        public int GetCurrentlySelectedOfSeries(string series)
        {
            if (series == "lobbyCards") return currentlySelectedCard;
            return 0;
        }

        // SelectOneButton.SelectOneButtonOwner
        public void SetCurrentlySelectedOfSeries(string series, int to)
        {
            if (series == "lobbyCards") currentlySelectedCard = to;
            return;
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
                    RequestLobbyJoin((lobbyButtons[currentlySelectedCard] as LobbyInfoCard).lobbyInfo, password);
                    break;
            }
        }

        public bool GetChecked(CheckBox box)
        {
            string idstring = box.IDString;
            if (idstring != null)
            {
                if (idstring == "SETPASSWORD")
                {
                    return setpassword;
                }
            }
            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            string idstring = box.IDString;
            if (idstring != null)
            {
                if (idstring == "SETPASSWORD")
                {
                    setpassword = !setpassword;
                }
            }
        }
    }
}
