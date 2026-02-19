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
        public const string Grub = "Grub";

        public const string InvsFrind = "Inv's Friend";
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
                    AbstractPhysicalObject desiredObject = null;
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
                    }

                    if (desiredObject != null && me != null)
                    {
                        (game.cameras[0].room.abstractRoom).AddEntity(desiredObject);
                        desiredObject.RealizeInRoom();
                        SpecialEvents.SpentMeadowCoin(itemEntry.Value);
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
                { InvsFrind, 50 },
            };
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
