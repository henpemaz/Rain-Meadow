using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DevInterface;
using HUD;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCoinMenu : Menu.Menu
    {
        ButtonSelector shopItems;
        MeadowCoinHUD hud;
        public MeadowCoinMenu(MeadowCoinHUD hud, ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
        {
            this.hud = hud;
            
            pages.Add(new(this, null, "Shop!", 0));
            Vector2 where = new Vector2(Futile.screen.pixelWidth - 200f, Futile.screen.pixelHeight - 50);
            shopItems = new(this, pages[0], "Shop!", new(where.x, where.y - 38), new(110, 30), 5, 10f);
            shopItems.populateList = (ButtonSelector selector, ButtonScroller scroller) =>
            {
                List<ButtonScroller.IPartOfButtonScroller> items = new();   
                ButtonScroller.ScrollerButton scape = new ButtonScroller.ScrollerButton(this, scroller, 
                    MeadowCoinHUD.boughtSilverCape.Value? "Silver Cape" : "Silver Cape (M$ 5)", Vector2.zero, new(150, 30));
                scape.OnClick += _ =>
                {
                    hud.Purchase(5, MeadowCoinHUD.boughtSilverCape);
                    if (MeadowCoinHUD.boughtSilverCape.Value)
                    {
                        scape.menuLabel.text = "Silver Cape";
                        MeadowCoinHUD.storeCapeEquiped.Value = "silver";
                        if (OnlineManager.lobby.gameMode.clientSettings.TryGetData<MeadowStoreCape>(out var data))
                        {
                            data.cape = MeadowCoinHUD.storeCapeEquiped.Value;
                            foreach(Player p in OnlineManager.lobby.gameMode.avatars.Select(x => x.abstractCreature?.realizedCreature).OfType<Player>())
                            {
                                SlugcatCape.RefreshCape(p);
                            }
                        }
                    }
                };
                items.Add(scape);

                ButtonScroller.ScrollerButton gcape = new ButtonScroller.ScrollerButton(this, scroller, 
                    MeadowCoinHUD.boughtGoldCape.Value? "Gold Cape" : "Gold Cape (M$ 20)", Vector2.zero, new(110, 30));
                gcape.OnClick += _ =>
                {
                    hud.Purchase(20, MeadowCoinHUD.boughtGoldCape);
                    if (MeadowCoinHUD.boughtGoldCape.Value)
                    {
                        gcape.menuLabel.text = "Gold Cape";
                        MeadowCoinHUD.storeCapeEquiped.Value = "gold";
                        if (OnlineManager.lobby.gameMode.clientSettings.TryGetData<MeadowStoreCape>(out var data))
                        {
                            data.cape = MeadowCoinHUD.storeCapeEquiped.Value;
                            foreach(Player p in OnlineManager.lobby.gameMode.avatars.Select(x => x.abstractCreature?.realizedCreature).OfType<Player>())
                            {
                                SlugcatCape.RefreshCape(p);
                            }
                        }
                    }
                    
                    
                };
                items.Add(gcape);

                ButtonScroller.ScrollerButton rcape = new ButtonScroller.ScrollerButton(this, scroller, 
                    MeadowCoinHUD.boughtRainbowCape.Value? "Rainbow Cape" : "Rainbow Cape (M$ 40)", Vector2.zero, new(110, 30));
                rcape.OnClick += _ =>
                {
                    hud.Purchase(40, MeadowCoinHUD.boughtRainbowCape);
                    if (MeadowCoinHUD.boughtRainbowCape.Value)
                    {
                        rcape.menuLabel.text = "Rainbow Cape";
                        MeadowCoinHUD.storeCapeEquiped.Value = "rainbow";
                        if (OnlineManager.lobby.gameMode.clientSettings.TryGetData<MeadowStoreCape>(out var data))
                        {
                            data.cape = MeadowCoinHUD.storeCapeEquiped.Value;
                            foreach(Player p in OnlineManager.lobby.gameMode.avatars.Select(x => x.abstractCreature?.realizedCreature).OfType<Player>())
                            {
                                SlugcatCape.RefreshCape(p);
                            }
                        }
                        
                    }

                };
                items.Add(rcape);

                return items.ToArray();
            };
            pages[0].subObjects.Add(shopItems);
        }
    }

    public class MeadowCoinHUD : HudPart
    {
        private RoomCamera camera;
        private readonly OnlineGameMode onlineGameMode;
        private FContainer container;

        static public Configurable<int> currentAmount;
        static public Configurable<string> storeCapeEquiped;
        static public Configurable<bool> boughtSilverCape;
        static public Configurable<bool> boughtGoldCape;
        static public Configurable<bool> boughtRainbowCape;

        public static void InitConfigurables()
        {
            currentAmount = RainMeadow.rainMeadowOptions.config.Bind("anni_meadowcoin", 0);
            storeCapeEquiped = RainMeadow.rainMeadowOptions.config.Bind("anni_storeCapeEquiped", "");
            boughtSilverCape = RainMeadow.rainMeadowOptions.config.Bind("anni_boughtSilverCape", false);
            boughtGoldCape = RainMeadow.rainMeadowOptions.config.Bind("anni_boughtGoldCape", false);
            boughtRainbowCape = RainMeadow.rainMeadowOptions.config.Bind("anni_boughtRainbowCape", false);
        }



        int coinCounter = 0;
        FLabel coinLabel; 
        List<(FLabel label, int value)> coinAdditionLabels; // main display and the +1 

        MeadowCoinMenu? menu;

        public void AddCoins(int amount, string why = "")
        {
            var label = new FLabel(Custom.GetFont(), why + ": +" + amount.ToString());
            coinAdditionLabels.Add((label, amount));
            container.AddChild(label);
        }

        public void Purchase(int amount, Configurable<bool> config)
        {
            if (config.Value) return;
            if (amount > currentAmount.Value) return;
            currentAmount.Value -= amount;
            coinLabel.text = FormatCoins(currentAmount.Value);
            config.Value = true;
        }



        public MeadowCoinHUD(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;
            hud.textPrompt.AddMessage(hud.rainWorld.inGameTranslator.Translate("Press '") +
             (RainMeadow.rainMeadowOptions.SpectatorKey.Value) + hud.rainWorld.inGameTranslator.Translate(" + Grab' to open the cape shop"), 60, 320, true, true);

            container = new FContainer();
            Futile.stage.AddChild(container);

            coinLabel = new FLabel(Custom.GetDisplayFont(), FormatCoins(currentAmount.Value));
            container.AddChild(coinLabel);
            coinAdditionLabels = new List<(FLabel label, int value)>();
        }


        bool debugCoolKey = false;
        bool openShop = false;

        public override void Update()
        {
            menu?.Update();
            bool coolkey = Input.GetKey(KeyCode.U);
            if(!debugCoolKey && coolkey)
            {
                AddCoins(10, "Awesome key press");
            }

            bool shouldopenShop = Input.GetKey(KeyCode.Q);

            if (shouldopenShop && !openShop)
            {   
                if (menu == null)
                {
                    menu = new MeadowCoinMenu(this, camera.game.manager, RainMeadow.Ext_ProcessID.ShopMode);
                }    
                else
                {
                    menu?.ShutDownProcess();
                    menu = null;
                }
            }

            openShop = shouldopenShop;

            if (coinAdditionLabels.Any())
            {
                coinCounter += 1;
                coinAdditionLabels.First().label.alpha = 1.0f - Mathf.InverseLerp(20, 40, (float)coinCounter);
                if (coinCounter > 40)
                {
                    coinCounter = 0;
                    (FLabel label, int value) = coinAdditionLabels.Pop();
                    label.RemoveFromContainer();

                    currentAmount.Value += value;
                    coinLabel.text = FormatCoins(currentAmount.Value);
                }
            }
            debugCoolKey = coolkey;
            base.Update();
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            container.RemoveFromContainer();
            menu?.ShutDownProcess();
            RainMeadow.rainMeadowOptions.config.Save();
        }

        public override void Draw(float timeStacker)
        {
            menu?.GrafUpdate(timeStacker);
            Vector2 where = new Vector2(Futile.screen.pixelWidth - coinLabel.textRect.width - 25f, Futile.screen.pixelHeight - 50);
            coinLabel.SetPosition(where.x, where.y);


            foreach ((FLabel label, _) in coinAdditionLabels)
            {
                where.y -= 50f;
                label.SetPosition(
                    Futile.screen.pixelWidth - 25f - label.textRect.width, 
                    where.y + 50f*Mathf.InverseLerp(20, 40, (float)coinCounter)
                );
            }

            base.Draw(timeStacker);
        }

        public static string FormatCoins(int coins)
        {
            return coins.ToString().PadLeft(4, '0');
        }
    }


    public class MeadowStoreCape : OnlineEntity.EntityData
    {

        public string cape = null;
        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new MeadowStoreCapeState(this);
        }


        public class MeadowStoreCapeState : EntityDataState
        {
            [OnlineField(nullable = true)]
            public string cape;

            public MeadowStoreCapeState() { }

            public MeadowStoreCapeState(MeadowStoreCape storeCape)
            {
                cape = storeCape.cape;
            }



            public override Type GetDataType()
            {
                return typeof(MeadowStoreCape);
            }

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                if (((MeadowStoreCape)data).cape != cape)
                {
                    ((MeadowStoreCape)data).cape = cape;
                    foreach(Player p in OnlineManager.lobby.gameMode.avatars.Select(x => x.abstractCreature?.realizedCreature).OfType<Player>())
                    {
                        SlugcatCape.RefreshCape(p);
                    }
                }
            }
        }

    }
}