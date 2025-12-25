using Menu;
using System.Collections.Generic;
using UnityEngine;
namespace RainMeadow.Arena.ArenaOnlineGameModes.Drown
{
    public class StoreOverlay : Menu.Menu
    {
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
            public bool mutedPlayer;
            private string clientMuteSymbol;
            public Dictionary<string, int> storeItems;
            public StoreOverlay overlay;
            public int cost;
            public string name;
            public bool didRespawn;
            public bool RequiresWatcher => ModManager.Watcher;
            public bool RequiresMSC => ModManager.MSC;
            public ItemButton(StoreOverlay menu, Vector2 pos, RainWorldGame game, ArenaOnlineGameMode arena, DrownMode drown, KeyValuePair<string, int> itemEntry, int index, bool canBuy = false)
            {
                bool teamWork = !game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers;
                this.overlay = menu;
                this.name = itemEntry.Key;
                this.cost = itemEntry.Value;
                this.button = new SimplerButton(menu, menu.pages[0], $"{itemEntry.Key}: {itemEntry.Value}", pos, new Vector2(110, 30));
                this.button.OnClick += (_) =>
                {
                    AbstractPhysicalObject desiredObject = null;
                    for (int i = 0; i < game.GetArenaGameSession.Players.Count; i++)
                    {
                        if (game.GetArenaGameSession.Players[i].IsLocal())
                        {
                            drown.meCreature = game.GetArenaGameSession.Players[i];
                        }
                    }
                    if (drown.meCreature == null) {
                        return;
                    }

                    switch (itemEntry.Key)
                    {
                        case Spear:
                            desiredObject = new AbstractSpear(game.world, null, drown.meCreature.pos, game.GetNewID(), false);
                            break;
                        case ExplosiveSpear:
                            desiredObject = new AbstractSpear(game.world, null, drown.meCreature.pos, game.GetNewID(), true);
                            break;
                        case ScavengerBomb:
                            desiredObject = new AbstractPhysicalObject(game.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, drown.meCreature.pos, game.GetNewID());
                            break;
                        case ElectricSpear:
                            desiredObject = new AbstractSpear(game.world, null, drown.meCreature.pos, game.GetNewID(), false, true);
                            break;
                        case Boomerang:
                            desiredObject = new AbstractPhysicalObject(game.world, Watcher.WatcherEnums.AbstractObjectType.Boomerang, null, drown.meCreature.pos, game.GetNewID());
                            break;

                        case Respawn:

                            didRespawn = false;
                            if (!didRespawn)
                            {
                                RevivePlayer(game.GetArenaGameSession, arena, drown);
                                didRespawn = true;
                            }

                            break;
                        case OpenDens:
                            OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs2);
                            if (cs2 != null)
                            {

                                cs2.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                                if (clientSettings != null)
                                {
                                    clientSettings.iOpenedDen = true;
                                }
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
                            game.cameras[0].hud.PlaySound(SoundID.UI_Multiplayer_Player_Revive);
                            break;
                        case Rock:
                            desiredObject = new AbstractPhysicalObject(game.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, drown.meCreature.pos, game.GetNewID());
                            break;
                    }

                    if (desiredObject != null)
                    {
                        game.cameras[0].room.abstractRoom.AddEntity(desiredObject);
                        desiredObject.RealizeInRoom();
                    }

                    OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                    if (cs != null)
                    {

                        cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                        if (clientSettings != null)
                        {
                            clientSettings.score = clientSettings.score - itemEntry.Value;
                        }
                    }
                    didRespawn = false;

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
        public DrownMode drown;

        public StoreOverlay(ProcessManager manager, RainWorldGame game, DrownMode drown, ArenaOnlineGameMode arena) : base(manager, RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            this.drown = drown;
            this.pages.Add(new Page(this, null, "store", 0));
            this.selectedObject = null;
            this.storeItemList = new();
            this.pos = new Vector2(180, 553);
            this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("STORE"), new Vector2(pos.x, pos.y + 30f), new Vector2(110, 30), true));
            var storeItems = new Dictionary<string, int> {
            { "Spear", drown.spearCost },
            { "Rock", drown.rockCost},
            { "Explosive Spear", drown.spearExplCost },
            { "Scavenger Bomb", drown.bombCost },
            { "Electric Spear", drown.electricSpearCost },
            { "Boomerang", drown.boomerangeCost },
            { "Respawn", drown.respCost},
            { "Open Dens", drown.denCost},


        };
            int index = 0; // Initialize an index variable

            foreach (var item in storeItems)
            {
                // Format the message for the button, for example: "Spear: 1"
                string buttonMessage = $"{item.Key}: {item.Value}";

                // Create a new ItemButton for each dictionary entry
                this.itemButtons = new ItemButton(this, pos, game, arena, drown, item, index, true);
                this.storeItemList.Add(itemButtons);


                pos.y -= 40; // Move the button 40 units down for the next one
                index++;
            }

        }

        public override void Update()
        {
            base.Update();
            bool teamWork = !game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers;
            if (RainMeadow.isArenaMode(out var arena) && (DrownMode.isDrownMode(arena, out var drown)))
            {
                if (storeItemList != null)
                {
                    OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                    if (cs != null)
                    {

                        cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                        if (clientSettings != null)
                        {

                            for (int i = 0; i < storeItemList.Count; i++)
                            {
                                storeItemList[i].button.buttonBehav.greyedOut = true;

                                if (storeItemList[i].name == "Respawn")
                                {
                                    if (drown.meCreature is not null && drown.meCreature.state.alive || drown.openedDen)
                                    {
                                        storeItemList[i].button.buttonBehav.greyedOut = true;
                                    }
                                    else
                                    {
                                        storeItemList[i].button.buttonBehav.greyedOut = (teamWork ? clientSettings.teamScore : clientSettings.score) < storeItemList[i].cost;
                                    }
                                }

                                if (storeItemList[i].name == "Open Dens" && !drown.openedDen)
                                {
                                    storeItemList[i].button.buttonBehav.greyedOut = (teamWork ? clientSettings.teamScore : clientSettings.score) < storeItemList[i].cost;
                                }

                                if (drown.meCreature != null && !drown.openedDen && (storeItemList[i].name != "Respawn" && storeItemList[i].name != "Open Dens"))
                                {
                                    if (storeItemList[i].name == "Electric Spear" && !ModManager.MSC || storeItemList[i].name == "Boomerang" && !ModManager.Watcher)
                                    {
                                        storeItemList[i].button.buttonBehav.greyedOut = true;
                                    }
                                    else
                                    {

                                        storeItemList[i].button.buttonBehav.greyedOut = (teamWork ? clientSettings.teamScore : clientSettings.score) < storeItemList[i].cost;
                                    }
                                }


                            }
                            if (drown.meCreature != null)
                            {
                                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.StoreItem1.Value) && (teamWork ? clientSettings.teamScore : clientSettings.score) >= storeItemList[0].cost)
                                {
                                    storeItemList[0].button.Clicked();
                                }
                                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.StoreItem2.Value) && (teamWork ? clientSettings.teamScore : clientSettings.score) >= storeItemList[1].cost)
                                {
                                    storeItemList[1].button.Clicked();
                                }
                                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.StoreItem3.Value) && (teamWork ? clientSettings.teamScore : clientSettings.score) >= storeItemList[2].cost)
                                {
                                    storeItemList[2].button.Clicked();
                                }
                                if (ModManager.MSC && Input.GetKeyDown(RainMeadow.rainMeadowOptions.StoreItem4.Value) && (teamWork ? clientSettings.teamScore : clientSettings.score) >= storeItemList[3].cost)
                                {
                                    storeItemList[3].button.Clicked();
                                }
                                if (ModManager.Watcher && Input.GetKeyDown(RainMeadow.rainMeadowOptions.StoreItem5.Value) && (teamWork ? clientSettings.teamScore : clientSettings.score) >= storeItemList[4].cost)
                                {
                                    storeItemList[4].button.Clicked();
                                }

                                if (ModManager.Watcher && Input.GetKeyDown(RainMeadow.rainMeadowOptions.StoreItem6.Value) && (teamWork ? clientSettings.teamScore : clientSettings.score) >= storeItemList[5].cost)
                                {
                                    storeItemList[5].button.Clicked();
                                }

                            }
                            if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.StoreItem7.Value) && (teamWork ? clientSettings.teamScore : clientSettings.score) >= storeItemList[6].cost && !drown.openedDen && (drown.meCreature == null || drown.meCreature.state.dead))
                            {
                                storeItemList[6].button.Clicked();
                            }
                            if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.StoreItem8.Value) && (teamWork ? clientSettings.teamScore : clientSettings.score) >= storeItemList[7].cost && !drown.openedDen)
                            {
                                storeItemList[7].button.Clicked();
                            }

                        }
                    }
                }
            }
        }

        private static void RevivePlayer(ArenaGameSession session, ArenaOnlineGameMode arena, DrownMode drown)
        {
        
            List<int> exitList = new List<int>();
            for (int i = 0; i < session.room.world.GetAbstractRoom(0).exits; i++)
            {
                exitList.Add(i);
            }
            arena.avatars.Clear();
            arena.externalArenaGameMode.SpawnPlayer(arena, session, session.room, exitList);
        }
    }
}