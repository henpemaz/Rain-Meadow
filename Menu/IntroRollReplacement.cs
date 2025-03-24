using HarmonyLib;
using Kittehface.Framework20;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public partial class IntroRollReplacement
    {
        public static void OnEnable()
        {
            On.Menu.IntroRoll.ctor += IntroRoll_ctor;
            On.Menu.IntroRoll.RawUpdate += IntroRoll_RawUpdate;
        }
        private static void IntroRoll_RawUpdate(On.Menu.IntroRoll.orig_RawUpdate orig, IntroRoll self, float dt)
        {
            if (RainMeadow.rainMeadowOptions.PickedIntroRoll.Value != RainMeadowOptions.IntroRoll.Meadow) {
                orig.Invoke(self, dt);
                return;
            }
            self.myTimeStacker += dt * (float)self.framesPerSecond;
            int num = 0;
            while (self.myTimeStacker > 1f)
            {
                self.Update();
                self.myTimeStacker -= 1f;
                num++;
                if (num > 2)
                {
                    self.myTimeStacker = 0f;
                }
                if (self.myTimeStacker > 1f)
                {
                    self.manager.rainWorld.rewiredInputManager.SendMessage("Update");
                }
            }
            self.GrafUpdate(self.myTimeStacker);

            float lastTime = self.time;
            self.time += dt;
            float lastTime2 = self.delayedTime;
            if (self.delayedTime < 4f || ((self.manager.musicPlayer == null || self.manager.musicPlayer.assetBundlesLoaded) && self.manager.soundLoader.assetBundlesLoaded && self.manager.rainWorld.platformInitialized && self.manager.rainWorld.OptionsReady && self.manager.rainWorld.progression.progressionLoaded))
            {
                self.delayedTime += dt;
            }
            self.rainEffect.rainFade = RWCustom.Custom.SCurve(Mathf.InverseLerp(0f, 5f, self.time), 0.8f);
            void Appearance(int i, float time, float appearAt, float noiseAmp = 0f, float frequency = 0f)
            {
                self.illustrations[i].alpha = RWCustom.Custom.SCurve(Mathf.InverseLerp(appearAt, appearAt+1f, time), 0.65f);
                self.illustrations[i].pos.y = (1 - RWCustom.Custom.SCurve(Mathf.InverseLerp(appearAt, appearAt+1f, time+0.4f), 0.65f)) * - 40f;
                if (noiseAmp != 0f)
                {
                    float noisetime = time * frequency;
                    Vector2 dissplacement = new(Mathf.PerlinNoise(noisetime + i, noisetime * 0.5f), Mathf.PerlinNoise(noisetime + i + 10, noisetime * 0.5f));
                    self.illustrations[i].pos = self.illustrations[i].pos * Vector2.up + dissplacement * noiseAmp;
                }
            }
            Appearance(0, self.time, 1f);
            Appearance(1, self.time, 1.5f);
            Appearance(2, self.time, 1.65f);
            self.illustrations[3].alpha = RWCustom.Custom.SCurve(Mathf.InverseLerp(3.2f, 3.8f, self.delayedTime), 0.65f);
            Appearance(4, self.delayedTime, 3.4f,  6f, 0.9f);
            Appearance(5, self.delayedTime, 3.55f, 6f, 1.2f);
            Appearance(6, self.delayedTime, 3.9f,  5f, 2.5f);

            for (int i = 0; i < self.illustrations.Length; i++)
            {
                self.illustrations[i].sprite.isVisible = (self.illustrations[i].alpha > 0f);
            }

            if (self.anyButtonLabel != null)
            {
                self.anyButtonLabel.label.alpha = RWCustom.Custom.SCurve(Mathf.InverseLerp(14f, 15f, self.delayedTime), 0.65f);
            }
            else if (Platform.initialized)
            {
                self.anyButtonLabel = new MenuLabel(self, self.pages[0], self.manager.dialog.Translate("Press any button to continue"), new Vector2(self.manager.rainWorld.screenSize.x / 2f - 50f, 40f), new Vector2(100f, 30f), false, null);
                self.pages[0].subObjects.Add(self.anyButtonLabel);
                MenuLabel menuLabel = self.anyButtonLabel;
                menuLabel.pos.x += Menu.Menu.HorizontalMoveToGetCentered(self.manager);
                self.anyButtonLabel.label.alpha = 0f;
            }

            if (self.CheckTimeStamp(lastTime, self.time, 1.5f)) { self.rainEffect.LightningSpike(0.4f, 30f); }
            if (self.CheckTimeStamp(lastTime, self.time, 3f))   { self.rainEffect.LightningSpike(0.6f, 40f); }
            if (self.CheckTimeStamp(lastTime, self.time, 5f))   { self.rainEffect.LightningSpike(0.3f, 1600f); }
            if (self.CheckTimeStamp(lastTime2, self.delayedTime, 10.3f))
            {
                self.manager.menuMic.PlaySound(SoundID.Thunder_Close, 0f, 0.3f, 1f);
                self.manager.menuMic.PlaySound(SoundID.Thunder, 0f, 0.4f, 1f);
            }
            if (self.CheckTimeStamp(lastTime2, self.delayedTime, 10.5f)) { self.rainEffect.LightningSpike(1f, 40f); }
            if (self.CheckTimeStamp(lastTime2, self.delayedTime, 2.8f) && self.manager.musicPlayer != null && self.manager.musicPlayer.song != null && self.manager.musicPlayer.song is Music.IntroRollMusic music)
            {
                music.StartMusic();
            }
            if (!self.continueToMenu && self.delayedTime > 120f)
            {
                self.GoToMenu();
            }
        }
        private static void IntroRoll_ctor(On.Menu.IntroRoll.orig_ctor orig, IntroRoll self, ProcessManager manager)
        {
            orig.Invoke(self, manager);
            if (RainMeadow.rainMeadowOptions.PickedIntroRoll.Value == RainMeadowOptions.IntroRoll.Meadow)
            {
                self.pages[0].subObjects.DoIf(c => c is MenuIllustration, c => { c.RemoveSprites(); });
                self.pages[0].subObjects.RemoveAll(c => c is MenuIllustration);
                string[] illustrationsnames = new string[7] { "adultswim", "akupara", "videocult", "titleandground", "nootmama", "nootbaby", "squidcada" };
                self.illustrations = new MenuIllustration[7];
                for (int i = 0; i < illustrationsnames.Length; i++) { self.illustrations[i] = new MenuIllustration(self, self.pages[0], "illustrations/rainmeadow introroll", illustrationsnames[i], new Vector2(0f, 0f), true, false); }
                for (int i = 0; i < self.illustrations.Length; i++)
                {
                    self.pages[0].subObjects.Add(self.illustrations[i]);
                    self.illustrations[i].sprite.isVisible = false;
                }
            }
            else if (manager.rainWorld.dlcVersion != 0 && RainMeadow.rainMeadowOptions.PickedIntroRoll.Value == RainMeadowOptions.IntroRoll.Downpour)
            {
                self.illustrations[2].RemoveSprites();
                self.pages[0].subObjects.Remove(self.illustrations[2]);
                string[] array = new string[] { "gourmand", "rivulet", "spear", "artificer", "saint" };
                self.illustrations[2] = new MenuIllustration(self, self.pages[0], "", "Intro_Roll_C_" + array[Random.Range(0, array.Length)], new Vector2(0f, 0f), true, false);
                self.pages[0].subObjects.Add(self.illustrations[2]);
                self.illustrations[2].sprite.isVisible = false;
            }
            else //(RainMeadow.rainMeadowOptions.PickedIntroRoll.Value == RainMeadowOptions.IntroRoll.Vanilla)
            {
                self.illustrations[2].RemoveSprites();
                self.pages[0].subObjects.Remove(self.illustrations[2]);
                self.illustrations[2] = new MenuIllustration(self, self.pages[0], "", "Intro_Roll_C", new Vector2(0f, 0f), true, false);
                self.pages[0].subObjects.Add(self.illustrations[2]);
                self.illustrations[2].sprite.isVisible = false;
            }

            if (ModManager.MSC && manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = SlugcatStats.Name.White;
                var popupDialog = new DialogBoxNotify(self, self.pages[0], "Rain Meadow: Please use an external mod to access Inv's campaign.", "HIDE_DIALOG", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
                self.pages[0].subObjects.Add(popupDialog);
                self.manager.menuMic?.PlaySound(SoundID.Thunder, 0f, 0.7f, 1f);
                if (manager.musicPlayer?.song is Music.Song song && song is Music.IntroRollMusic) song.FadeOut(2f); 
            }
        }
    }
}
