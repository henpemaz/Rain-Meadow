using Menu;
using On.MoreSlugcats;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace RainMeadow
{
    public class StoreOverlay : Menu.Menu
    {
        public AbstractCreature? spectatee;

        public Vector2 pos;


        public class ItemButton
        {
            public OnlinePhysicalObject player;
            public SimplerButton button;
            public SimplerSymbolButton? kickbutton;
            public bool mutedPlayer;
            private string clientMuteSymbol;
            public Dictionary<string, int> storeItems;
            public StoreOverlay overlay;
            public int cost;
            public ItemButton(StoreOverlay menu, Vector2 pos, RainWorldGame game, Onslaught onslaught, KeyValuePair<string, int> itemEntry, int index, bool canBuy = false)
            {
                this.overlay = menu;
                this.cost = itemEntry.Value;
                this.button = new SimplerButton(menu, menu.pages[0], $"{itemEntry.Key}: {itemEntry.Value}", pos, new Vector2(110, 30));
                WorldCoordinate myAbstractPos;


                this.button.OnClick += (_) =>
                {
                    foreach (var player in game.GetArenaGameSession.Players)
                    {
                        if (OnlinePhysicalObject.map.TryGetValue(player, out var onlineP) && onlineP.owner == OnlineManager.mePlayer)
                        {
                            myAbstractPos = player.pos;
                            AbstractPhysicalObject desiredObject = null;
                            switch (index)
                            {

                                case 0:
                                    desiredObject = new AbstractSpear(game.world, null, myAbstractPos, game.GetNewID(), false);
                                    break;
                                case 1:
                                    desiredObject = new AbstractSpear(game.world, null, myAbstractPos, game.GetNewID(), true);
                                    break;

                            }
                            (game.cameras[0].room.abstractRoom).AddEntity(desiredObject);
                            desiredObject.RealizeInRoom();
                            onslaught.currentPoints = onslaught.currentPoints - itemEntry.Value;



                        }
                    }




                };
                this.button.owner.subObjects.Add(button);
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
        public Onslaught onslaught;

        public StoreOverlay(ProcessManager manager, RainWorldGame game, Onslaught onslaught) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            this.onslaught = onslaught;
            this.pages.Add(new Page(this, null, "store", 0));
            this.selectedObject = null;
            this.storeItemList = new();
            this.pos = new Vector2(180, 553);
            this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("ITEMS"), new(pos.x, pos.y + 30f), new(110, 30), true));
            var storeItems = new Dictionary<string, int> {
            { "Spear", 1 },
            { "Explosive Spear", 5 },
            { "Scav Bomb", 5 }
        };
            int index = 0; // Initialize an index variable

            foreach (var item in storeItems)
            {
                // Format the message for the button, for example: "Spear: 1"
                string buttonMessage = $"{item.Key}: {item.Value}";

                // Create a new ItemButton for each dictionary entry
                this.itemButtons = new ItemButton(this, pos, game, onslaught, item, index, true);
                this.storeItemList.Add(itemButtons);

                // Optionally, adjust `pos` (the position) if you want the buttons to be spaced out.
                // For example, you could increment the Y position for each new button:
                pos.y -= 40; // Move the button 40 units down for the next one
                index++;
            }

        }

        public override void Update()
        {
            base.Update();
            if (storeItemList != null)
            {
                for (int i = 0; i < storeItemList.Count; i++)
                {
                    storeItemList[i].button.buttonBehav.greyedOut = onslaught.currentPoints < storeItemList[i].cost;
                }
            }
        }
    }
}
