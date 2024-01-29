using System;
using System.Linq;

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
                oe.gameModeData = new MeadowCreatureData(oc);
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
                lobby.resourceData = new MeadowLobbyData(lobby);
            }
            else if (res is WorldSession ws)
            {
                ws.resourceData = new MeadowWorldData(ws);
            }
            else if (res is RoomSession rs)
            {

            }
        }

        internal override void ResourceActive(OnlineResource res)
        {
            base.ResourceAvailable(res);
            if (res is Lobby lobby)
            {
                RainMeadow.Debug("Adding persona settings!");
                var def = new MeadowPersonaSettingsDefinition(new OnlineEntity.EntityId(OnlineManager.mePlayer.inLobbyId, OnlineEntity.EntityId.IdType.unique, 0), OnlineManager.mePlayer, false);
                avatarSettings = new MeadowAvatarSettings(def);
                avatarSettings.EnterResource(lobby);

                MeadowLobbyData data = lobby.resourceData as MeadowLobbyData;
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
                MeadowWorldData data = ws.resourceData as MeadowWorldData;
                data.itemsPerRoom = new ushort[ws.subresources.Count];
                if (ws.isOwner)
                {
                    RainMeadow.Debug("Region index: " + ws.ShortId());
                    var toSpawn = ((ws.super as Lobby).resourceData as MeadowLobbyData).itemsPerRegion[ws.ShortId()];
                    RainMeadow.Debug("Region items: " + toSpawn);
                    var perNode = toSpawn / (float)ws.roomSessions.Values.Select(rs => rs.absroom.nodes.Length).Sum();
                    var stacker = 0f;
                    for (int i = 0; i < ws.subresources.Count; i++)
                    {
                        stacker += perNode * (ws.subresources[i] as RoomSession).absroom.nodes.Length;
                        data.itemsPerRoom[i] = (ushort)stacker;
                        stacker -= (ushort)stacker;
                    }
                }
            }
            else if (res is RoomSession rs)
            {
                if (rs.isOwner)
                {
                    var toSpawn = ((rs.super as WorldSession).resourceData as MeadowWorldData).itemsPerRoom[rs.absroom.index - rs.absroom.world.firstRoomIndex];
                    RainMeadow.Debug("Room items: " + toSpawn);
                    for (int i = 0; i < toSpawn; i++)
                    {
                        // missing what
                        // is it a physob in the room
                        // is it random placement
                        // if I do it only in the room, how well will the timeout work
                        var e = new AbstractMeadowCollectible(rs.absroom.world, RainMeadow.Ext_PhysicalObjectType.MeadowPlant, new WorldCoordinate(rs.absroom.index, -1, -1, 0), rs.absroom.world.game.GetNewID());
                        rs.absroom.AddEntity(e);
                        e.RealizeInRoom();
                    }
                }
            }
        }
    }
}
