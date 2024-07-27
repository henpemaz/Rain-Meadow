using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowGameMode : OnlineGameMode
    {
        public bool spawnPlants = true;

        public MeadowGameMode(Lobby lobby) : base(lobby)
        {
            MeadowProgression.LoadProgression();
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.MeadowMenu;
        }

        internal override void NewEntity(OnlineEntity oe, OnlineResource inResource)
        {
            base.NewEntity(oe, inResource);
            if (oe is OnlineCreature oc)
            {
                RainMeadow.Debug("Registering new creature: " + oc);
                oe.AddData(new MeadowCreatureData(oc));
                oe.AddData(new MeadowMusicData(oc));
                if (oc.realizedCreature != null && EmoteDisplayer.map.TryGetValue(oc.realizedCreature, out var d))
                {
                    d.ownerEntity = oc;
                    d.creatureData = oe.GetData<MeadowCreatureData>();
                }
            }
        }

        public override AbstractCreature SpawnAvatar(RainWorldGame game, WorldCoordinate location)
        {
            if (location.room == MeadowProgression.progressionData.currentCharacterProgress.saveLocation.room) location = MeadowProgression.progressionData.currentCharacterProgress.saveLocation;
            var settings = (clientSettings as MeadowAvatarSettings);
            var skinData = MeadowProgression.skinData[settings.skin];
            var abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(skinData.creatureType), null, location, new EntityID(-1, 0) { altSeed = skinData.randomSeed });
            if (skinData.creatureType == CreatureTemplate.Type.Slugcat)
            {
                abstractCreature.state = new PlayerState(abstractCreature, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer, false);
                game.session.AddPlayer(abstractCreature);
            }
            else
            {
                game.GetStorySession.playerSessionRecords[0] = new PlayerSessionRecord(0);
                game.GetStorySession.playerSessionRecords[0].wokeUpInRegion = game.world.region.name;
            }
            game.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);

            return abstractCreature;
        }

        internal override void ResourceAvailable(OnlineResource res)
        {
            base.ResourceAvailable(res);
            if (res is Lobby lobby)
            {
                lobby.AddData<MeadowLobbyData>(true);
            }
            else if (res is WorldSession ws)
            {
                ws.AddData<MeadowWorldData>(true);
            }
            else if (res is RoomSession rs)
            {
                rs.AddData<MeadowRoomData>(true);
            }
        }

        internal override void AddAvatarSettings()
        {
            RainMeadow.Debug("Adding avatar settings!");
            MeadowAvatarSettings meadowAvatarSettings = new MeadowAvatarSettings(new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, OnlineEntity.EntityId.IdType.settings, 0), OnlineManager.mePlayer);
            clientSettings = meadowAvatarSettings;
            if (!RWCustom.Custom.rainWorld.setup.startScreen) // skipping all menus
            {
                meadowAvatarSettings.skin = MeadowProgression.currentTestSkin;
            }
            clientSettings.EnterResource(lobby);
        }

        internal override void ResourceActive(OnlineResource res)
        {
            base.ResourceActive(res);
            if (res is Lobby lobby)
            {
                MeadowLobbyData lobbyData = lobby.GetData<MeadowLobbyData>();

                var totalRooms = 0;
                var totalRegions = lobby.subresources.Count;

                lobbyData.regionSpawnWeights = new float[totalRegions];
                if (lobby.isOwner)
                {
                    lobbyData.regionRedTokensGoal = new ushort[totalRegions];
                    lobbyData.regionBlueTokensGoal = new ushort[totalRegions];
                    lobbyData.regionGoldTokensGoal = new ushort[totalRegions];
                } // otherwise the above is initialized on state receive
                    
                for (int i = 0; i < totalRegions; i++)
                {
                    var region = (lobby.subresources[i] as WorldSession).region;
                    totalRooms += region.numberOfRooms;
                }

                // around 2 items per room
                lobbyData.redTokensGoal = (int)(16 * totalRegions + 0.8f * totalRooms);
                lobbyData.blueTokensGoal = (int)(12 * totalRegions + 0.6f * totalRooms);
                lobbyData.goldTokensGoal = (int)(8 * totalRegions + 0.4f * totalRooms);
                for (int i = 0; i < totalRegions; i++)
                {
                    var region = (lobby.subresources[i] as WorldSession).region;
                    var spawnWeight = 0.5f / totalRegions + 0.5f * region.numberOfRooms / (float)totalRooms;

                    lobbyData.regionSpawnWeights[i] = spawnWeight;
                    if (lobby.isOwner)
                    {
                        // default starting values
                        lobbyData.regionRedTokensGoal[i] = (ushort)(lobbyData.redTokensGoal * spawnWeight);
                        lobbyData.regionBlueTokensGoal[i] = (ushort)(lobbyData.blueTokensGoal * spawnWeight);
                        lobbyData.regionGoldTokensGoal[i] = (ushort)(lobbyData.goldTokensGoal * spawnWeight);
                    } // otherwise received from owner
                }
            }
            else if (res is WorldSession ws)
            {
                MeadowWorldData regionData = ws.GetData<MeadowWorldData>();
                RainMeadow.Debug("Processing world data for " + ws);
                var shelters = new HashSet<int>(ws.world.shelters);
                var gates = new HashSet<int>(ws.world.gates);
                var validRooms = ws.world.abstractRooms.Where(r => !shelters.Contains(r.index) && !gates.Contains(r.index)).ToArray();
                var roomCount = validRooms.Length;
                var nodeCount = validRooms.Select(r => r.nodes.Length).Sum();
                regionData.roomWeights = new float[roomCount];
                regionData.validRooms = validRooms;

                for (int i = 0; i < validRooms.Length; i++)
                {
                    var r = validRooms[i];
                    regionData.roomWeights[i] = 0.5f / roomCount + 0.5f * r.nodes.Length / nodeCount;
                }

                if (ws.isOwner && spawnPlants)
                {
                    int SpawnItems(int toSpawn, AbstractPhysicalObject.AbstractObjectType type)
                    {
                        RainMeadow.Debug($"Spawning {toSpawn} {type}");
                        var spawnedItems = validRooms.Select(r=>r.entities.Count(e=>e is AbstractPhysicalObject apo && apo.type == type)).Sum();
                        toSpawn -= spawnedItems;

                        var perRoom = 0.5 * toSpawn / (double)roomCount;
                        var perNode = 0.5 * toSpawn / (double)nodeCount;
                        var stacker = 0d;
                        for (int i = 0; i < validRooms.Length; i++)
                        {
                            var r = validRooms[i];
                            stacker += perRoom + perNode * r.nodes.Length;
                            var n = (ushort)stacker;
                            for (int k = 0; k < n; k++)
                            {
                                var e = new AbstractMeadowCollectible(r.world, type, new WorldCoordinate(r.index, -1, -1, 0), r.world.game.GetNewID());
                                r.AddEntity(e);
                                spawnedItems += 1;
                            }
                            stacker -= (ushort)stacker;
                        }
                        return spawnedItems;
                    }

                    var lobbyData = (ws.super as Lobby).GetData<MeadowLobbyData>();
                    SpawnItems(lobbyData.regionRedTokensGoal[ws.ShortId()], RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed);
                    SpawnItems(lobbyData.regionGoldTokensGoal[ws.ShortId()], RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue);
                    SpawnItems(lobbyData.regionBlueTokensGoal[ws.ShortId()], RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold);
                }
            }
            else if (res is RoomSession rs)
            {
                var mrd = rs.GetData<MeadowRoomData>();

                RainMeadow.Debug("Registering places in room " + rs);

                var self = rs.absroom.realizedRoom;
                bool ValidPlacement(int x, int y)
                {
                    var terrain = self.GetTile(x, y).Terrain;
                    if (terrain != Room.Tile.TerrainType.ShortcutEntrance && terrain != Room.Tile.TerrainType.Solid && terrain != Room.Tile.TerrainType.Slope)
                    {
                        var bottom = self.GetTile(x, y - 1).Terrain;
                        var top = self.GetTile(x, y + 1).Terrain;

                        return (bottom == Room.Tile.TerrainType.ShortcutEntrance || bottom == Room.Tile.TerrainType.Solid || bottom == Room.Tile.TerrainType.Slope)
                            && !(top == Room.Tile.TerrainType.ShortcutEntrance || top == Room.Tile.TerrainType.Solid || top == Room.Tile.TerrainType.Slope);
                        // dynamic directions.... one day maybe
                        //var anySolid = false;
                        //var anyAir = false;
                        //for (int i = 0; i < 4; i++)
                        //{
                        //    var target = self.GetTile(pos + Custom.fourDirections[i]);
                        //    var solid = terrain == Room.Tile.TerrainType.ShortcutEntrance || terrain == Room.Tile.TerrainType.Solid || terrain == Room.Tile.TerrainType.Slope;
                        //    anySolid |= solid;
                        //    anyAir |= !solid;
                        //}
                        //return anySolid && anyAir;
                    }
                    return false;
                }

                var line5 = RainMeadow.line5.GetValue(self, (r) => throw new Exception());
                string[] array4 = line5.Split(new char[] { '|' });
                for (int j = 0; j < array4.Length - 1; j++)
                {
                    var parts = array4[j].Split(new char[] { ',' }).Select(s => Convert.ToInt32(s, CultureInfo.InvariantCulture)).ToArray();
                    var x = parts[1] - 1;
                    var y = self.Height - parts[2];
                    bool rare = parts[0] == 1;

                    if (ValidPlacement(x, y))
                    {
                        mrd.AddItemPlacement(x, y, rare);
                    }
                }
                var density = Mathf.Min(self.roomSettings.RandomItemDensity, 0.2f);
                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(self.abstractRoom.index);
                // todo improve this
                //for (int num16 = (int)((float)self.TileWidth * (float)self.TileHeight * Mathf.Pow(density, 2f) / 5f); num16 >= 0; num16--)
                var target = 2 + self.abstractRoom.nodes.Length;
                while (mrd.NumberOfPlaces < target)
                {
                    IntVector2 intVector = self.RandomTile();
                    if (ValidPlacement(intVector.x, intVector.y))
                    {
                        mrd.AddItemPlacement(intVector.x, intVector.y, UnityEngine.Random.value < SlugcatStats.SpearSpawnModifier(self.game.StoryCharacter, density));
                    }
                }
                UnityEngine.Random.state = state;

                RainMeadow.Debug($"{mrd.NumberOfPlaces} places registered");
            }
        }

        static int RandomIndexFromWeightedList(float[] weights) // weights must add up to 1
        {
            var rnd = UnityEngine.Random.value;
            float weight = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                weight += weights[i];
                if (weight > rnd) return i;
            }
            return weights.Length - 1; // exact 1 or fpe
        }

        [RPCMethod]
        public static void ItemConsumed(RPCEvent evt, byte region, AbstractPhysicalObject.AbstractObjectType type)
        {
            if (OnlineManager.lobby.isOwner && OnlineManager.lobby.isActive)
            {
                RainMeadow.Debug($"Item consumed: {OnlineManager.lobby.subresources[region].Id()} {type} from {evt.from}");
                var lobbyData = OnlineManager.lobby.GetData<MeadowLobbyData>();
                var newRegion = RandomIndexFromWeightedList(lobbyData.regionSpawnWeights);
                if (type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed)
                {
                    lobbyData.regionRedTokensGoal[region] -= 1;
                    lobbyData.regionRedTokensGoal[newRegion] += 1;
                    OnlineManager.lobby.subresources[newRegion].owner?.InvokeRPC(SpawnItem, (byte)newRegion, type);
                }
                if (type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue)
                {
                    lobbyData.regionBlueTokensGoal[region] -= 1;
                    lobbyData.regionBlueTokensGoal[newRegion] += 1;
                    OnlineManager.lobby.subresources[newRegion].owner?.InvokeRPC(SpawnItem, (byte)newRegion, type);
                }
                if (type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold)
                {
                    lobbyData.regionGoldTokensGoal[region] -= 1;
                    lobbyData.regionGoldTokensGoal[newRegion] += 1;
                    OnlineManager.lobby.subresources[newRegion].owner?.InvokeRPC(SpawnItem, (byte)newRegion, type);
                }
            }
        }

        [RPCMethod]
        public static void SpawnItem(RPCEvent evt, byte region, AbstractPhysicalObject.AbstractObjectType type)
        {
            if (OnlineManager.lobby.isActive)
            {
                RainMeadow.Debug($"Item respawning: {OnlineManager.lobby.subresources[region].Id()} {type} from {evt.from}");
                var ws = OnlineManager.lobby.subresources[region] as WorldSession;
                if (ws.isActive && ws.isOwner)
                {
                    var regionData = ws.GetData<MeadowWorldData>();
                    var newRoom = RandomIndexFromWeightedList(regionData.roomWeights);
                    var r = regionData.validRooms[newRoom];
                    if (type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed || type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue || type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold)
                    {
                        new AbstractMeadowCollectible(r.world, type, new WorldCoordinate(r.index, -1, -1, 0), r.world.game.GetNewID());
                    }
                }
            }
        }

        internal override void Customize(Creature creature, OnlineCreature oc)
        {
            if (lobby.playerAvatars[oc.owner] == oc.id)
            {
                RainMeadow.Debug($"Customizing avatar {creature} for {oc.owner}");
                var settings = lobby.activeEntities.First(em => em is ClientSettings avs && avs.avatarId == oc.id) as MeadowAvatarSettings;

                var mcc = (MeadowAvatarCustomization)RainMeadow.creatureCustomizations.GetValue(creature, (c) => settings.MakeCustomization());

                if (oc.TryGetData<MeadowCreatureData>(out var mcd))
                {
                    EmoteDisplayer.map.GetValue(creature, (c) => new EmoteDisplayer(creature, oc, mcd, mcc));
                }
                else
                {
                    RainMeadow.Error("missing mcd?? " + oc);
                }
                // playable creatures
                CreatureController.BindAvatar(creature, oc);
            }
            else
            {
                RainMeadow.Error("creature not avatar ?? " + oc);
            }
        }
    }
}
