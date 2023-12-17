using System;
using System.IO;
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

    public class AbstractMeadowCollectible : AbstractPhysicalObject
    {
        bool collectedLocally;
        bool collected;
        int collectedAt;
        TickReference collectedTR;
        public AbstractMeadowCollectible(World world, AbstractObjectType type, WorldCoordinate pos, EntityID ID) : base(world, type, null, pos, ID)
        {

        }

        public override void Update(int time)
        {
            base.Update(time);
            // todo expire
        }

        public void Collect()
        {
            if(collected) { return; }
            collected = true;
            collectedAt = world.game.clock;
            collectedTR = world.GetResource().owner.MakeTickReference();
        }

        public override void Realize()
        {
            if (this.realizedObject != null)
            {
                return;
            }
            if (type == RainMeadow.Ext_PhysicalObjectType.MeadowPlant)
            {
                this.realizedObject = new MeadowPlant(this);
            }
        }
    }

    public abstract class MeadowCollectible : PhysicalObject
    {
        public AbstractMeadowCollectible abstractCollectible => this.abstractPhysicalObject as AbstractMeadowCollectible;
        public MeadowCollectible(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            RainMeadow.Debug("MeadowCollectible");
            this.bodyChunks = new BodyChunk[1] { new BodyChunk(this, 0, default, 1, 1) };
            this.bodyChunkConnections = new BodyChunkConnection[0];
            this.firstChunk.pos = this.abstractPhysicalObject.Room.realizedRoom.RandomPos();
            RainMeadow.Debug("Realized at pos " + this.firstChunk.pos);

            this.gravity = 0;
        }
    }

    public class MeadowPlant : MeadowCollectible
    {
        public MeadowPlant(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            RainMeadow.Debug("MeadowPlant");
        }

        public override void InitiateGraphicsModule()
        {
            RainMeadow.Debug("InitiateGraphicsModule");
            if (base.graphicsModule == null)
            {
                this.graphicsModule = new MeadowPlantGraphics(this);
            }
        }

        public class MeadowPlantGraphics : GraphicsModule
        {
            public Vector2 pos;
            public MeadowPlantGraphics(PhysicalObject ow) : base(ow, false)
            {
                RainMeadow.Debug("MeadowPlantGraphics");
                this.pos = ow.firstChunk.pos;
                this.LoadFile("meadowPlant");
            }

            public void LoadFile(string fileName)
            {
                if (Futile.atlasManager.GetAtlasWithName(fileName) != null)
                {
                    return;
                }
                string str = AssetManager.ResolveFilePath("illustrations" + Path.DirectorySeparatorChar.ToString() + fileName + ".png");
                Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + str, true, true);
                HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, texture, false); // todo this becomes a spritesheet
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                RainMeadow.Debug("MeadowPlantGraphics.InitiateSprites");
                base.InitiateSprites(sLeaser, rCam);
                sLeaser.sprites = new FSprite[3];
                sLeaser.sprites[0] = new FSprite("meadowPlant");
                sLeaser.sprites[0].shader = owner.room.game.rainWorld.Shaders["RM_LeveIltem"];

                sLeaser.sprites[1] = new FSprite("Futile_White");
                sLeaser.sprites[1].shader = owner.room.game.rainWorld.Shaders["FlatLightBehindTerrain"];
                sLeaser.sprites[1].scale = 75f / 16f;
                sLeaser.sprites[1].alpha = (0.4f);
                sLeaser.sprites[1].color = RWCustom.Custom.HSL2RGB(RainWorld.AntiGold.hue, 0.6f, 0.2f);

                sLeaser.sprites[2] = new FSprite("Futile_White");
                sLeaser.sprites[2].shader = owner.room.game.rainWorld.Shaders["FlatLightBehindTerrain"];
                sLeaser.sprites[2].scale = 75f / 16f;
                sLeaser.sprites[2].alpha = (0.4f);
                sLeaser.sprites[2].color = RainWorld.GoldRGB; // maybe "effect color"

                this.AddToContainer(sLeaser, rCam, null);
                this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            }

            public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
                base.AddToContainer(sLeaser, rCam, newContatiner);
                rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[0]);
                rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[1]);
                rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[2]);
            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
                base.ApplyPalette(sLeaser, rCam, palette);
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                Vector2 newPos = this.pos - camPos;
                sLeaser.sprites[0].SetPosition(newPos);
                sLeaser.sprites[1].SetPosition(newPos);
                sLeaser.sprites[2].SetPosition(newPos);
                if (owner.slatedForDeletetion || owner.room != rCam.room)
                {
                    sLeaser.CleanSpritesAndRemove();
                }
            }
        }
    }
}
