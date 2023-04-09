using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace RainMeadow
{

    public class LobbySelectMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner
    {
        private List<FSprite> sprites;
        private SelectOneButton[] lobbyButtons;
        private LobbyInfo[] lobbies;
        private float scroll;

        public override MenuScene.SceneID GetScene => MenuScene.SceneID.Landscape_CC;
        public LobbySelectMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbySelectMenu)
        {
            Vector2 buttonSize = new(130f, 30f);

            // title at the top
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "", "MeadowShadow", new Vector2(-2.99f, 265.01f), true, false));
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "", "MeadowTitle", new Vector2(-2.99f, 265.01f), true, false));
            this.scene.flatIllustrations[this.scene.flatIllustrations.Count - 1].sprite.shader = this.manager.rainWorld.Shaders["MenuText"];

            // 690 on mock -> 720 -> 768 - 720 = 48, placed at 50 so my mock has a +2 offset
            // play button at lower right
            var playButton = new SimplerButton(this, mainPage, Translate("PLAY!"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
            mainPage.subObjects.Add(playButton);

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
            var modeLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("MODE: ") + Translate("RAIN MEADOW"), where, new Vector2(200, 20f), false, null);
            mainPage.subObjects.Add(modeLabel);
            where.y -= 35;
            var modeDescriptionLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("A peaceful mode about exploring around and discovering little secrets, \ntogether or on your own."), where, new Vector2(0, 20f), false, null);
            mainPage.subObjects.Add(modeDescriptionLabel);

            // center-low settings
            where.y -= 45;
            var visibilityLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Visibility:"), where, new Vector2(200, 20f), false, null);
            mainPage.subObjects.Add(visibilityLabel);
            where.x += 80;
            var visibilityDropDown = new OpComboBox(new Configurable<LobbyManager.LobbyVisibility>(LobbyManager.LobbyVisibility.Public), where, 160, OpResourceSelector.GetEnumNames(null, typeof(LobbyManager.LobbyVisibility)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList());
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
            var downButton = new EventfulScrollButton(this, mainPage, new(316, 113), 2, 100);
            mainPage.subObjects.Add(downButton);

            // cards
            CreateLobbyCards();
            // waiting for lobby data!

            // Lobby machine go!
            LobbyManager.OnLobbyListReceived += OnlineManager_OnLobbyListReceived;
            LobbyManager.OnLobbyJoined += OnlineManager_OnLobbyJoined;
            SteamNetworkingUtils.InitRelayNetworkAccess();
            LobbyManager.RequestLobbyList();
        }

        private void CreateLobbyCards()
        {
            var oldLobbyButtons = lobbyButtons;
            if(oldLobbyButtons != null) // remove old
            {
                for (int i = 1; i < oldLobbyButtons.Length; i++) // skips newlobby
                {
                    var btn = oldLobbyButtons[i];
                    btn.RemoveSprites();
                    mainPage.RemoveSubObject(btn);
                }
            }

            lobbyButtons = new SelectOneButton[lobbies != null ? 1 + lobbies.Length : 1];
            if(oldLobbyButtons == null)
            {
                lobbyButtons[0] = new NewLobbyButton(this, mainPage, Translate("CREATE NEW LOBBY"), new(214, 530), new(304, 40), lobbyButtons, 0);
                mainPage.subObjects.Add(lobbyButtons[0]);
            }
            else
            {
                lobbyButtons[0] = oldLobbyButtons[0];
            }
            
            if(lobbies != null)
            {
                for (int i = 0; i < lobbies.Length; i++)
                {
                    var lobby = lobbies[i];
                    var btn = new LobbyInfoCard(this, mainPage, lobby.name, CardPosition(i), new(304, 60), lobbyButtons, i + 1, lobby);
                    mainPage.subObjects.Add(btn);
                }
            }
        }

        private Vector2 CardPosition(int i)
        {
            Vector2 rootPos = new(214, 460);
            Vector2 offset = new(0,70);
            return rootPos - (scroll + (float)i) * offset;
        }

        class NewLobbyButton : EventfulSelectOneButton
        {
            public NewLobbyButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, SelectOneButton[] buttonArray, int buttonArrayIndex) : base(menu, owner, displayText, pos, size, buttonArray, buttonArrayIndex)
            {
            
            }
        }

        class LobbyInfoCard : EventfulSelectOneButton
        {
            LobbyInfo lobbyInfo;
            public LobbyInfoCard(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, SelectOneButton[] buttonArray, int buttonArrayIndex, LobbyInfo lobbyInfo) : base(menu, owner, displayText, pos, size, buttonArray, buttonArrayIndex)
            {
                this.lobbyInfo = lobbyInfo;
            }
        }

        void NewLobbyClicked(EventfulSelectOneButton button)
        {

        }

        void InfoCardClicked(EventfulSelectOneButton button)
        {
            if(button is LobbyInfoCard infoCard)
            {
                // todo
            }
        }

        void RequestLobbyCreate()
        {
            RainMeadow.DebugMe();
            LobbyManager.CreateLobby();
        }

        void RequestLobbyJoin(LobbyInfo lobby)
        {
            RainMeadow.DebugMe();
            LobbyManager.JoinLobby(lobby);
        }

        private void OnlineManager_OnLobbyJoined(bool ok)
        {
            RainMeadow.Debug(ok);
            if (ok)
            {
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
            LobbyManager.OnLobbyListReceived -= OnlineManager_OnLobbyListReceived;
            LobbyManager.OnLobbyJoined -= OnlineManager_OnLobbyJoined;
            base.ShutDownProcess();
        }

        // SelectOneButton.SelectOneButtonOwner
        public int GetCurrentlySelectedOfSeries(string series)
        {
            return 0; // TODO
        }

        // SelectOneButton.SelectOneButtonOwner
        public void SetCurrentlySelectedOfSeries(string series, int to)
        {
            return; // TODO
        }
    }
}
