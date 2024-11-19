using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;


#if !LOCAL_P2P
using Steamworks;
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using UnityEngine;

namespace RainMeadow
{
    public class LobbySelectMenu : SmartMenu
    {
        private List<FSprite> sprites;
        private LobbyCardsList lobbyList;
        private OpComboBox2 filterModeDropDown;
        private OpCheckBox filterPublicLobbiesOnly;
        private OpTextBox filterLobbyLimit;
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
        private OpCheckBox enablePasswordCheckbox;
        private int maxPlayerCount;
        private EventfulScrollButton upButton;
        private EventfulScrollButton downButton;
        private List<MenuObject> creationMenuElements;
        private List<UIconfig> creationMenuUiConfigElements;
        public bool creationMenuEnabled = false;
        public decimal creationMenuFade = 1;
        public FContainer creationMenuContainer;

        public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;
        public LobbySelectMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbySelectMenu)
        {
            RainMeadow.DebugMe();

            this.backTarget = ProcessManager.ProcessID.MainMenu;

            lobbyList = new LobbyCardsList(this, mainPage, new Vector2(518, 100f), new Vector2(330f, 490f));
            lobbyList.RefreshButton.OnClick += RefreshLobbyList;
            mainPage.subObjects.Add(lobbyList);

            // // title at the top
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "", "MeadowShadow", new Vector2(-2.99f, 265.01f), true, false));
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "", "MeadowTitle", new Vector2(-2.99f, 265.01f), true, false));
            this.scene.flatIllustrations[this.scene.flatIllustrations.Count - 1].sprite.shader = this.manager.rainWorld.Shaders["MenuText"];

            createButton = new SimplerButton(this, mainPage, Translate("CREATE!"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
            createButton.OnClick += (obj) => creationMenuEnabled = !creationMenuEnabled;
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

            creationMenuElements = new List<MenuObject>();
            creationMenuUiConfigElements = new List<UIconfig>();
            // // center description
            var where = new Vector2(500f, 550);
            var modeLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Mode:"), where, new Vector2(200, 20f), false);
            creationMenuElements.Add(modeLabel);
            mainPage.subObjects.Add(modeLabel);
            where.x += 80;
            modeDropDown = new OpComboBox2(new Configurable<OnlineGameMode.OnlineGameModeType>(OnlineGameMode.OnlineGameModeType.Meadow), where, 160, OpResourceSelector.GetEnumNames(null, typeof(OnlineGameMode.OnlineGameModeType)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            modeDropDown.OnChanged += UpdateModeDescription;
            creationMenuUiConfigElements.Add(modeDropDown);
            new UIelementWrapper(this.tabWrapper, modeDropDown);
            where.x -= 80;

            where.y -= 35;
            modeDescriptionLabel = new ProperlyAlignedMenuLabel(this, mainPage, "", where, new Vector2(0, 20f), false);
            mainPage.subObjects.Add(modeDescriptionLabel);
            creationMenuElements.Add(modeDescriptionLabel);
            UpdateModeDescription();

            // center-low settings
            where.y -= 45;
            var visibilityLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Visibility:"), where, new Vector2(200, 20f), false);
            mainPage.subObjects.Add(visibilityLabel);
            creationMenuElements.Add(visibilityLabel);
            where.x += 80;
            visibilityDropDown = new OpComboBox2(new Configurable<MatchmakingManager.LobbyVisibility>(MatchmakingManager.LobbyVisibility.Public), where, 160, OpResourceSelector.GetEnumNames(null, typeof(MatchmakingManager.LobbyVisibility)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            creationMenuUiConfigElements.Add(visibilityDropDown);
            new UIelementWrapper(this.tabWrapper, visibilityDropDown);
            where.x -= 80;

            where.y -= 45;
            enablePasswordCheckbox = new OpCheckBox(new Configurable<bool>(false), where); //new Vector2(600f, 400f));//where);
            creationMenuUiConfigElements.Add(enablePasswordCheckbox);
            new UIelementWrapper(tabWrapper, enablePasswordCheckbox);

            // password setting
            where.x += 160;
            passwordInputBox = new OpTextBox(new Configurable<string>(""), where, 160f);
            passwordInputBox.accept = OpTextBox.Accept.StringASCII;
            passwordInputBox.allowSpace = true;
            passwordInputBox.description = "Lobby Password";
            passwordInputBox.label.text = "Password";
            creationMenuUiConfigElements.Add(passwordInputBox);
            new UIelementWrapper(this.tabWrapper, passwordInputBox);

            // textbox lobby limit option
            where.x -= 160;
            where.y -= 45;
            var limitNumberLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Player max:"), where, new Vector2(400, 20f), false);
            mainPage.subObjects.Add(limitNumberLabel);
            creationMenuElements.Add(limitNumberLabel);
            where.x += 80;
            where.y -= 5;
            lobbyLimitNumberTextBox = new OpTextBox(new Configurable<int>(maxPlayerCount = 4), where, 160f);
            lobbyLimitNumberTextBox.accept = OpTextBox.Accept.Int;
            lobbyLimitNumberTextBox.maxLength = 2;
            lobbyLimitNumberTextBox.description = "The Max of the Players for the Lobby (up to 32)";
            creationMenuUiConfigElements.Add(lobbyLimitNumberTextBox);
            new UIelementWrapper(this.tabWrapper, lobbyLimitNumberTextBox);
            where.y += 5;

            // // display version
            // MenuLabel versionLabel = new MenuLabel(this, pages[0], $"Rain Meadow Version: {RainMeadow.MeadowVersionStr}", new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f + 20f, manager.rainWorld.screenSize.y - 768f), new Vector2(200f, 20f), false, null);
            MenuLabel versionLabel = new MenuLabel(this, pages[0], $"hehe funni text no version number 4 u :P", new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f + 20f, manager.rainWorld.screenSize.y - 768f), new Vector2(200f, 20f), false, null);
            versionLabel.size = new Vector2(versionLabel.label.textRect.width, versionLabel.size.y);
            mainPage.subObjects.Add(versionLabel);

            // // filters
            // filter = new LobbyFilter();
            // var filterLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Filters"), new Vector2(600f, 305f), new Vector2(200f, 20f), true);
            // mainPage.subObjects.Add(filterLabel);

            where = new Vector2(300f, 400f);
            var filterModeLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Lobby Mode"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterModeLabel);
            where.y -= 27;
            // filterModeDropDown = new OpComboBox2(new Configurable<LobbyCardsList.LobbyCardsFilter.GameModeFilter>(LobbyCardsList.LobbyCardsFilter.GameModeFilter.All), where, 160f, OpResourceSelector.GetEnumNames(null, typeof(LobbyFilter.ModeFilter)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            // filterModeDropDown.OnChange += UpdateLobbyFilter;
            // new UIelementWrapper(this.tabWrapper, filterModeDropDown);
            where.y -= 30;
            var filterPublicLobbiesOnlyLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Public Lobbies Only"), where, new Vector2(200f, 20f), false);
            mainPage.subObjects.Add(filterPublicLobbiesOnlyLabel);
            where.y -= 27;
            filterPublicLobbiesOnly = new OpCheckBox(new Configurable<bool>(false), where);
            filterPublicLobbiesOnly.OnChange += UpdateLobbyFilter;
            new UIelementWrapper(this.tabWrapper, filterPublicLobbiesOnly);
            // where.y -= 30;
            // var filterLobbyLimitLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Max Lobby Size"), where, new Vector2(200f, 20f), false);
            // mainPage.subObjects.Add(filterLobbyLimitLabel);
            // where.y -= 27;
            // filterLobbyLimit = new OpTextBox(new Configurable<int>(32), where, 50f);
            // filterLobbyLimit.accept = OpTextBox.Accept.Int;
            // filterLobbyLimit.maxLength = 2;
            // filterLobbyLimit.OnChange += UpdateLobbyFilter;
            // new UIelementWrapper(this.tabWrapper, filterLobbyLimit);

            // // left lobby selector
            // // bg
            // sprites = new();
            // FSprite sprite = new FSprite("pixel") { x = 204 - (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, y = 137, anchorY = 0, scaleY = 444, color = MenuRGB(MenuColors.MediumGrey) };
            // mainPage.Container.AddChild(sprite);
            // sprites.Add(sprite);
            // sprite = new FSprite("pixel") { x = 528 - (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, y = 137, anchorY = 0, scaleY = 444, color = MenuRGB(MenuColors.MediumGrey) };
            // mainPage.Container.AddChild(sprite);
            // sprites.Add(sprite);

            // // buttons
            // upButton = new EventfulScrollButton(this, mainPage, new(316, 581), 0, 100);
            // mainPage.subObjects.Add(upButton);
            // upButton.OnClick += (_) => scrollTo -= 1f;
            // downButton = new EventfulScrollButton(this, mainPage, new(316, 113), 2, 100);
            // mainPage.subObjects.Add(downButton);
            // downButton.OnClick += (_) => scrollTo += 1f;

            // // searchbar
            // lobbySearchInputBox = new OpTextBox(new Configurable<string>(""), new Vector2(214f, 550f), 304f);
            // lobbySearchInputBox.label.text = "Search Lobbies";
            // lobbySearchInputBox.OnChange += UpdateLobbyFilter;
            // new UIelementWrapper(this.tabWrapper, lobbySearchInputBox);

            // // cards
            // lobbies = new LobbyInfo[0];
            // lobbyButtons = new LobbyInfoCard[1];
            // CreateLobbyCards();
            // // waiting for lobby data!

            // if (OnlineManager.currentlyJoiningLobby != default)
            // {
            //     ShowLoadingDialog("Joining lobby...");
            // }

            // // Lobby machine go!
            MatchmakingManager.instance.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            // MatchmakingManager.instance.OnLobbyJoined += OnlineManager_OnLobbyJoined;
#if !LOCAL_P2P
            // SteamNetworkingUtils.InitRelayNetworkAccess();
#endif
            // MatchmakingManager.instance.RequestLobbyList();

            if (manager.musicPlayer != null)
            {
                manager.musicPlayer.MenuRequestsSong("Establish", 1, 0);
                if (manager.musicPlayer.nextSong == null)
                {
                    manager.musicPlayer.song.Loop = true; //well if you want that you gotta also make it disable when out of the menu hehe
                }
                else
                {
                    manager.musicPlayer.nextSong.Loop = true; //well if you want that you gotta also make it disable when out of the menu hehe
                }
            }
        }

        private void UpdateModeDescription()
        {
            modeDescriptionLabel.text = Translate(OnlineGameMode.OnlineGameModeType.descriptions[new OnlineGameMode.OnlineGameModeType(modeDropDown.value)]);
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

            if (creationMenuEnabled)
            {
                if (creationMenuFade > 0) creationMenuFade -= 0.1m;
            }
            else if (!creationMenuEnabled && creationMenuFade < 1) creationMenuFade += 0.1m;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            for (int i = 0; i < creationMenuElements.Count; i++)
            {
                switch (creationMenuElements[i])
                {
                    case ProperlyAlignedMenuLabel menuLabel:
                        menuLabel.label.alpha = (float)(1 - creationMenuFade);
                        break;
                }
            }
            for (int i = 0; i < creationMenuUiConfigElements.Count; i++)
            {
                creationMenuUiConfigElements[i].myContainer.alpha = (float)(1 - creationMenuFade);
                creationMenuUiConfigElements[i].greyedOut = creationMenuFade == 1;
                // switch (creationMenuUiConfigElements[i])
                // {
                //     case OpComboBox2 comboBox:
                //         comboBox.greyedOut = true;
                //         break;
                //     case OpCheckBox checkBox:
                //         checkBox. = true;
                //         break;
                //     case OpTextBox textBox:
                //         textBox.greyedOut = true;
                //         break;
                // }
            }
        }

        private void ApplyLobbyLimit()
        {
            maxPlayerCount = lobbyLimitNumberTextBox.valueInt;
            if (lobbyLimitNumberTextBox.valueInt > 32) lobbyLimitNumberTextBox.valueInt = 32;
            if (lobbyLimitNumberTextBox.valueInt < 2) lobbyLimitNumberTextBox.valueInt = 2;
        }

        private void UpdateLobbyFilter()
        {
            lobbyList.filter.gameMode = filterModeDropDown.value;
            lobbyList.filter.publicLobby = filterPublicLobbiesOnly.GetValueBool();

            lobbyList.FilterLobbies();
        }

        private void Play(SimplerButton obj)
        {
            // if (obj is not LobbyInfoCard infoCard)
            // {
            //     ShowErrorDialog("Play was called by something other than a lobby card");
            //     return;
            // }
            // if (ModManager.JollyCoop)
            // {
            //     ShowErrorDialog("Please disable JollyCoop before playing Online");
            //     return;
            // }


            // lastClickedLobbyButton = infoCard;

            // var lobbyInfo = infoCard.lobbyInfo;
            // MatchmakingManager.MAX_LOBBY = lobbyInfo.maxPlayerCount;
            // if (lobbyInfo.playerCount >= lobbyInfo.maxPlayerCount)
            // {
            //     ShowErrorDialog("Failed to join lobby.<LINE> Lobby is full");
            // }
            // else
            // {
            //     if (lobbyInfo.hasPassword)
            //     {
            //         ShowPasswordRequestDialog();
            //     }
            //     else
            //     {
            //         ShowLoadingDialog("Joining lobby...");
            //         RequestLobbyJoin(lobbyInfo);
            //     }
            // }
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

        private void RefreshLobbyList(SymbolButton obj)
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
                lobbyList.allLobbies = lobbies.ToList();
                lobbyList.FilterLobbies();
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
                    // RequestLobbyJoin(lastClickedLobbyButton.lobbyInfo, password);
                    break;
            }
        }
    }
}
