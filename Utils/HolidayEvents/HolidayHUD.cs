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
        public Vector2 pos;

        public class ItemButton
        {
            public OnlinePhysicalObject player;
            public SimplerButton button;
            public Dictionary<string, int> storeItems;
            public HolidayStoreOverlay overlay;
            public int cost;
            public string name;
            public bool didRespawn;
            public bool RequiresWatcher => ModManager.Watcher;
            public bool RequiresMSC => ModManager.MSC;

            public ItemButton(
                HolidayStoreOverlay menu,
                AbstractCreature me,
                Vector2 pos,
                RainWorldGame game,
                KeyValuePair<string, int> itemEntry,
                int index,
                bool canBuy = false
            )
            {
                this.overlay = menu;
                this.name = itemEntry.Key;
                this.cost = itemEntry.Value;
                this.button = new SimplerButton(
                    menu,
                    menu.pages[0],
                    $"{itemEntry.Key}: ¤{itemEntry.Value}",
                    pos,
                    new Vector2(110, 30)
                );

                this.button.OnClick += (_) =>
                {
                    ICapeColor? desiredCape = null;
                    AbstractPhysicalObject? desiredObject = null;
                    switch (itemEntry.Key)
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
                        case SilverCape: desiredCape = new SolidCapeColor(Color.red); break;
                        case GoldenCape: desiredCape = new SolidCapeColor(new Color(0.863f, 0.918f, 0.941f)); break;
                        case RainbowCape: desiredCape = new RainbowCapeColor(); break;
                    }

                    if (me != null && SpecialEvents.CanSpendMeadowCoin(itemEntry.Value))
                    {
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

                            SpecialEvents.SpendMeadowCoin(itemEntry.Value);
                        }
                        else if (desiredCape != null)
                        {
                            if (me.GetOnlineCreature() is OnlineCreature critter && critter.TryGetData<SlugcatCustomization>(out var slugcatCustomization))
                            {
                                slugcatCustomization.eventCape = desiredCape;
                            }
                            
                            SpecialEvents.SpendMeadowCoin(itemEntry.Value);
                        }
                    }
                    didRespawn = false;
                };
                menu.pages[0].subObjects.Add(this.button); // Add to the page specifically
            }

            public void Destroy()
            {
                this.button.RemoveSprites();
                this.button.page.RemoveSubObject(this.button);
            }
        }

        public RainWorldGame game;
        public List<ItemButton> storeItemList;
        ItemButton itemButtons;
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

            var storeItems = new Dictionary<string, int>
            {
                { Rock, 1 },
                { Spear, 5 },
                { ExplosiveSpear, 10 },
                { ScavengerBomb, 15 },
                { JokerRifle, 50 },
                { MeadowCoin, 1 },
                { SilverCape, 75 },
                { GoldenCape, 100 },
                { RainbowCape, 150 },
            };
            for (int i = 0; i < game.Players.Count; i++)
            {
                if (
                    game.Players[i].IsLocal()
                )
                {
                    me = game.Players[i];
                    break;
                }
            }
            int index = 0;
            foreach (var item in storeItems)
            {
                // Pass the calculated position to the button
                Vector2 buttonPos = new Vector2(pos.x, this.pos.y - 38 - (index * 40));

                var newItemButton = new ItemButton(this, me, buttonPos, game, item, index, true);

                // Ensure the button is actually visible on the page
                if (!this.pages[0].subObjects.Contains(newItemButton.button))
                {
                    this.pages[0].subObjects.Add(newItemButton.button);
                }

                if (newItemButton.name == JokerRifle && !ModManager.MSC)
                {
                    index++;
                    continue;
                }
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
                storeItemList[i].button.buttonBehav.greyedOut =
                    me != null
                        ? RainMeadow.rainMeadowOptions.MeadowCoins.Value < storeItemList[i].cost
                        : me == null;
            }
        }
    }
}
