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
            public ItemButton(StoreOverlay menu, Vector2 pos, RainWorldGame game, bool canBuy = false)
            {
                this.overlay = menu;
                this.button = new SimplerButton(menu, menu.pages[0], "Spear", pos, new Vector2(110, 30));
                this.storeItems = new Dictionary<string, int>
                {
                    { "Spear", 1 }
                };

                this.button.OnClick += (_) =>
                {
                    Rock rock = new Rock(new AbstractPhysicalObject(game.cameras[0].room.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, game.cameras[0].followAbstractCreature.pos, game.GetNewID()), game.cameras[0].room.world);
                    (game.cameras[0].followAbstractCreature.Room).AddEntity(rock.abstractPhysicalObject);
                    rock.abstractPhysicalObject.RealizeInRoom();


                    //if ((spear.data as PlacedObject.MultiplayerItemData).type == PlacedObject.MultiplayerItemData.Type.Spear)
                    //{
                    //    abstractObjectType = AbstractPhysicalObject.AbstractObjectType.Spear;
                    //}
                    //            else if (roomSettings.placedObjects[num10].type == PlacedObject.Type.MultiplayerItem)
                    //{
                    //    if (game.IsArenaSession)
                    //    {
                    //game.GetArenaGameSession.SpawnItem(game.cameras[0].room, spear.PlaceInRoom);
                    //    }


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

        public StoreOverlay(ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            this.pages.Add(new Page(this, null, "store", 0));
            this.selectedObject = null;
            this.storeItems = new();
            this.pos = new Vector2(180, 553);
            this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("ITEMS"), new(110, 300), new(110, 30), true));
            this.itemButtons = new ItemButton(this, pos, game, true);

        }

        public override void Update()
        {
            base.Update();

            //foreach (var button in storeItems)
            //{
            //    var ac = button.player.apo as AbstractCreature;
            //    button.button.toggled = ac != null && ac == spectatee;
            //    button.button.buttonBehav.greyedOut = ac is null || (ac.state.dead || (ac.realizedCreature != null && ac.realizedCreature.State.dead));
            //}
        }
    }
}
