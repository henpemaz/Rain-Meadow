using Smoke;
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
        public static bool activatedNightcat = false;
        public static bool isActive = false;

        public static float cooldownTimer = 0f;
        public static bool initiateCountdownTimer = false;

        public static bool notifiedPlayer = false;
        public static int flashEyes = 80;


        public static void ActivateNightcat(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            isActive = true;
            sLeaser.sprites[9]._color = Color.white; // eyes
            for (int i = 0; i < spriteCount; i++)
            {
                // Increment alphaOffsets for fading out
                float fadeOutAlpha = Mathf.Clamp01(Time.deltaTime * 0.9f);
                alphaOffsets[i] = Mathf.Clamp01(alphaOffsets[0] + fadeOutAlpha); // Adjust speed


                // Apply transparency using the updated value
                sLeaser.sprites[i]._color.a = Mathf.Lerp(arena.avatarSettings.bodyColor.a, 0f, alphaOffsets[i]);


            }
            if (sLeaser.sprites[0]._color.a == 0f && !activateNightcatSFX)
            {
                activateNightcatSFX = true;
                self.player.room.PlaySound(SoundID.Firecracker_Disintegrate, self.player.mainBodyChunk, loop: false, 1f, 1f);
                self.player.room.AddObject(new ZapCoil.ZapFlash(self.player.bodyChunks[1].pos, 5f));
            }



        }

        public static void DeactivateNightcat(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {

            isActive = false;
            activateNightcatSFX = false;

            if (!activateNightcatSFX)
            {   
                self.player.room.PlaySound(SoundID.Firecracker_Disintegrate, self.player.mainBodyChunk, loop: false, 1f, 1f);
                self.player.room.AddObject(new ZapCoil.ZapFlash(self.player.bodyChunks[1].pos, 5f));
                activateNightcatSFX = true;
            }
            //self.player.room.AddObject(new ShockWave(self.player.bodyChunks[1].pos, 100f, 0.07f, 6));

            // Reset alphaOffsets for the next cycle
            for (int i = 0; i < spriteCount; i++)
            {
                // Apply the alpha based on the current offset
                sLeaser.sprites[i]._color.a = arena.avatarSettings.bodyColor.a;
                alphaOffsets[i] = 0f; // Reset to start restoring from fully transparent

            }

            cooldownTimer = 300;
            durationPhase = 300;
            deactivateNightcatSFX = false;
            activatedNightcat = false;
            activateNightcatSFX = false;

        }

        public static void NotifyReadyForNightcat(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            flashEyes--;
            sLeaser.sprites[9]._color = UnityEngine.Color.white; // eyes
            notifiedPlayer = true;

        }

        public static void NightcatImplementation(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {

            if (Nightcat.activatedNightcat)
            {
                Nightcat.ActivateNightcat(arena, self, sLeaser, rCam, timeStacker, camPos);
                if (Nightcat.durationPhase > 0)
                {
                    Nightcat.durationPhase--;
                }

                Nightcat.durationPhase = Mathf.Max(Nightcat.durationPhase, 0);
            }


            if ((Nightcat.durationPhase == 0 || self.player.input[0].thrw) &&  Nightcat.isActive)
            {
                Nightcat.DeactivateNightcat(arena, self, sLeaser, rCam, timeStacker, camPos);

            }
        }

    }

}