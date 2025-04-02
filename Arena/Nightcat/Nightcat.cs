using Smoke;
using UnityEngine;

namespace RainMeadow.Arena.Nightcat
{
    internal static class Nightcat
    {
        public static int durationPhase = 600;
        public static float[] alphaOffsets = { 0.0f, 0.05f, 0.1f, 0.15f, 0.2f }; // Offsets for tail, feet, neck, chin, head. Currently unused
        public static int spriteCount = 5;

        // SFX management
        public static bool activateNightcatSFX = false;
        public static bool deactivateNightcatSFX = false;

        // 
        public static bool activatedNightcat = false;
        public static bool isActive = false;

        //public static bool isActive = false;

        public static float cooldownTimer = 0f;
        public static bool initiateCountdownTimer = false;


        // Skin / HUD management
        public static bool notifiedPlayer = true;
        public static bool isReverseLerping = false;
        public static float SwitchInterval = 0.9f;


        // HUD management
        public static bool firstTimeInitiating = false;



        // Eye management
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

        public static void ResetSneak(Player player)
        {
            player.slugcatStats.visualStealthInSneakMode = 0.1f;
        }

        public static void ImproveSneak(Player player)
        {
            player.slugcatStats.visualStealthInSneakMode = 1f;
        }


        public static void ResetThrowingSkill(Player player)
        {
            player.slugcatStats.throwingSkill = 1;
        }

        public static void ImproveThrow(Player player)
        {
            player.slugcatStats.throwingSkill = 3;
        }


        public static void ResetCoolDownTimer()
        {
            cooldownTimer = 360f;
        }

        public static void CheckInputForActivatingNightcat(Player player)
        {

            if (player.input[0].pckp && player.input[0].jmp && !Nightcat.activatedNightcat && Nightcat.cooldownTimer == 0)
            {
                Nightcat.activatedNightcat = true;
            }
            if (Nightcat.cooldownTimer > 0)
            {
                Nightcat.cooldownTimer--;
            }

            Nightcat.cooldownTimer = Mathf.Max(Nightcat.cooldownTimer, 0);


        }

        public static void ActivateNightcat(ArenaOnlineGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            isActive = self.player.isCamo;

            //sLeaser.sprites[9]._color = Color.white; // eyes
            //sLeaser.sprites[8]._color = Color.black; // arms

            //for (int i = 0; i < spriteCount; i++)
            //{
            //    // Increment alphaOffsets for fading out
            //    float fadeOutAlpha = Mathf.Clamp01(Time.deltaTime * 0.9f);
            //    alphaOffsets[i] = Mathf.Clamp01(alphaOffsets[0] + fadeOutAlpha); // Adjust speed


            //    // Apply transparency using the updated value
            //    sLeaser.sprites[i]._color.a = Mathf.Lerp(arena.avatarSettings.bodyColor.a, 0f, alphaOffsets[i]);


            //}
            if (self.player.isCamo && !activateNightcatSFX)
            {
                activateNightcatSFX = true;
                deactivateNightcatSFX = false;
                self.player.room.PlaySound(SoundID.Firecracker_Disintegrate, self.player.mainBodyChunk, loop: false, 1f, 1f);
                self.player.room.AddObject(new Watcher.RippleRing(self.player.bodyChunks[1].pos, 5, 5f, 5f));
                Nightcat.ImproveSneak(self.player);
                Nightcat.ImproveThrow(self.player);
            }



        }

        public static void DeactivateNightcat(ArenaOnlineGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            isActive = self.player.isCamo;
            activateNightcatSFX = false;
            //self.player.room.AddObject(new ShockWave(self.player.bodyChunks[1].pos, 100f, 0.07f, 6));

            // Reset alphaOffsets for the next cycle
            //for (int i = 0; i < spriteCount; i++)
            //{
            //    // Apply the alpha based on the current offset
            //    sLeaser.sprites[i]._color.a = arena.avatarSettings.bodyColor.a;
            //    alphaOffsets[i] = 0f; // Reset to start restoring from fully transparent

            //}

            if (!activateNightcatSFX && !isActive)
            {
                self.player.room.PlaySound(SoundID.Firecracker_Disintegrate, self.player.mainBodyChunk, loop: false, 1f, 1f);
                self.player.room.AddObject(new Watcher.RippleRing(self.player.bodyChunks[1].pos, 5, 5f, 5f));
                activateNightcatSFX = true;
                self.player.isCamo = false;
            }

            ResetNightcat();
            ResetCoolDownTimer();
            ResetSneak(self.player);
            ResetThrowingSkill(self.player);
            firstTimeInitiating = false;
        }

        public static void NightcatImplementation(ArenaOnlineGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

            MakeEyesFlash(arena, self, sLeaser, rCam, timeStacker, camPos);

            if (self.player.isCamo)
            {
                ActivateNightcat(arena, self, sLeaser, rCam, timeStacker, camPos);
                //if (durationPhase > 0)
                //{
                //    durationPhase--;
                //}

                //durationPhase = Mathf.Max(durationPhase, 0);
            }


            if (self.player != null && (self.player.inCamoTime >= self.player.camoLimit || self.player.dead || self.player.dangerGrasp != null && self.player.dangerGraspTime > 20) && self.player.isCamo)
            {
                DeactivateNightcat(arena, self, sLeaser, rCam, timeStacker, camPos);

            }
            
        }

        public static void MakeEyesFlash(ArenaOnlineGameMode arena, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {


            flashTimer += Time.deltaTime;
            float t = flashTimer / flashDuration;
            float intensity = Mathf.PingPong(t, Nightcat.maxFlashIntensity);
            sLeaser.sprites[9]._color = Color.Lerp(Color.red, arena.avatarSettings.eyeColor, intensity);


        }

    }

}