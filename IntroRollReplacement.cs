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
            self.rainEffect.rainFade    = RWCustom.Custom.SCurve(Mathf.InverseLerp(0f, 7f, self.time), 0.8f);
            self.illustrations[0].alpha = RWCustom.Custom.SCurve(Mathf.InverseLerp(1f, 2f, self.time), 0.65f);
            self.illustrations[1].alpha = RWCustom.Custom.SCurve(Mathf.InverseLerp(1.3f, 2.3f, self.time), 0.65f);
            self.illustrations[2].alpha = RWCustom.Custom.SCurve(Mathf.InverseLerp(1.5f, 2.7f, self.time), 0.65f);

            //if you want it to movement ?
            //self.illustrations[0].pos.y = (1 - RWCustom.Custom.SCurve(Mathf.InverseLerp(1f, 2f, self.time), 0.65f)    ) * -10f;
            //self.illustrations[1].pos.y = (1 - RWCustom.Custom.SCurve(Mathf.InverseLerp(1.3f, 2.3f, self.time), 0.65f)) * -10f;
            //self.illustrations[2].pos.y = (1 - RWCustom.Custom.SCurve(Mathf.InverseLerp(1.5f, 2.7f, self.time), 0.65f)) * -10f;

            float TitleAlpha = RWCustom.Custom.SCurve(Mathf.InverseLerp(3f, 4f, self.delayedTime), 0.65f);
            self.illustrations[3].alpha = TitleAlpha;
            self.illustrations[4].alpha = TitleAlpha;
            self.illustrations[5].alpha = TitleAlpha;

            //self.illustrations[3].pos.y = -(1-TitleAlpha)*10f;
            //self.illustrations[4].pos.y = -(1-TitleAlpha)*10f;
            //self.illustrations[5].pos.y = -(1-TitleAlpha)*10f;

            for (int i = 0; i < self.illustrations.Length; i++)
            {
                self.illustrations[i].sprite.isVisible = (self.illustrations[i].alpha > 0f);
            }

            if (Time.time % 2f > 1f)
            {
                self.illustrations[4].sprite.isVisible = true;
                self.illustrations[5].sprite.isVisible = false;
            }
            else
            {
                self.illustrations[4].sprite.isVisible = false;
                self.illustrations[5].sprite.isVisible = true;
            }

            bool flag = true;
            bool flag2 = true;
            if (self.anyButtonLabel != null)
            {
                self.anyButtonLabel.label.alpha = RWCustom.Custom.SCurve(Mathf.InverseLerp(14f, 15f, self.delayedTime), 0.65f);
            }
            else if (Platform.initialized && flag)
            {
                if (flag2)
                {
                    self.anyButtonLabel = new MenuLabel(self, self.pages[0], self.manager.dialog.Translate("Press any button to continue"), new Vector2(self.manager.rainWorld.screenSize.x / 2f - 50f, 40f), new Vector2(100f, 30f), false, null);
                }
                else
                {
                    InGameTranslator.LanguageID language = self.manager.rainWorld.options.language;
                    self.manager.rainWorld.options.language = InGameTranslator.systemLanguage;
                    if (language != self.manager.rainWorld.options.language)
                    {
                        self.languageFontDirty = true;
                        self.dirtyLanguage = InGameTranslator.systemLanguage;
                        InGameTranslator.UnloadFonts(language);
                        InGameTranslator.LoadFonts(self.manager.rainWorld.options.language, self);
                    }
                    self.anyButtonLabel = new MenuLabel(self, self.pages[0], self.manager.dialog.Translate("Please log in to STOVE Client with the account that has purchased the game"), new Vector2(self.manager.rainWorld.screenSize.x / 2f - 50f, 40f), new Vector2(100f, 30f), false, null);
                    self.manager.rainWorld.options.language = language;
                }
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
            if (self.CheckTimeStamp(lastTime2, self.delayedTime, 3f) && self.manager.musicPlayer != null && self.manager.musicPlayer.song != null && self.manager.musicPlayer.song is Music.IntroRollMusic)
            {
                (self.manager.musicPlayer.song as Music.IntroRollMusic).StartMusic();
            }
            if (!self.continueToMenu && self.delayedTime > 120f)
            {
                self.GoToMenu();
            }
        }
        private static void IntroRoll_ctor(On.Menu.IntroRoll.orig_ctor orig, IntroRoll self, ProcessManager manager)
        {
            RainMeadow.Debug("ohio");
            orig.Invoke(self, manager);
            self.pages[0].subObjects.DoIf(c => c is MenuIllustration, c => { c.RemoveSprites(); });
            self.pages[0].subObjects.RemoveAll(c => c is MenuIllustration);
            string[] illustrationsnames = new string[6] { "intro_roll_adultswim", "intro_roll_akupara", "intro_roll_videocult", "intro_roll_3_andlizard", "intro_roll_3_frameone", "intro_roll_3_frametwo" };
            self.illustrations = new MenuIllustration[6];
            for (int i = 0; i < illustrationsnames.Length; i++) { self.illustrations[i] = new MenuIllustration(self, self.pages[0], "", illustrationsnames[i], new Vector2(0f, 0f), true, false); }
            for (int i = 0; i < self.illustrations.Length; i++)
            {
                self.pages[0].subObjects.Add(self.illustrations[i]);
                self.illustrations[i].sprite.isVisible = false;
            }
        }
    }
}
