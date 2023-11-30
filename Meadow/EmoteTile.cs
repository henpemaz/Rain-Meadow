using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static RainMeadow.MeadowCustomization;

namespace RainMeadow
{
    public class EmoteType : ExtEnum<EmoteType>
    {
        public EmoteType(string value, bool register = false) : base(value, register) { }

        public static EmoteType emoteHappy = new EmoteType("emoteHappy", true);
        public static EmoteType emoteSad = new EmoteType("emoteSad", true);
        public static EmoteType emoteAngry = new EmoteType("emoteAngry", true);
        public static EmoteType emoteConfused = new EmoteType("emoteConfused", true);
        public static EmoteType emoteAmazed = new EmoteType("emoteAmazed", true);
        public static EmoteType emoteDead = new EmoteType("emoteDead", true);
        public static EmoteType emoteGoofy = new EmoteType("emoteGoofy", true);
        public static EmoteType emoteMischievous = new EmoteType("emoteMischievous", true);
        public static EmoteType emoteHello = new EmoteType("emoteHello", true);
        public static EmoteType emoteWink = new EmoteType("emoteWink", true);
        public static EmoteType emoteHugging = new EmoteType("emoteHugging", true);
        public static EmoteType emoteShrug = new EmoteType("emoteShrug", true);

        public static EmoteType symbolYes = new EmoteType("symbolYes", true);
        public static EmoteType symbolNo = new EmoteType("symbolNo", true);
        public static EmoteType symbolQuestion = new EmoteType("symbolQuestion", true);
        // todo
    }

    class EmoteHandler
    {
        private InputScheme currentInputScheme;

        static KeyCode[] alphaRow = new[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals };

        static EmoteType[][] keyboardMappingRows = new[]{
            new EmoteType[12]{
                EmoteType.emoteHappy,
                EmoteType.emoteSad,
                EmoteType.emoteAngry,
                EmoteType.emoteConfused,
                EmoteType.emoteAmazed,
                EmoteType.emoteDead,
                EmoteType.emoteGoofy,
                EmoteType.emoteMischievous,
                EmoteType.emoteHello,
                EmoteType.emoteWink,
                EmoteType.emoteHugging,
                EmoteType.emoteShrug,
            },new EmoteType[12]{
                EmoteType.symbolYes,
                EmoteType.symbolNo,
                EmoteType.symbolQuestion,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
            },new EmoteType[12]{
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
                EmoteType.symbolNo,
            }
        };
        private int currentKeyboardRow;

        enum InputScheme
        {
            none,
            keyboard,
            mouse,
            controller
        }

        public void UnityUpdate()
        {
            if(currentInputScheme == InputScheme.keyboard)
            {
                for (int i = 0; i < alphaRow.Length; i++)
                {
                    if (Input.GetKeyDown(alphaRow[i]))
                    {
                        EmotePressed(keyboardMappingRows[currentKeyboardRow][i]);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    currentKeyboardRow = (currentKeyboardRow + 1) % 3;
                }
            }
        }

        MeadowGameMode gameMode;
        EmoteDisplayer mainHolder => EmoteDisplayer.map.GetValue(gameMode.avatar.realizedCreature, null);

        public EmoteHandler(MeadowGameMode gameMode)
        {
            this.gameMode = gameMode;

            todo wire me in
        }

        private void EmotePressed(EmoteType emoteType)
        {
            RainMeadow.Debug(emoteType);
            if (gameMode?.avatar == null) return;
            if(mainHolder.AddEmoteLocal(emoteType))
            {
                // todo play local input sound
            }
        }
    }

    public class EmoteDisplayer
    {
        public Creature owner;
        public MeadowCustomization.CreatureCustomization customization;
        public List<EmoteType> emotes = new();

        private int maxEmoteCount = 4;
        private float initialLifetime = 4; // seconds

        public int startInGameClock;
        public float timeToLive;

        public EmoteDisplayer(Creature owner, MeadowCreatureData creatureData, MeadowCustomization.CreatureCustomization customization)
        {
            this.owner = owner;
            this.customization = customization ?? throw new System.ArgumentNullException(nameof(customization));
        }

        public void OnUpdate()
        {
            this.pos = owner.firstChunk.pos;

            var game = owner.abstractPhysicalObject.world.game;
            time = (game.clock - startInGameClock) / (float)game.framesPerSecond;
            alpha = Mathf.Min(
                Mathf.InverseLerp(0, 0.6f, time), //fade in
                Mathf.InverseLerp(timeToLive, timeToLive - 1f, time) // fade out
                );
        }

        // maybe move this logic to the data thing?
        internal bool AddEmoteLocal(EmoteType emoteType)
        {
            RainMeadow.Debug(emoteType);
            if (emotes.Contains(emoteType)) return false;
            // todo past half life, but more than .25 left
            if (emotes.Count >= maxEmoteCount) return false;
            if (owner.abstractPhysicalObject.realizedObject == null) return false;
            if (owner.abstractPhysicalObject.Room.realizedRoom == null) return false;

            if (emotes.Count == 0)
            {
                startInGameClock = owner.abstractPhysicalObject.world.game.clock;
                timeToLive = initialLifetime;
            }
            else
            {
                timeToLive += initialLifetime / (emotes.Count + 1);
            }
            emotes.Add(emoteType);

            // todo display
            owner.abstractPhysicalObject.Room.realizedRoom.AddObject(new EmoteTile(emoteType, emotes.Count -1, this));

            RainMeadow.Debug("Added");
            return true;
        }

        static Vector2 mainOffset = new Vector2(0, 60);
        static Vector2 halfHeight = new Vector2(0, 30);
        static Vector2 width = new Vector2(60, 0);

        public Vector2 pos;
        public float time;
        public float alpha;
        public static ConditionalWeakTable<Creature, EmoteDisplayer> map = new();

        internal Vector2 GetPos(int index)
        {
            switch (emotes.Count)
            {
                case 0:
                case 1:
                default:
                    return pos + mainOffset;
                case 2:
                    return pos + mainOffset + (index == 1 ? width : -width);
                case 3: // could go fancy here but this runs just as fine
                    return pos + mainOffset + (index == 0 ? -width + halfHeight : index == 1 ? -halfHeight : -width + halfHeight);
                case 4:
                    return pos + mainOffset + (index == 0 ? -width + halfHeight : index == 1 ? -halfHeight : index == 2 ? -width + halfHeight : halfHeight);
            }
        }
    }

    internal class EmoteTile : UpdatableAndDeletable, IDrawable
    {
        private EmoteType emote;
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
            this.lastPos = this.pos;
            this.pos = holder.GetPos(index);
            lastAlpha = alpha;
            alpha = holder.alpha;
            if(holder.owner.room != this.room) { Destroy(); }
            base.Update(eu);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("emotes/background_tile");
            sLeaser.sprites[0].color = holder.customization.EmoteTileColor();
            sLeaser.sprites[1] = new FSprite(holder.customization.GetEmote(emote));

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
