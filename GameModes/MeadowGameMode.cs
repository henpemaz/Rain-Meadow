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
        public MeadowGameMode(Lobby lobby) : base(lobby)
        {
            
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.MeadowMenu;
        }

        internal override void NewEntity(OnlineEntity oe)
        {
            base.NewEntity(oe);
            if (oe is OnlineCreature oc)
            {
                RainMeadow.Debug("Registering new creature: " + oc);
                oe.AddData(new MeadowCreatureData(oc));
            }
        }

        public override AbstractCreature SpawnAvatar(RainWorldGame game, WorldCoordinate location)
        {
            var settings = (avatarSettings as MeadowAvatarSettings);
            var skinData = MeadowProgression.skinData[settings.skin];
            var abstractCreature = new AbstractCreature(game.world, StaticWorld.GetCreatureTemplate(skinData.creatureType), null, location, new EntityID(-1, 0));
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
                lobby.AddData<MeadowLobbyData>();
            }
            else if (res is WorldSession ws)
            {
                ws.AddData<MeadowWorldData>();
            }
            else if (res is RoomSession rs)
            {
                rs.AddData<MeadowRoomData>();
            }
        }

        internal override void ResourceActive(OnlineResource res)
        {
            base.ResourceActive(res);
            if (res is Lobby lobby)
            {
                RainMeadow.Debug("Adding persona settings!");
                var def = new MeadowPersonaSettingsDefinition(new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, OnlineEntity.EntityId.IdType.unique, 0), OnlineManager.mePlayer, false);
                avatarSettings = new MeadowAvatarSettings(def);
                avatarSettings.EnterResource(lobby);

                MeadowLobbyData data = lobby.GetData<MeadowLobbyData>();
                RainMeadow.Debug(lobby.subresources.Count);
                data.itemsPerRegion = new ushort[lobby.subresources.Count]; // count available here

                if (lobby.isOwner)
                {
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
                    RainMeadow.Debug("Region index: " + ws.ShortId());
                    var toSpawn = (ws.super as Lobby).GetData<MeadowLobbyData>().itemsPerRegion[ws.ShortId()];
                    RainMeadow.Debug("Region items: " + toSpawn);
                    var shelters = new HashSet<int>(ws.world.shelters);
                    var gates = new HashSet<int>(ws.world.gates);
                    var validRooms = ws.world.abstractRooms.Where(r => !shelters.Contains(r.index) && !gates.Contains(r.index)).ToArray();
                    var perNode = toSpawn / (double)validRooms.Select(r => r.nodes.Length).Sum();
                    var stacker = 0d;
                    for (int i = 0; i < validRooms.Length; i++)
                    {
                        var r = validRooms[i];
                        stacker += perNode * r.nodes.Length;
                        var n = (ushort)stacker;
                        for (int k = 0; k < n; k++)
                        {
                            var e = new AbstractMeadowCollectible(r.world, RainMeadow.Ext_PhysicalObjectType.MeadowPlant, new WorldCoordinate(r.index, -1, -1, 0), r.world.game.GetNewID(), false);
                            r.AddEntity(e);
                            data.spawnedItems += 1;
                        }
                        stacker -= (ushort)stacker;
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
    }
}
