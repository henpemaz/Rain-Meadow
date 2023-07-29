﻿using Menu;
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
    public class LobbySelectMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner
    {
        private List<FSprite> sprites;
        private EventfulSelectOneButton[] lobbyButtons;
        private LobbyInfo[] lobbies;
        private float scroll;
        private float scrollTo;
        private int currentlySelectedCard;
        private OpComboBox visibilityDropDown;
        private SimplerButton playButton;
        private SimplerButton refreshButton;
        private OpComboBox2 modeDropDown;
        private ProperlyAlignedMenuLabel modeDescriptionLabel;

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
            modeDropDown = new OpComboBox2(new Configurable<OnlineGameMode.OnlineGameModeType>(OnlineGameMode.OnlineGameModeType.FreeRoam), where, 160, OpResourceSelector.GetEnumNames(null, typeof(OnlineGameMode.OnlineGameModeType)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
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
            visibilityDropDown = new OpComboBox(new Configurable<MatchmakingManager.LobbyVisibility>(MatchmakingManager.LobbyVisibility.Public), where, 160, OpResourceSelector.GetEnumNames(null, typeof(MatchmakingManager.LobbyVisibility)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
            new UIelementWrapper(this.tabWrapper, visibilityDropDown);

            // left lobby selector
            // bg
            sprites = new();
            FSprite sprite = new FSprite("pixel") { x = 204, y = 137, anchorY = 0, scaleY = 444, color = MenuRGB(MenuColors.MediumGrey) };
            mainPage.Container.AddChild(sprite);
            sprites.Add(sprite);
            sprite = new FSprite("pixel") { x = 528, y = 137, anchorY = 0, scaleY = 444, color = MenuRGB(MenuColors.MediumGrey) };
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
                this.subObjects.Add(new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.mode, new(5, 10), new(10, 50), true));
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
            if (currentlySelectedCard == 0)
            {
                RequestLobbyCreate();
            }
            else
            {
                RequestLobbyJoin((lobbyButtons[currentlySelectedCard] as LobbyInfoCard).lobbyInfo);
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
            MatchmakingManager.instance.CreateLobby(value, modeDropDown.value);
        }

        private void RequestLobbyJoin(LobbyInfo lobby)
        {
            RainMeadow.DebugMe();
            MatchmakingManager.instance.JoinLobby(lobby);
        }

        private void OnlineManager_OnLobbyJoined(bool ok)
        {
            RainMeadow.Debug(ok);
            if (ok)
            {
                // todo: switch case for different lobby types
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbyMenu);
            }
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
    }
}
