using Smoke;
using UnityEngine;

namespace RainMeadow.Arena.Nightcat
{
    internal static class Nightcat
    {
        public static int ticker = 300;
        public static int durationPhase = 360;
        public static float[] alphaOffsets = { 0.0f, 0.05f, 0.1f, 0.15f, 0.2f }; // Offsets for tail, feet, neck, chin, head
        public static int spriteCount = 5;
        public static bool activateNightcatSFX = false;
        public static bool deactivateNightcatSFX = false;
        public static bool activatedNightcat = false;
        public static bool isActive = false;

        public static float cooldownTimer = 0f;
        public static bool initiateCountdownTimer = false;

        public static bool notifiedPlayer = true;
        public static bool firstTimeInitiating = false;
        public static bool isReverseLerping = false;
        public static float SwitchInterval = 0.9f;


        public static float flashTimer;
        public static float flashDuration = 0.9f; // Duration of each flash
        public static float maxFlashIntensity = 1f; // Maximum intensity of the flash
        public static Color eyeColor;
        public static void ResetNightcat()
        {
            durationPhase = 360;
            deactivateNightcatSFX = false;
            activatedNightcat = false;
            activateNightcatSFX = false;
            firstTimeInitiating = false;
            notifiedPlayer = false;

        }

        public static void ResetCoolDownTimer()
        {
            cooldownTimer = 360f;
        }

        public static void ActivateNightcat(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            isActive = true;

            sLeaser.sprites[9]._color = Color.white; // eyes
            sLeaser.sprites[8]._color = Color.black; // arms

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

        public static void DeactivateNightcat(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            isActive = false;
            activateNightcatSFX = false;
            //self.player.room.AddObject(new ShockWave(self.player.bodyChunks[1].pos, 100f, 0.07f, 6));

            // Reset alphaOffsets for the next cycle
            for (int i = 0; i < spriteCount; i++)
            {
                // Apply the alpha based on the current offset
                sLeaser.sprites[i]._color.a = arena.avatarSettings.bodyColor.a;
                alphaOffsets[i] = 0f; // Reset to start restoring from fully transparent

            }

            if (!activateNightcatSFX)
            {
                self.player.room.PlaySound(SoundID.Firecracker_Disintegrate, self.player.mainBodyChunk, loop: false, 1f, 1f);
                self.player.room.AddObject(new ZapCoil.ZapFlash(self.player.bodyChunks[1].pos, 5f));
                activateNightcatSFX = true;
            }

            ResetNightcat();
            ResetCoolDownTimer();
            firstTimeInitiating = false;
            self.player.slugcatStats.visualStealthInSneakMode = 0.5f;

        }

        public static void NightcatImplementation(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            MakeEyesFlash(arena, self, sLeaser, rCam, timeStacker, camPos);

            if (activatedNightcat)
            {
                ActivateNightcat(arena, self, sLeaser, rCam, timeStacker, camPos);
                if (durationPhase > 0)
                {
                    durationPhase--;
                }

                durationPhase = Mathf.Max(durationPhase, 0);
            }


            if ((durationPhase == 0 || self.player.input[0].thrw || self.player != null && (self.player.dead || self.player.dangerGrasp != null && self.player.dangerGraspTime > 20)) && isActive)
            {
                DeactivateNightcat(arena, self, sLeaser, rCam, timeStacker, camPos);

            }
        }

        public static void MakeEyesFlash(ArenaCompetitiveGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!Nightcat.isActive && Nightcat.cooldownTimer == 0 && !arena.countdownInitiatedHoldFire)
            {

                flashTimer += Time.deltaTime;
                float t = flashTimer / flashDuration;
                float intensity = Mathf.PingPong(t, Nightcat.maxFlashIntensity);
                sLeaser.sprites[9]._color = Color.Lerp(Color.white, arena.avatarSettings.eyeColor, intensity);

            }
        }

    }

}