﻿using Menu;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public class CustomLobbyMenu : SmartMenu
    {
        public List<FSprite> sprites;
        public ProperlyAlignedMenuLabel[] playerLabels = new ProperlyAlignedMenuLabel[0];
        public PlayerInfo[] players;
        public float scroll;
        public float scrollTo;
        public Vector2 btns = new Vector2(350, 50);
        public Vector2 btnsize = new Vector2(100, 20);
        public SimplerButton startbtn;

        public override MenuScene.SceneID GetScene => MenuScene.SceneID.Landscape_CC;
        public CustomLobbyMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            RainMeadow.DebugMe();
            this.backTarget = RainMeadow.Ext_ProcessID.LobbySelectMenu;

            // player list
            // label
            var playerListLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Players"), new(204, 610), new Vector2(200, 20f), true);
            mainPage.subObjects.Add(playerListLabel);

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
            List<PlayerInfo> players = new List<PlayerInfo>();
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                players.Add(new PlayerInfo(() => player.id.OpenProfileLink(), player.id.name));
            }
            this.players = players.ToArray();
            CreatePlayerCards();

            MatchmakingManager.currentInstance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        }

        public override void Update()
        {
            base.Update();
            int extraItems = Mathf.Max(players.Length - 4, 0);
            scrollTo = Mathf.Clamp(scrollTo, -0.5f, extraItems + 0.5f);
            if (scrollTo < 0) scrollTo = RWCustom.Custom.LerpAndTick(scrollTo, 0, 0.1f, 0.1f);
            if (scrollTo > extraItems) scrollTo = RWCustom.Custom.LerpAndTick(scrollTo, extraItems, 0.1f, 0.1f);
            scroll = RWCustom.Custom.LerpAndTick(scroll, scrollTo, 0.1f, 0.1f);
        }

        public void CreatePlayerCards()
        {
            for (int i = 0; i < playerLabels.Length; i++)
            {
                var label = playerLabels[i];
                label.RemoveSprites();
                mainPage.RemoveSubObject(label);
            }

            playerLabels = new ProperlyAlignedMenuLabel[players.Length];

            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var label = new PlayerInfoCard(this, mainPage, CardPosition(i), new(304, 60), i, player);
                mainPage.subObjects.Add(label);
                playerLabels[i] = label;
            }
        }

        public Vector2 CardPosition(int i)
        {
            Vector2 rootPos = new(214, 540);
            Vector2 offset = new(0, 20);
            return rootPos - (scroll + i - 1) * offset;
        }

        public class PlayerInfoCard : ProperlyAlignedMenuLabel
        {
            public PlayerInfo playerInfo;
            public int playerIndex;
            public PlayerInfoCard(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, int playerIndex, PlayerInfo playerInfo) : base(menu, owner, playerInfo.name, pos, size, false)
            {
                this.playerInfo = playerInfo;
                this.playerIndex = playerIndex;
            }

            public override void Update()
            {
                base.Update();
                pos = (menu as CustomLobbyMenu).CardPosition(this.playerIndex);
            }
        }

        public void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            this.players = players;
            CreatePlayerCards();
        }

        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe();
            MatchmakingManager.currentInstance.OnPlayerListReceived -= OnlineManager_OnPlayerListReceived;
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                OnlineManager.LeaveLobby();
            }
            base.ShutDownProcess();
        }
    }
}
