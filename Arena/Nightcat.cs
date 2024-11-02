using UnityEngine;

namespace RainMeadow
{
    internal static class Nightcat
    {


        public static int ticker = 300;
        public static int durationPhase = 300;
        public static float[] alphaOffsets = { 0.0f, 0.05f, 0.1f, 0.15f, 0.2f }; // Offsets for tail, feet, neck, chin, head
        public static int spriteCount = 5;
        public static bool activateNightcatSFX = false;
        public static bool deactivateNightcatSFX = false;
        public static bool activateNightcat = false;
        public static float cooldownTimer = 0f;
        public static bool initiateCountdownTimer = false;


        public static void ActivateNightcat(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {

            if (ticker > 0)
            {
                ticker--;

                for (int i = 0; i < spriteCount; i++)
                {
                    // Increment alphaOffsets for fading out
                    float fadeOutAlpha = Mathf.Clamp01(Time.deltaTime * 0.3f);
                    alphaOffsets[i] = Mathf.Clamp01(alphaOffsets[0] + fadeOutAlpha); // Adjust speed


                    // Apply transparency using the updated value
                    sLeaser.sprites[i]._color.a = Mathf.Lerp(arena.avatarSettings.bodyColor.a, 0f, alphaOffsets[i]);
                }
                if (sLeaser.sprites[0]._color.a == 0f && !activateNightcatSFX)
                {
                    activateNightcatSFX = true;
                    self.player.room.PlaySound(SoundID.Firecracker_Disintegrate, self.player.mainBodyChunk, loop: false, 1f, 1f);
                    self.player.room.AddObject(new ShockWave(self.player.bodyChunks[1].pos, 100f, 0.07f, 6));

                }
            }
            else if (ticker <= 0) // Restore phase
            {
                activateNightcatSFX = false;
                durationPhase--;

                if (durationPhase <= 0 && !deactivateNightcatSFX)
                {
                    deactivateNightcatSFX = true;
                    self.player.room.PlaySound(SoundID.Firecracker_Disintegrate, self.player.mainBodyChunk, loop: false, 1f, 1f);
                    self.player.room.AddObject(new ShockWave(self.player.bodyChunks[1].pos, 100f, 0.07f, 6));

                    // Reset alphaOffsets for the next cycle
                    for (int i = 0; i < spriteCount; i++)
                    {
                        // Apply the alpha based on the current offset
                        sLeaser.sprites[i]._color.a = arena.avatarSettings.bodyColor.a;
                        alphaOffsets[i] = 0f; // Reset to start restoring from fully transparent

                    }

                    ticker = 300;
                    cooldownTimer = 300;
                    durationPhase = 300;
                    deactivateNightcatSFX = false;
                    activateNightcat = false;
                    initiateCountdownTimer = true;

                }

            }

        }
    }

}