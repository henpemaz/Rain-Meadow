using Menu;
using System.Collections.Generic;
using UnityEngine;
using RainMeadow;
using System.Linq;
using MoreSlugcats;
using Drown;

namespace RainMeadow
{
    public class StoreOverlay : Menu.Menu
    {
        public AbstractCreature? foundMe;
        public Vector2 pos;

        public class ItemButton
        {
            public const string Rock = "Rock";
            public const string Spear = "Spear";
            public const string ExplosiveSpear = "Explosive Spear";
            public const string ScavengerBomb = "Scavenger Bomb";
            public const string ElectricSpear = "Electric Spear";
            public const string Boomerang = "Boomerang";
            public const string Respawn = "Respawn";
            public const string OpenDens = "Open Dens";

            public OnlinePhysicalObject player;
            public SimplerButton button;
            public StoreOverlay overlay;

            public int cost;
            public string name;
            public KeyCode hotkey;

            private int lastClickFrame = -1; // Protects against double execution in the same frame

            public bool RequiresWatcher => ModManager.Watcher;
            public bool RequiresMSC => ModManager.MSC;

            public ItemButton(StoreOverlay menu, Vector2 pos, RainWorldGame game, ArenaOnlineGameMode arena, string itemName, int itemCost, KeyCode hotkey)
            {
                this.overlay = menu;
                this.name = itemName;
                this.cost = itemCost;
                this.hotkey = hotkey;

                bool teamWork = !game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers;

                if (DrownMode.isDrownMode(arena, out var drown))
                {
                    this.button = new SimplerButton(menu, menu.pages[0], $"{itemName}: {itemCost}", pos, new Vector2(110, 30));
                    this.button.OnClick += (_) =>
                    {
                        // Prevent running twice in the same frame
                        if (Time.frameCount == lastClickFrame) return;
                        lastClickFrame = Time.frameCount;
                        AbstractCreature me = null;
                        for (int i = 0; i < game.GetArenaGameSession.Players.Count; i++)
                        {
                            if (OnlinePhysicalObject.map.TryGetValue(game.GetArenaGameSession.Players[i], out var onlineP) && onlineP.owner == OnlineManager.mePlayer)
                            {
                                me = game.GetArenaGameSession.Players[i];
                                break;
                            }
                        }

                        AbstractPhysicalObject desiredObject = null;

                        switch (itemName)
                        {
                            case Spear:
                                desiredObject = new AbstractSpear(game.world, null, me.pos, game.GetNewID(), false);
                                break;
                            case ExplosiveSpear:
                                desiredObject = new AbstractSpear(game.world, null, me.pos, game.GetNewID(), true);
                                break;
                            case ScavengerBomb:
                                desiredObject = new AbstractPhysicalObject(game.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, me.pos, game.GetNewID());
                                break;
                            case ElectricSpear:
                                desiredObject = new AbstractSpear(game.world, null, me.pos, game.GetNewID(), false, true);
                                break;
                            case Boomerang:
                                desiredObject = new AbstractPhysicalObject(game.world, Watcher.WatcherEnums.AbstractObjectType.Boomerang, null, me.pos, game.GetNewID());
                                break;
                            case Rock:
                                desiredObject = new AbstractPhysicalObject(game.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, me.pos, game.GetNewID());
                                break;
                            case Respawn:
                                RevivePlayer(game.GetArenaGameSession, arena, drown);
                                break;
                            case OpenDens:
                                HandleOpenDens(arena, drown);
                                game.cameras[0].hud.PlaySound(SoundID.UI_Multiplayer_Player_Revive);
                                break;
                        }

                        // Realize physical objects
                        if (desiredObject != null && me != null)
                        {
                            game.cameras[0].room.abstractRoom.AddEntity(desiredObject);
                            desiredObject.RealizeInRoom();
                        }


                        game.GetArenaGameSession.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].score -= itemCost;
                        me?.GetOnlineCreature()?.BroadcastRPCInRoom(ArenaRPCs.UpdatePlayerScore, ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer), game.GetArenaGameSession.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].score);

                    };

                    this.button.owner.subObjects.Add(button);
                }
            }

            private void HandleOpenDens(ArenaOnlineGameMode arena, DrownMode drown)
            {
                if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs2) &&
                    cs2.TryGetData<ArenaDrownClientSettings>(out var clientSettings))
                {
                    clientSettings.iOpenedDen = true;
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

            public void Destroy()
            {
                this.button.RemoveSprites();
                this.button.page.RemoveSubObject(this.button);
            }
        }

        public RainWorldGame game;
        public List<ItemButton> storeItemList;
        public DrownMode drown;

        public StoreOverlay(ProcessManager manager, RainWorldGame game, DrownMode drown, ArenaOnlineGameMode arena) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            this.drown = drown;
            this.pages.Add(new Page(this, null, "store", 0));
            this.selectedObject = null;
            this.storeItemList = new List<ItemButton>();
            this.pos = new Vector2(180, 553);

            this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("STORE"), new Vector2(pos.x, pos.y + 30f), new Vector2(110, 30), true));

            // Map item data directly to their respective hotkeys to avoid hardcoded indices later
            var storeItemsData = new (string name, int cost, KeyCode hotkey)[] {
                (ItemButton.Spear, drown.spearCost, RainMeadow.rainMeadowOptions.StoreItem1.Value),
                (ItemButton.Rock, drown.rockCost, RainMeadow.rainMeadowOptions.StoreItem2.Value),
                (ItemButton.ExplosiveSpear, drown.spearExplCost, RainMeadow.rainMeadowOptions.StoreItem3.Value),
                (ItemButton.ScavengerBomb, drown.bombCost, RainMeadow.rainMeadowOptions.StoreItem4.Value),
                (ItemButton.ElectricSpear, drown.electricSpearCost, RainMeadow.rainMeadowOptions.StoreItem5.Value),
                (ItemButton.Boomerang, drown.boomerangeCost, RainMeadow.rainMeadowOptions.StoreItem6.Value),
                (ItemButton.Respawn, drown.respCost, RainMeadow.rainMeadowOptions.StoreItem7.Value),
                (ItemButton.OpenDens, drown.denCost, RainMeadow.rainMeadowOptions.StoreItem8.Value)
            };

            foreach (var itemData in storeItemsData)
            {
                var itemBtn = new ItemButton(this, pos, game, arena, itemData.name, itemData.cost, itemData.hotkey);
                this.storeItemList.Add(itemBtn);
                pos.y -= 40; // Move the button 40 units down for the next one
            }
        }

        public override void Update()
        {
            base.Update();

            if (!RainMeadow.isArenaMode(out var arena) || !DrownMode.isDrownMode(arena, out var drownMode))
                return;

            // Robustly find if the local player is currently alive
            bool isAlive = false;
            foreach (var player in game.Players)
            {
                // Check if this player is the local player and is alive
                if (OnlinePhysicalObject.map.TryGetValue(player, out var onlineC) && onlineC.owner == OnlineManager.mePlayer)
                {
                    if (player.state.alive || (player.realizedCreature != null && !player.realizedCreature.dead))
                    {
                        isAlive = true;
                        break; // Found the player and they are alive, no need to check others
                    }
                }
            }

            if (storeItemList == null || storeItemList.Count == 0) return;

            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs) &&
                cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings))
            {
                bool teamWork = !game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers;
                int currentScore = teamWork ? drown.teamPoints : game.GetArenaGameSession.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].score;

                foreach (var item in storeItemList)
                {
                    bool canAfford = currentScore >= item.cost;
                    bool greyedOut = false;

                    // Logic gate for specific buttons
                    switch (item.name)
                    {
                        case ItemButton.Respawn:
                            // Grey out if ALIVE or CANNOT AFFORD or GAME OVER
                            greyedOut = isAlive || !canAfford || drown.openedDen;
                            break;

                        case ItemButton.OpenDens:
                            // Grey out if already opened or cannot afford
                            greyedOut = drownMode.openedDen || !canAfford;
                            break;

                        case ItemButton.ElectricSpear:
                            greyedOut = !ModManager.MSC || !canAfford;
                            break;

                        case ItemButton.Boomerang:
                            greyedOut = !ModManager.Watcher || !canAfford;
                            break;

                        default:
                            greyedOut = !canAfford;
                            break;
                    }

                    // Apply visual state
                    item.button.buttonBehav.greyedOut = greyedOut;

                    // Handle Input (only if not greyed out)
                    if (!greyedOut && Input.GetKeyDown(item.hotkey))
                    {
                        item.button.Clicked();
                    }
                }
            }
        }

        private static void RevivePlayer(ArenaGameSession game, ArenaOnlineGameMode arena, DrownMode drown)
        {
            List<int> exitList = new List<int>();

            for (int i = 0; i < game.room.world.GetAbstractRoom(0).exits; i++)
            {
                exitList.Add(i);
            }

            arena.avatars.Clear();
            arena.externalArenaGameMode.SpawnPlayer(arena, game, game.room, exitList);
            game.Players.Remove(drown.abstractCreatureToRemove);

        }
    }
}