using System.Collections.Generic;
using System.Linq;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public abstract class BaseStoreOverlay : Menu.Menu
    {
        public const string Rock = "Rock";
        public const string Spear = "Spear";
        public const string ExplosiveSpear = "Explosive Spear";
        public const string ScavengerBomb = "Scavenger Bomb";

        public RainWorldGame game;
        public Vector2 pos;
        public AbstractCreature? me;

        protected BaseStoreOverlay(ProcessManager manager, RainWorldGame game)
            : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            this.pos = new Vector2(180, 553);
            this.pages.Add(new Page(this, null, "store", 0));
            this.selectedObject = null;
        }

        public override void Update()
        {
            base.Update();
            FindLocalPlayer();
        }

        protected void FindLocalPlayer()
        {
            if (me != null) return;

            foreach (var player in game.Players)
            {
                if (player != null && OnlinePhysicalObject.map.TryGetValue(player, out var onlineP) && onlineP.owner == OnlineManager.mePlayer)
                {
                    me = player;
                    break;
                }
            }
        }

        protected AbstractPhysicalObject? CreateStandardItem(string itemName, AbstractCreature player)
        {
            switch (itemName)
            {
                case Spear:
                    return new AbstractSpear(game.world, null, player.pos, game.GetNewID(), false);
                case ExplosiveSpear:
                    return new AbstractSpear(game.world, null, player.pos, game.GetNewID(), true);
                case ScavengerBomb:
                    return new AbstractPhysicalObject(game.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, player.pos, game.GetNewID());
                case Rock:
                    return new AbstractPhysicalObject(game.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, player.pos, game.GetNewID());
                default:
                    return null;
            }
        }

        protected virtual void FinalizeObjectSpawn(AbstractPhysicalObject desiredObject)
        {
            game.cameras[0].room.abstractRoom.AddEntity(desiredObject);
            desiredObject.RealizeInRoom();
        }
    }

    public abstract class BaseStoreButton : SimplerButton
    {
        public string itemName;
        public int cost;

        protected BaseStoreButton(Menu.Menu menu, MenuObject owner, Vector2 pos, string itemName, int cost)
            : base(menu, owner, "", pos, new Vector2(110, 30))
        {
            this.itemName = menu.Translate(itemName);
            this.cost = cost;

            OnClick += (_) => OnButtonClick();
        }

        protected abstract void OnButtonClick();
        public abstract void UpdateText();
    }
}