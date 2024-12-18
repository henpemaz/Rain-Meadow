// HACK thrown together in a panic

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
        private SimplerButton createButton;
        private OpComboBox2 filterModeDropDown;
        private OpCheckBox filterPublicLobbiesOnly;
        private OpTextBox filterLobbyLimit;
        private LobbyCardsList lobbyList;
        public LobbyInfo lastClickedLobby;
        private MenuDialogBox popupDialog;

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

            // // display version
            // MenuLabel versionLabel = new MenuLabel(this, pages[0], $"Rain Meadow Version: {RainMeadow.MeadowVersionStr}", new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f + 20f, manager.rainWorld.screenSize.y - 768f), new Vector2(200f, 20f), false, null);
            MenuLabel versionLabel = new MenuLabel(this, pages[0], $"hehe funni text no version number 4 u :P", new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f + 20f, manager.rainWorld.screenSize.y - 768f), new Vector2(200f, 20f), false, null);
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

            // if (OnlineManager.currentlyJoiningLobby != default)
            // {
            //     ShowLoadingDialog("Joining lobby...");
            // }

            // // Lobby machine go!
            MatchmakingManager.instance.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            MatchmakingManager.instance.OnLobbyJoined += OnlineManager_OnLobbyJoined;
#if !LOCAL_P2P
            SteamNetworkingUtils.InitRelayNetworkAccess();
#endif
            MatchmakingManager.instance.RequestLobbyList();

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
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }

        private void UpdateLobbyFilter()
        {
            lobbyList.filter.gameMode = filterModeDropDown.value;
            lobbyList.filter.publicLobby = filterPublicLobbiesOnly.GetValueBool();

            lobbyList.FilterLobbies();
        }

        public void Play(LobbyInfo lobbyInfo)
        {

            if (ModManager.JollyCoop)
            {
                ShowErrorDialog("Please disable JollyCoop before playing Online");
                return;
            }
            lastClickedLobby = lobbyInfo;



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

        private void RefreshLobbyList(SymbolButton obj)
        {
            MatchmakingManager.instance.RequestLobbyList();
        }

        public void RequestLobbyJoin(LobbyInfo lobby, string? password = null)
        {
            RainMeadow.DebugMe();
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

        public void GreyOutLobbyCards(bool greyedOut)
        {
            lobbyList.lobbyCards.Do(card => card.buttonBehav.greyedOut = greyedOut);
        }

        public void ShowPasswordRequestDialog()
        {
            if (popupDialog != null) HideDialog();

            popupDialog = new CustomInputDialogueBox(this, mainPage, "Password Required", "HIDE_PASSWORD", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
            mainPage.subObjects.Add(popupDialog);

            GreyOutLobbyCards(true);
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
                    ShowLoadingDialog("Joining lobby...");
                    RequestLobbyJoin(lastClickedLobby, password);
                    break;
            }
        }
    }
}
