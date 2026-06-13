using System.Collections.Generic;
using System.Linq;
using Menu;
using MoreSlugcats;
using UnityEngine;

namespace RainMeadow
{
    public class HolidayStoreOverlay : BaseStoreOverlay
    {
        public const string JokerRifle = "Joke Rifle";
        public const string MeadowCoin = "Meadow Coin";
        public const string SilverCape = "Silver Cape";
        public const string GoldenCape = "Golden Cape";
        public const string RainbowCape = "Rainbow Cape";
        public const string GoldenSkin = "Golden Skin";

        public List<HolidayItemButton> storeItemList = new();
        public MenuLabel meadowCoinValue;

        public class HolidayItemButton : BaseStoreButton
        {
            public Configurable<bool>? permanentPurchase;
            public Color chosenColor;
            private HolidayStoreOverlay Store => (HolidayStoreOverlay)menu;

            public HolidayItemButton(HolidayStoreOverlay menu, MenuObject owner, Vector2 pos, string item, int cost, Configurable<bool>? permanentPurchase)
                : base(menu, owner, pos, item, cost)
            {
                this.permanentPurchase = permanentPurchase;
                UpdateText();
                owner.subObjects.Add(this);
            }

            public override void UpdateText()
            {
                this.menuLabel.text = (permanentPurchase?.Value ?? false) ? $"{itemName} : Free" : $"{itemName}: ¤{cost}";
            }

            protected override void OnButtonClick() => Store.HandlePurchase(this);
        }

        public HolidayStoreOverlay(ProcessManager manager, RainWorldGame game) : base(manager, game)
        {
            SpecialEvents.LoadElement("meadowcoin");

            this.container.AddChild(new FSprite("meadowcoin") { x = pos.x + 30, y = pos.y + 15, scale = 0.10f, color = Color.yellow });
            meadowCoinValue = new MenuLabel(this, pages[0], this.Translate($"¤{RainMeadow.rainMeadowOptions.MeadowCoins.Value}"), new Vector2(pos.x + 15, pos.y), new Vector2(110, 30), true);
            pages[0].subObjects.Add(meadowCoinValue);

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

            me = game.Players.FirstOrDefault(x => x is not null && x.IsLocal());

            for (int i = 0; i < storeItems.Count; i++)
            {
                Vector2 buttonPos = new Vector2(pos.x, pos.y - 38 - (i * 40));
                storeItemList.Add(new HolidayItemButton(this, pages[0], buttonPos, storeItems[i].Item1, storeItems[i].Item2, storeItems[i].Item3));
            }
        }

        protected override void FinalizeObjectSpawn(AbstractPhysicalObject desiredObject)
        {
            base.FinalizeObjectSpawn(desiredObject);
            if (me == null) return;

            me.Room.GetResource()?.ApoEnteringRoom(desiredObject, desiredObject.pos);
            if (me.realizedCreature is Player p)
            {
                int freehand = p.FreeHand();
                if (freehand >= 0) p.SlugcatGrab(desiredObject.realizedObject, freehand);
            }
        }

        public void HandlePurchase(HolidayItemButton btn)
        {
            if (me == null) return;
            bool purchased = btn.permanentPurchase?.Value ?? false;

            if (!SpecialEvents.CanSpendMeadowCoin(btn.cost) && !purchased) return;

            ICapeColor? desiredCape = null;
            AbstractPhysicalObject? desiredObject = CreateStandardItem(btn.itemName, me);

            if (desiredObject == null)
            {
                switch (btn.itemName)
                {
                    case MeadowCoin:
                        desiredObject = new DataPearl.AbstractDataPearl(game.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, me.pos, game.GetNewID(), -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Misc);
                        break;
                    case JokerRifle:
                        desiredObject = new JokeRifle.AbstractRifle(game.world, null, me.pos, game.GetNewID(), JokeRifle.AbstractRifle.AmmoType.Fruit);
                        break;
                    case SilverCape:
                        btn.chosenColor = new Color(0.863f, 0.918f, 0.941f);
                        desiredCape = new SolidCapeColor(btn.chosenColor);
                        break;
                    case GoldenCape:
                        btn.chosenColor = RainWorld.SaturatedGold;
                        desiredCape = new SolidCapeColor(btn.chosenColor);
                        break;
                    case RainbowCape:
                        desiredCape = new RainbowCapeColor();
                        break;
                }
            }

            if (btn.itemName == GoldenSkin && me.GetOnlineCreature() is OnlineCreature critter && critter.TryGetData<SlugcatCustomization>(out var data))
            {
                data.overlaySkin = new CoinSkin();
                CapeManager.RefreshGraphicalModule(critter.realizedCreature);
            }

            if (desiredObject != null)
            {
                FinalizeObjectSpawn(desiredObject);
                if (!purchased) SpecialEvents.SpendMeadowCoin(btn.cost);
            }
            else if (desiredCape != null && me.GetOnlineCreature() is OnlineCreature cc && cc.TryGetData<SlugcatCustomization>(out var custom))
            {
                custom.eventCape = desiredCape;
                CapeManager.RefreshGraphicalModule(cc.realizedCreature);
                if (!purchased) SpecialEvents.SpendMeadowCoin(btn.cost);
                RainMeadow.rainMeadowOptions.currentlyActiveCapeColor.Value = btn.chosenColor;
            }

            if (btn.permanentPurchase is not null) btn.permanentPurchase.Value = true;
            RainMeadow.rainMeadowOptions.config.Save();
            btn.UpdateText();
        }

        public override void Update()
        {
            base.Update();
            if (meadowCoinValue != null)
            {
                meadowCoinValue.text = $"¤{RainMeadow.rainMeadowOptions.MeadowCoins.Value}";
            }

            foreach (var btn in storeItemList)
            {
                btn.buttonBehav.greyedOut = me != null
                    ? (btn.permanentPurchase?.Value == true ? false : RainMeadow.rainMeadowOptions.MeadowCoins.Value < btn.cost)
                    : true;
            }
        }
    }
}