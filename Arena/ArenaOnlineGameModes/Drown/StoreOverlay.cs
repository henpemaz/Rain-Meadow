using Menu;
using System.Collections.Generic;
using UnityEngine;
using MoreSlugcats;
using Drown;
using System.CodeDom;

namespace RainMeadow
{
    public class DrownStoreOverlay : BaseStoreOverlay
    {
        public const string ElectricSpear = "Electric Spear";
        public const string Boomerang = "Boomerang";
        public const string Respawn = "Respawn";
        public const string OpenDens = "Open Dens";

        public List<ArenaItemButton> storeItemList = new();
        public DrownMode drown;

        public class ArenaItemButton : BaseStoreButton
        {
            public KeyCode hotkey;
            private int lastClickFrame = -1;
            private DrownStoreOverlay Store => (DrownStoreOverlay)menu;

            public ArenaItemButton(DrownStoreOverlay menu, MenuObject owner, Vector2 pos, string itemName, int itemCost, KeyCode hotkey)
                : base(menu, owner, pos, itemName, itemCost)
            {
                this.hotkey = hotkey;
                UpdateText();
                owner.subObjects.Add(this);
            }

            public override void UpdateText() => this.menuLabel.text = $"{itemName}: {cost}";

            protected override void OnButtonClick()
            {
                if (Time.frameCount == lastClickFrame) return;
                lastClickFrame = Time.frameCount;
                Store.HandlePurchase(this);
            }
        }

        public DrownStoreOverlay(ProcessManager manager, RainWorldGame game, DrownMode drown, ArenaOnlineGameMode arena)
            : base(manager, game)
        {
            this.drown = drown;
            pages[0].subObjects.Add(new MenuLabel(this, pages[0], this.Translate("STORE"), new Vector2(pos.x, pos.y + 30f), new Vector2(110, 30), true));

            var storeItemsData = new (string name, int cost, KeyCode hotkey)[] {
                (Spear, drown.spearCost, RainMeadow.rainMeadowOptions.StoreItem1.Value),
                (Rock, drown.rockCost, RainMeadow.rainMeadowOptions.StoreItem2.Value),
                (ExplosiveSpear, drown.spearExplCost, RainMeadow.rainMeadowOptions.StoreItem3.Value),
                (ScavengerBomb, drown.bombCost, RainMeadow.rainMeadowOptions.StoreItem4.Value),
                (ElectricSpear, drown.electricSpearCost, RainMeadow.rainMeadowOptions.StoreItem5.Value),
                (Boomerang, drown.boomerangeCost, RainMeadow.rainMeadowOptions.StoreItem6.Value),
                (Respawn, drown.respCost, RainMeadow.rainMeadowOptions.StoreItem7.Value),
                (OpenDens, drown.denCost, RainMeadow.rainMeadowOptions.StoreItem8.Value)
            };

            foreach (var itemData in storeItemsData)
            {
                if (DrownMode.isDrownMode(arena, out var _))
                {
                    storeItemList.Add(new ArenaItemButton(this, pages[0], pos, itemData.name, itemData.cost, itemData.hotkey));
                    pos.y -= 40;
                }
            }
        }

        public void HandlePurchase(ArenaItemButton btn)
        {
            if (!RainMeadow.isArenaMode(out var arena)) return;

            AbstractPhysicalObject? desiredObject = CreateStandardItem(btn.itemName, me);

            if (desiredObject == null)
            {
                switch (btn.itemName)
                {
                    case ElectricSpear:
                        desiredObject = new AbstractSpear(game.world, null, me.pos, game.GetNewID(), false, true);
                        break;
                    case Boomerang:
                        desiredObject = new AbstractPhysicalObject(game.world, Watcher.WatcherEnums.AbstractObjectType.Boomerang, null, me.pos, game.GetNewID());
                        break;
                    case Respawn:
                        RevivePlayer(game.GetArenaGameSession, arena, drown);
                        break;
                    case OpenDens:
                        HandleOpenDens(arena, drown);
                        game.cameras[0].hud.PlaySound(SoundID.UI_Multiplayer_Player_Revive);
                        break;
                }
            }

            if (desiredObject != null)
            {
                FinalizeObjectSpawn(desiredObject);
            }

            var playerSitting = game.GetArenaGameSession.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)];
            playerSitting.score -= btn.cost;

            foreach (var orderId in arena.arenaSittingOnlineOrder)
            {
                OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByLobbyId(orderId);
                if (pl == null || pl.isMe) continue;
                // pl.InvokeOnceRPC(ArenaRPCs.UpdatePlayerScore, ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer), playerSitting.score);
            }
        }

        private void HandleOpenDens(ArenaOnlineGameMode arena, DrownMode drown)
        {
            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs) && cs.TryGetData<ArenaDrownClientSettings>(out var settings))
            {
                settings.iOpenedDen = true;
            }
            drown.openedDen = true;


            for (int j = 0; j < arena.arenaSittingOnlineOrder.Count; j++)
            {
                OnlinePlayer? currentPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, j);
                if (currentPlayer != null && !OnlineManager.lobby.isOwner)
                {
                    OnlineManager.lobby.owner.InvokeOnceRPC(DrownModeRPCs.Arena_OpenDen, drown.openedDen);
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (!RainMeadow.isArenaMode(out var arena) || !DrownMode.isDrownMode(arena, out var drownMode) || storeItemList.Count == 0)
                return;

            bool isAlive = me != null && (me.state.alive || me.realizedCreature?.State?.alive == true);

            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs) && cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings))
            {
                bool teamWork = !game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers;
                int currentScore = teamWork ? drown.teamPoints : game.GetArenaGameSession.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].score;

                foreach (var item in storeItemList)
                {
                    bool canAfford = currentScore >= item.cost;
                    bool greyedOut = item.itemName switch
                    {
                        Respawn => isAlive || !canAfford || drown.openedDen,
                        OpenDens => drownMode.openedDen || !canAfford,
                        ElectricSpear => !ModManager.MSC || !canAfford || !isAlive,
                        Boomerang => !ModManager.Watcher || !canAfford || !isAlive,
                        _ => !canAfford || !isAlive
                    };

                    item.buttonBehav.greyedOut = greyedOut;

                    if (!greyedOut && Input.GetKeyDown(item.hotkey))
                    {
                        item.Clicked();
                    }
                }
            }
        }

        private static void RevivePlayer(ArenaGameSession game, ArenaOnlineGameMode arena, DrownMode drown)
        {
            List<int> exitList = new List<int>();
            for (int i = 0; i < game.room.world.GetAbstractRoom(0).exits; i++) exitList.Add(i);

            arena.avatars.Clear();
            arena.externalArenaGameMode.SpawnPlayer(arena, game, game.room, exitList);

            foreach (var orderId in arena.arenaSittingOnlineOrder)
            {
                OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByLobbyId(orderId);
                if (pl == null || pl.isMe) continue;
                pl.InvokeOnceRPC(DrownModeRPCs.Arena_RemoveAbstractCreatureFromList);
            }
            game.Players.RemoveAll(x => x.state.dead || x.realizedCreature == null || x.realizedCreature.State.dead);
        }
    }
}
