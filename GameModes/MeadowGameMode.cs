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
                if (oc.realizedCreature != null && EmoteDisplayer.map.TryGetValue(oc.realizedCreature, out var d))
                {
                    d.ownerEntity = oc;
                    d.creatureData = oe.GetData<MeadowCreatureData>();
                }
                if (CreatureController.creatureControllers.TryGetValue(oc.creature, out var cc))
                {
                    cc.onlineCreature = oc;
                }
            }
        }

        public override AbstractCreature SpawnAvatar(RainWorldGame game, WorldCoordinate location)
        {
            var settings = (clientSettings as MeadowAvatarSettings);
            var skinData = MeadowProgression.skinData[settings.skin];
            var abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(skinData.creatureType), null, location, new EntityID(-1, 0) { altSeed = skinData.randomSeed });
            if (skinData.creatureType == CreatureTemplate.Type.Slugcat)
            {
                abstractCreature.state = new PlayerState(abstractCreature, 0, skinData.statsName, false);
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
            MeadowAvatarSettings meadowAvatarSettings = new MeadowAvatarSettings(
                            new MeadowAvatarSettings.Definition(
                                new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, OnlineEntity.EntityId.IdType.settings, 0)
                                , OnlineManager.mePlayer));
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
                MeadowLobbyData data = lobby.GetData<MeadowLobbyData>();
                if (lobby.isOwner)
                {
                    RainMeadow.Debug("Processing lobby data");
                    data.itemsPerRegion = new ushort[lobby.subresources.Count]; // count available here
                    for (int i = 0; i < lobby.subresources.Count; i++)
                    {
                        data.itemsPerRegion[i] = 200;
                    }
                }
            }
            else if (res is WorldSession ws)
            {
                MeadowWorldData data = ws.GetData<MeadowWorldData>();
                if (ws.isOwner)
                {
                    RainMeadow.Debug("Processing world data");
                    RainMeadow.Debug("Region index: " + ws.ShortId());
                    var toSpawn = (ws.super as Lobby).GetData<MeadowLobbyData>().itemsPerRegion[ws.ShortId()];
                    RainMeadow.Debug("Region items: " + toSpawn);
                    var shelters = new HashSet<int>(ws.world.shelters);
                    var gates = new HashSet<int>(ws.world.gates);
                    var validRooms = ws.world.abstractRooms.Where(r => !shelters.Contains(r.index) && !gates.Contains(r.index)).ToArray();
                    var perRoom = 0.5 * toSpawn / (double)validRooms.Length;
                    var perNode = 0.5 * toSpawn / (double)validRooms.Select(r => r.nodes.Length).Sum();
                    var stacker = 0d;
                    if (spawnPlants)
                    {
                        for (int i = 0; i < validRooms.Length; i++)
                        {
                            var r = validRooms[i];
                            stacker += perRoom + perNode * r.nodes.Length;
                            var n = (ushort)stacker;
                            for (int k = 0; k < n; k++)
                            {
                                var type = UnityEngine.Random.value switch
                                {
                                    < 0.25f =>
                                        RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed,
                                    < 0.5f =>
                                        RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue,
                                    < 0.75f =>
                                        RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold,
                                    _ =>
                                        RainMeadow.Ext_PhysicalObjectType.MeadowPlant,
                                };
                                var e = new AbstractMeadowCollectible(r.world, type, new WorldCoordinate(r.index, -1, -1, 0), r.world.game.GetNewID());
                                r.AddEntity(e);
                                data.spawnedItems += 1;
                            }
                            stacker -= (ushort)stacker;
                        }
                    }
                    RainMeadow.Debug($"Region items: {toSpawn} spawned {data.spawnedItems}");
                }
            }
            else if (res is RoomSession rs)
            {
                var mrd = rs.GetData<MeadowRoomData>();

                RainMeadow.Debug("Registering places in room " + rs);
                var self = rs.absroom.realizedRoom;
                var line5 = RainMeadow.line5.GetValue(self, (r) => throw new Exception());
                //RainMeadow.line5.Remove(self); this breaks in gates, reuses same room activates twice
                string[] array4 = line5.Split(new char[] { '|' });
                for (int j = 0; j < array4.Length - 1; j++)
                {
                    var parts = array4[j].Split(new char[] { ',' }).Select(s => Convert.ToInt32(s, CultureInfo.InvariantCulture)).ToArray();
                    var x = parts[1] - 1;
                    var y = self.Height - parts[2];
                    bool rare = parts[0] == 1;

                    mrd.AddItemPlacement(x, y, rare);
                }
                var density = Mathf.Min(self.roomSettings.RandomItemDensity, 0.2f);
                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(self.abstractRoom.index);
                //for (int num16 = (int)((float)self.TileWidth * (float)self.TileHeight * Mathf.Pow(density, 2f) / 5f); num16 >= 0; num16--)

                // todo improve this
                while (mrd.NumberOfPlaces < self.abstractRoom.nodes.Length)
                {
                    IntVector2 intVector = self.RandomTile();
                    if (!self.GetTile(intVector).Solid)
                    {
                        bool flag5 = true;
                        if (self.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.ZeroG) < 1f || self.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.BrokenZeroG) > 0f)
                        {
                            for (int num17 = -1; num17 < 2; num17++)
                            {
                                if (!self.GetTile(intVector + new IntVector2(num17, -1)).Solid)
                                {
                                    flag5 = false;
                                    break;
                                }
                            }
                        }
                        if (flag5)
                        {
                            mrd.AddItemPlacement(intVector.x, intVector.y, UnityEngine.Random.value < SlugcatStats.SpearSpawnModifier(self.game.StoryCharacter, density));
                        }
                    }
                }
                UnityEngine.Random.state = state;
            }
        }

        internal override void Customize(Creature creature, OnlineCreature oc)
        {
            if (lobby.playerAvatars.Any(a => a.Value == oc.id)) // little cache
            {
                RainMeadow.Debug($"Customizing avatar {creature} for {oc.owner}");
                var settings = lobby.entities.Values.First(em => em.entity is ClientSettings avs && avs.avatarId == oc.id).entity as MeadowAvatarSettings;

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
                RainMeadow.Error("missing mas?? " + oc);
            }
        }
    }
}
