using Menu;
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
            public ItemButton(StoreOverlay menu, Vector2 pos, RainWorldGame game, Onslaught onslaught, bool canBuy = false)
            {
                this.overlay = menu;
                this.button = new SimplerButton(menu, menu.pages[0], "Spear: 1", pos, new Vector2(110, 30));
                WorldCoordinate myAbstractPos;


                this.button.OnClick += (_) =>
                {
                    foreach (var player in game.GetArenaGameSession.Players)
                    {
                        if (OnlinePhysicalObject.map.TryGetValue(player, out var onlineP) && onlineP.owner == OnlineManager.mePlayer)
                        {
                            myAbstractPos = player.pos;
                            AbstractSpear spear = new AbstractSpear(game.world, null, myAbstractPos, game.GetNewID(), false);
                            (game.cameras[0].room.abstractRoom).AddEntity(spear);
                            spear.RealizeInRoom();
                            onslaught.currentPoints--;
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
        public List<ItemButton> storeItems;
        ItemButton itemButtons;
        public Onslaught onslaught;

        public StoreOverlay(ProcessManager manager, RainWorldGame game, Onslaught onslaught) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            this.onslaught = onslaught;
            this.pages.Add(new Page(this, null, "store", 0));
            this.selectedObject = null;
            this.storeItems = new();
            this.pos = new Vector2(180, 553);
            this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("ITEMS"), new(110, 300), new(110, 30), true));
            this.itemButtons = new ItemButton(this, pos, game, onslaught, true);
            this.storeItems.Add(itemButtons);

        }

        public override void Update()
        {
            base.Update();
            if (storeItems != null)
            {
                for (int i = 0; i < storeItems.Count; i++)
                {
                    storeItems[i].button.buttonBehav.greyedOut = onslaught.currentPoints < 1;
                }
            }
        }
    }
}
