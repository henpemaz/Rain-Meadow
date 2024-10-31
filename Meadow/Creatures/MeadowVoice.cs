using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowVoice
    {
        CreatureController owner;

        public MeadowVoice(CreatureController owner)
        {
            this.owner = owner;
            volume = 1f;
            myId = owner.customization.VoiceId;
        }

        SoundID myId;
        float volume;
        float spam;
        float silence;

        public bool Display => spam < 15f;

        public float Volume => volume;

        internal void Call()
        {
            RainMeadow.Debug(owner.onlineCreature);
            spam = Mathf.Min(20, spam + 1f);
            volume = 1f - 0.75f * RWCustom.Custom.SCurve(Mathf.InverseLerp(3, 15, spam), 0.25f);
            var room = owner.creature.room;
            if (spam < 15f)
            {
                room.PlaySound(myId, owner.creature.firstChunk, false, volume, 1.0f);

                foreach (RoomCamera cam in room.game.cameras)
                {
                    if (cam.room != null && cam.room.shortCutsReady)
                    {
                        int neighborIndex = cam.room.abstractRoom.ExitIndex(room.abstractRoom.index);
                        if (neighborIndex >= 0)
                        {
                            IntVector2 startTile = cam.room.ShortcutLeadingToNode(neighborIndex).StartTile;
                            var shortcut = cam.room.shortcutsIndex.IndexfOf(startTile);
                            cam.room.BlinkShortCut(shortcut, -1, 1f);
                            cam.shortcutGraphics.ColorEntrance(shortcut, owner.effectColor);
                            if (UnityEngine.Random.value < volume)
                            {
                                cam.room.PlaySound(myId, cam.room.MiddleOfTile(startTile), volume / 2f, 1.0f);
                            }
                        }
                    }
                }
            }
            silence = 0;
        }

        internal void Update()
        {
            silence = Mathf.Min(4f, silence + 1f / 40f);
            spam = RWCustom.Custom.LerpAndTick(spam, 0, (1f + silence) / 400f, (1f + silence) / 40f);
        }
    }
}
