using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class EmoteDisplayer
    {
        public Creature owner;
        private readonly OnlineCreature ownerEntity;
        private readonly MeadowCreatureData creatureData;
        public MeadowCustomization.CreatureCustomization customization;
        private RainWorldGame game;

        public const int maxEmoteCount = 4;
        public const float initialLifetime = 4; // seconds

        public int startInGameClock;
        public float timeToLive;
        private int lastClock;
        public Vector2 pos;
        public float time;
        public float alpha;
        public static ConditionalWeakTable<Creature, EmoteDisplayer> map = new();
        private List<EmoteTile> tiles = new();
        private byte localVersion;

        public EmoteDisplayer(Creature owner, OnlineCreature ownerEntity, MeadowCreatureData creatureData, MeadowCustomization.CreatureCustomization customization)
        {
            RainMeadow.Debug($"EmoteDisplayer created for {owner}");
            this.owner = owner;
            this.ownerEntity = ownerEntity;
            this.creatureData = creatureData;
            this.customization = customization;

            game = owner.abstractPhysicalObject.world.game;

            if (!Futile.atlasManager.DoesContainAtlas("emotes_common"))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/emotes_common").name);
            }
            if (!Futile.atlasManager.DoesContainAtlas(customization.EmoteAtlas))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/" + customization.EmoteAtlas).name);
            }
        }

        public void ProcessRemoteData()
        {
            if (localVersion != this.creatureData.emotesVersion)
            {
                RainMeadow.Debug("new version");
                Clear();
                localVersion = creatureData.emotesVersion;
                RainMeadow.Debug("Time since tick is: " + creatureData.emotesTick.TimeSinceTick());
                startInGameClock = (int)(game.clock - creatureData.emotesTick.TimeSinceTick() * game.framesPerSecond);
            }
            this.timeToLive = this.creatureData.emotesLife;
            foreach (var e in creatureData.emotes.Except(tiles.Select(t => t.emote)))
            {
                AddEmoteRemote(e);
            }
        }

        public void OnUpdate() // this update is called from creature.update and the individual tiles, so that stuff keeps updating even on shortcuts
        {
            if (game.clock == lastClock) return;
            lastClock = game.clock;

            this.pos = owner.firstChunk.pos;
            if((owner.inShortcut || owner.room == null) && game.shortcuts.OnScreenPositionOfInShortCutCreature(owner.abstractPhysicalObject.Room.realizedRoom, owner) is Vector2 inShortcutPos)
            {
                this.pos = inShortcutPos;
            }

            if (!ownerEntity.isMine)
            {
                ProcessRemoteData();
            }

            time = (game.clock - startInGameClock) / (float)game.framesPerSecond;
            alpha = Mathf.Min(
                Mathf.InverseLerp(0, 0.6f, time), //fade in
                Mathf.InverseLerp(timeToLive, timeToLive - 1f, time) // fade out
                );

            if(ownerEntity.isMine && tiles.Count > 0 && time > timeToLive)
            {
                Clear();
                this.creatureData.emotes.Clear();
                this.creatureData.emotesVersion++;
            }
        }

        public void ChangeRooms(WorldCoordinate newRoom)
        {
            Clear();
            var room = game.world.GetAbstractRoom(newRoom)?.realizedRoom;
            if (room == null) return;
            for (int i = 0; i < this.creatureData.emotes.Count; i++)
            {
                var tile = new EmoteTile(this.creatureData.emotes[i], i, this);
                room.AddObject(tile);
                this.tiles.Add(tile);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].Destroy();
            }
            tiles.Clear();
        }

        // maybe move this logic to the data thing?
        internal bool AddEmoteLocal(EmoteType emoteType)
        {
            RainMeadow.Debug(emoteType);
            if (!ownerEntity.isMine) throw new InvalidProgrammerException("not mine");
            if (this.creatureData.emotes.Contains(emoteType)) return false;
            if (this.creatureData.emotes.Count >= maxEmoteCount) return false;
            if (owner.abstractPhysicalObject.realizedObject == null) return false;
            if (owner.abstractPhysicalObject.Room.realizedRoom == null) return false;
            if (this.creatureData.emotes.Count > 0 && (timeToLive < this.creatureData.emotes.Count)) return false;

            if (this.creatureData.emotes.Count == 0)
            {
                startInGameClock = owner.abstractPhysicalObject.world.game.clock;
                timeToLive = initialLifetime;
                this.creatureData.emotesVersion++;
                this.creatureData.emotesTick = new TickReference(ownerEntity.primaryResource.owner);
                this.creatureData.emotesLife = timeToLive;
            }
            else
            {
                timeToLive += initialLifetime / (this.creatureData.emotes.Count + 1);
                this.creatureData.emotesLife = timeToLive;
            }

            var tile = new EmoteTile(emoteType, this.tiles.Count, this);
            owner.abstractPhysicalObject.Room.realizedRoom.AddObject(tile);
            this.creatureData.emotes.Add(emoteType);
            this.tiles.Add(tile);

            RainMeadow.Debug("Added");
            return true;
        }

        private void AddEmoteRemote(EmoteType emoteType)
        {
            RainMeadow.Debug(emoteType);
            if (ownerEntity.isMine) throw new InvalidProgrammerException("mine");
            if (tiles.Any(t => t.emote == emoteType)) return;
            if (tiles.Count >= maxEmoteCount) return;
            if (owner.abstractPhysicalObject.realizedObject == null) return;
            if (owner.abstractPhysicalObject.Room.realizedRoom == null) return;

            var tile = new EmoteTile(emoteType, this.tiles.Count, this);
            owner.abstractPhysicalObject.Room.realizedRoom.AddObject(tile);
            this.tiles.Add(tile);

            RainMeadow.Debug("Added");
        }

        public const float emoteSize = 60f;
        public const float emoteSourceSize = 240f;
        public const float gap = 5f;

        static Vector2 mainOffset = new Vector2(0, 30 + emoteSize / 2f);
        static Vector2 halfHeight = new Vector2(0, (emoteSize + gap) / 2f);
        static Vector2 halfWidth = new Vector2((emoteSize + gap) / 2f, 0);

        internal Vector2 GetPos(int index)
        {
            switch (tiles.Count)
            {
                case 0:
                case 1:
                default:
                    return pos + mainOffset;
                case 2:
                    return pos + mainOffset + (index == 1 ? halfWidth : -halfWidth);
                case 3: // could go fancy here but this runs just as fine
                case 4:
                    return pos + mainOffset + (index == 0 ? (1.414f) * (-halfWidth + halfHeight) : index == 1 ? Vector2.zero : index == 2 ? (1.414f) * (halfWidth + halfHeight) : 2.828f * halfHeight);
            }
        }
        internal class EmoteTile : UpdatableAndDeletable, IDrawable
        {
            public EmoteType emote;
            private EmoteDisplayer holder;
            private int index;

            public Vector2 pos;
            private float lastAlpha;
            private float alpha;
            public Vector2 lastPos;

            public EmoteTile(EmoteType emote, int index, EmoteDisplayer emoteHolder)
            {
                this.emote = emote;
                this.index = index;
                this.holder = emoteHolder;
                this.pos = holder.GetPos(index);
                this.alpha = holder.alpha;
                this.lastPos = this.pos;
                lastAlpha = alpha;
            }

            public override void Update(bool eu)
            {
                holder.OnUpdate();
                this.lastPos = this.pos;
                this.pos = holder.GetPos(index);
                lastAlpha = alpha;
                alpha = holder.alpha;
                if (holder.owner.abstractPhysicalObject.Room.realizedRoom != this.room) { RainMeadow.Debug("EmoteTile destroyed"); Destroy(); }
                base.Update(eu);
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[2];
                sLeaser.sprites[0] = new FSprite(holder.customization.GetBackground(emote));
                sLeaser.sprites[0].color = holder.customization.EmoteBackgroundColor(emote);
                sLeaser.sprites[1] = new FSprite(holder.customization.GetEmote(emote));
                sLeaser.sprites[1].color = holder.customization.EmoteColor(emote);

                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    sLeaser.sprites[i].scale = emoteSize / emoteSourceSize;
                }

                var container = rCam.ReturnFContainer("HUD");
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    container.AddChild(sLeaser.sprites[i]);
                }
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) { }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                Vector2 newPos = Vector2.Lerp(this.lastPos, this.pos, timeStacker) - camPos;
                var newAlpha = Mathf.Lerp(alpha, lastAlpha, timeStacker);
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    sLeaser.sprites[i].SetPosition(newPos);
                    sLeaser.sprites[i].alpha = newAlpha;
                }
                if (base.slatedForDeletetion || this.room != rCam.room)
                {
                    sLeaser.CleanSpritesAndRemove();
                }
            }
        }
    }
}
