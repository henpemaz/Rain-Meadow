using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Menu;
using MoreSlugcats;
using RainMeadow;
using UnityEngine;

namespace RainMeadow
{
    public class HolidayStoreOverlay : Menu.Menu
    {
        public const string Rock = "Rock";
        public const string Spear = "Spear";
        public const string ExplosiveSpear = "Explosive Spear";
        public const string ScavengerBomb = "Scavenger Bomb";
        public const string JokerRifle = "Joke Rifle";
        public const string MeadowCoin = "Meadow Coin";

        public const string SilverCape = "Silver Cape";
        public const string GoldenCape = "Golden Cape";
        public const string RainbowCape = "Rainbow Cape";

        public const string GoldenSkin = "Golden Skin";
        public Vector2 pos;

        public class ItemButton : SimplerButton
        {
            public Configurable<bool>? permanentPurchase;
            public OnlinePhysicalObject player;
            public int cost;
            public HolidayStoreOverlay overlay;

            public string name;
            public bool RequiresWatcher => ModManager.Watcher;
            public bool RequiresMSC => ModManager.MSC;
            public Color chosenColor;
            public ItemButton(
                HolidayStoreOverlay menu,
                MenuObject menuObject,
                Vector2 pos,
                AbstractCreature me,
                RainWorldGame game,
                string item,
                int cost,
                Configurable<bool>? permanentPurchase
            ) : base(menu, menuObject, "", pos, new Vector2(110, 30))
            {

                this.permanentPurchase = permanentPurchase;
                this.overlay = menu;
                this.name = item;
                this.cost = cost;
                UpdateText();



                OnClick += (_) =>
                {
                    ICapeColor? desiredCape = null;
                    AbstractPhysicalObject? desiredObject = null;
                    switch (name)
                    {
                        case Spear:
                            desiredObject = new AbstractSpear(
                                game.world,
                                null,
                                me.pos,
                                game.GetNewID(),
                                false
                            );
                            break;
                        case ExplosiveSpear:
                            desiredObject = new AbstractSpear(
                                game.world,
                                null,
                                me.pos,
                                game.GetNewID(),
                                true
                            );
                            break;
                        case ScavengerBomb:
                            desiredObject = new AbstractPhysicalObject(
                                game.world,
                                AbstractPhysicalObject.AbstractObjectType.ScavengerBomb,
                                null,
                                me.pos,
                                game.GetNewID()
                            );
                            break;
                        case Rock:
                            desiredObject = new AbstractPhysicalObject(
                                game.world,
                                AbstractPhysicalObject.AbstractObjectType.Rock,
                                null,
                                me.pos,
                                game.GetNewID()
                            );
                            break;
                        case MeadowCoin:
                            desiredObject = new DataPearl.AbstractDataPearl(
                            game.world,
                            AbstractPhysicalObject.AbstractObjectType.DataPearl,
                            null,
                            me.pos,
                            game.GetNewID(),
                            -1,
                            -1,
                            null,
                            DataPearl.AbstractDataPearl.DataPearlType.Misc
                        );
                            break;
                        case JokerRifle:
                            desiredObject = new JokeRifle.AbstractRifle(
                                game.world,
                                null,
                                me.pos,
                                game.GetNewID(),
                                JokeRifle.AbstractRifle.AmmoType.Fruit
                            );
                            break;
                        case SilverCape:
                            chosenColor = new Color(0.863f, 0.918f, 0.941f);
                            desiredCape = new SolidCapeColor(chosenColor);
                            break;
                        case GoldenCape:
                            chosenColor = RainWorld.SaturatedGold;
                            desiredCape = new SolidCapeColor(chosenColor);
                            break;
                        case RainbowCape: desiredCape = new RainbowCapeColor(); break;
                    }

                    bool purchased = permanentPurchase?.Value ?? false;

                    if (me != null && (SpecialEvents.CanSpendMeadowCoin(cost) || purchased))
                    {
                        if (name == GoldenSkin)
                        {
                            if (me.GetOnlineCreature() is OnlineCreature critter && critter.TryGetData<SlugcatCustomization>(out var data))
                            {
                                data.overlaySkin = new CoinSkin();
                                CapeManager.RefreshGraphicalModule(critter.realizedCreature);
                            }
                        }

                        if (desiredObject != null)
                        {
                            (game.cameras[0].room.abstractRoom).AddEntity(desiredObject);
                            desiredObject.RealizeInRoom();
                            // me.Room.world.GetResource().ApoEnteringWorld(desiredObject);
                            me.Room.GetResource()?.ApoEnteringRoom(desiredObject, desiredObject.pos);
                            if (me.realizedCreature is Player p)
                            {
                                int freehand = p.FreeHand();
                                if (freehand >= 0) p.SlugcatGrab(desiredObject.realizedObject, freehand);
                            }

                            if (!purchased) SpecialEvents.SpendMeadowCoin(cost);

                        }
                        else if (desiredCape != null)
                        {
                            if (me.GetOnlineCreature() is OnlineCreature critter && critter.TryGetData<SlugcatCustomization>(out var slugcatCustomization))
                            {
                                slugcatCustomization.eventCape = desiredCape;
                                CapeManager.RefreshGraphicalModule(critter.realizedCreature);
                            }

                            if (!purchased) SpecialEvents.SpendMeadowCoin(cost);
                            RainMeadow.rainMeadowOptions.currentlyActiveCapeColor.Value = chosenColor;
                        }

                        if (permanentPurchase is not null) permanentPurchase.Value = true;
                        RainMeadow.rainMeadowOptions.config.Save();
                    }
                    UpdateText();
                };
            }

            public void UpdateText()
            {
                this.menuLabel.text = (permanentPurchase?.Value ?? false) ? $"{name} : Free" : $"{name}: ¤{cost}";
            }
        }

        public RainWorldGame game;
        public List<ItemButton> storeItemList;
        public ButtonScroller playerScroller;
        public List<ItemButton> ItemButtons => playerScroller.GetSpecificButtons<ItemButton>();

        public static int MaxVisibleOnList => 8;
        public static float ButtonSpacingOffset => 8;
        public static float ButtonSize => 30;

        public MenuLabel meadowCoinValue;

        public AbstractCreature me = null;

        public HolidayStoreOverlay(ProcessManager manager, RainWorldGame game)
            : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            SpecialEvents.LoadElement("meadowcoin");
            this.game = game;
            this.pages.Add(new Page(this, null, "store", 0));
            this.selectedObject = null;
            this.storeItemList = new();
            this.pos = new Vector2(180, 553);
            this.container.AddChild(
                new FSprite("meadowcoin")
                {
                    x = pos.x + 30,
                    y = pos.y + 15,
                    scale = 0.10f,
                    color = Color.yellow,
                }
            );
            meadowCoinValue = new Menu.MenuLabel(
                this,
                this.pages[0],
                this.Translate($"¤{RainMeadow.rainMeadowOptions.MeadowCoins.Value}"),
                new Vector2(pos.x + 15, pos.y),
                new Vector2(110, 30),
                true
            );
            this.pages[0].subObjects.Add(meadowCoinValue);

            var storeItems = new List<(string, int, Configurable<bool>?)>
            {
                (SilverCape, 75, RainMeadow.rainMeadowOptions.boughtSilverCape),
                (GoldenCape, 100, RainMeadow.rainMeadowOptions.boughtGoldenCape),
                (RainbowCape, 150, RainMeadow.rainMeadowOptions.boughtRainbowCape),
                (GoldenSkin, 150, RainMeadow.rainMeadowOptions.boughtGoldenSkin),
                (Rock, 1, null),
                (Spear, 5, null),
                (ExplosiveSpear, 10, null),
                (ScavengerBomb, 15, null),
                (MeadowCoin, 1, null),
            };

            if (ModManager.MSC) storeItems.Add((JokerRifle, 50, null));
            me = game.Players.First(x => x is not null && x.IsLocal());
            int index = 0;
            foreach (var item in storeItems)
            {
                // Pass the calculated position to the button
                Vector2 buttonPos = new Vector2(pos.x, this.pos.y - 38 - (index * 40));
                var newItemButton = new ItemButton(this, this.pages[0], buttonPos, me, game, item.Item1, item.Item2, item.Item3);
                this.pages[0].subObjects.Add(newItemButton);
                this.storeItemList.Add(newItemButton);
                index++;
            }
        }

        public override void Update()
        {
            base.Update();
            if (meadowCoinValue != null)
            {
                meadowCoinValue.text = $"¤{RainMeadow.rainMeadowOptions.MeadowCoins.Value}";
            }
            if (me == null)
            {
                for (int i = 0; i < game.Players.Count; i++)
                {
                    if (
                        OnlinePhysicalObject.map.TryGetValue(game.Players[i], out var onlineP)
                        && onlineP.owner == OnlineManager.mePlayer
                    )
                    {
                        me = game.Players[i];
                        break;
                    }
                }
            }
            for (int i = 0; i < storeItemList.Count; i++)
            {
                storeItemList[i].buttonBehav.greyedOut =
                    me != null
                        ? storeItemList[i].permanentPurchase?.Value == true ? false : RainMeadow.rainMeadowOptions.MeadowCoins.Value < storeItemList[i].cost
                        : me == null;
            }
        }
    }
}
