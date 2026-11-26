using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System.IO;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCredits : SmartMenu
    {
        public float time, scrollSpeed, scrollInputSupression;
        public bool anyButton, lastAnyButton;
        public RainEffect rainEffect;
        public CreditsStage currentStage;
        public MeadowCreditsObject currentCreditsObject;

        public override MenuScene.SceneID GetScene => MenuScene.SceneID.Intro_3_In_Tree;
        public MeadowCredits(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.MeadowCredits)
        {
            RainMeadow.DebugMe();
            rainEffect = new(this, pages[0]);
            pages[0].subObjects.Add(rainEffect);

            backTarget = RainMeadow.Ext_ProcessID.LobbySelectMenu;
            mySoundLoopID = SoundID.MENU_End_Credits_LOOP;
        }

        public void SpawnCreditsObject()
        {
            if (currentStage != CreditsStage.Wait)
            {
                currentCreditsObject = new(this, pages[0], currentStage);
                pages[0].subObjects.Add(currentCreditsObject);
            }
        }

        public void DisposeCreditsObject()
        {
            if (currentCreditsObject == null) return;
            currentCreditsObject.RemoveSprites();
            pages[0].subObjects.Remove(currentCreditsObject);
            currentCreditsObject = null;
        }

        public override void Update()
        {
            base.Update();
            anyButton = false;
            if (currentCreditsObject != null)
            {
                if (input.y > 0)
                {
                    scrollSpeed = Custom.LerpAndTick(scrollSpeed, Mathf.Lerp(-4f, currentCreditsObject.CurrentDefaultScrollSpeed, scrollInputSupression), 0.12f, 0.09090909f);
                }
                else if (input.y < 0)
                {
                    scrollSpeed = Custom.LerpAndTick(scrollSpeed, Mathf.Lerp(16f, currentCreditsObject.CurrentDefaultScrollSpeed, scrollInputSupression), 0.12f, 0.09090909f);
                }
                else
                {
                    scrollSpeed = Custom.LerpAndTick(scrollSpeed, anyButton ? 0f : currentCreditsObject.CurrentDefaultScrollSpeed, 0.12f, 0.09090909f);
                }

                if (currentCreditsObject.OutOfScreen) NextStage();
                else if (currentCreditsObject.BeforeScreen && scrollSpeed < 0f) PreviousStage();
            }
            else
            {
                NextStage();
            }
            lastAnyButton = anyButton;
            if (time > 14f && Random.value < 0.00625f && rainEffect != null)
            {
                rainEffect.LightningSpike(Mathf.Pow(Random.value, 2f) * 0.85f, Mathf.Lerp(20f, 120f, Random.value));
            }
        }

        public void NextStage()
        {
            if (currentStage == CreditsStage.End)
            {
                ExitCredits();
                return;
            }
            DisposeCreditsObject();
            currentStage++;
            SpawnCreditsObject();
        }

        public void PreviousStage()
        {
            if (currentStage == CreditsStage.End || currentStage == CreditsStage.Contributors)
            {
                return;
            }
            DisposeCreditsObject();
            currentStage--;
            SpawnCreditsObject();
        }

        public void ExitCredits()
        {
            manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
        }

        public override void RawUpdate(float dt)
        {
            base.RawUpdate(dt);
            time += dt;
            rainEffect?.rainFade = Custom.SCurve(Mathf.InverseLerp(0f, 6f, time), 0.8f) * 0.5f;
        }

        public class MeadowCreditsObject : PositionedMenuObject
        {
            public int age;
            public float scroll, lastScroll;
            public float defaultScroll = 4f;
            public float slowScroll = 1f;
            public float slowZone = 30f;

            public bool BeforeScreen => scroll <= -100f;

            public bool OutOfScreen
            {
                get
                {
                    foreach(var obj in subObjects)
                    {
                        if (obj is not RectangularMenuObject menuObject) continue;

                        float lowestPoint = menuObject.DrawY(1f) - ((menuObject is MenuIllustration) ? menuObject.size.y : 0);
                        if (lowestPoint < 800f) return false;
                    }
                    return true;
                }
            }

            public float CurrentDefaultScrollSpeed => Custom.LerpMap(Mathf.Abs(500f - scroll), slowZone, slowZone + 100f, slowScroll, defaultScroll, 0.6f);

            public CreditsStage stage;
            public MeadowCreditsObject(Menu.Menu menu, MenuObject owner, CreditsStage stage) : base(menu, owner, default)
            {
                this.stage = stage;
                string creditsFile = "";
                switch(stage)
                {
                    case CreditsStage.RM_Logo:
                        MenuIllustration logo = new(menu, this, "illustrations/rainmeadowtitle", "title_credits", new(683f, 384f), true, true)
                        {
                            lastAlpha = 0f,
                            alpha = 0f,
                        };
                        subObjects.Add(logo);

                        scroll = 0;
                        defaultScroll = 4f;
                        slowScroll = 4f;
                        break;
                    case CreditsStage.Contributors:
                        creditsFile = "01 - MEADOW CONTRIBUTORS";

                        defaultScroll = 1.75f;
                        slowScroll = 1f;
                        break;
                    case CreditsStage.Playtesters:
                        creditsFile = "02 - MEADOW PLAY TESTERS";

                        defaultScroll = 2.5f;
                        slowScroll = 1.5f;
                        break;
                }

                if (creditsFile != null)
                {
                    string path = AssetManager.ResolveFilePath("Text" + Path.DirectorySeparatorChar.ToString() + "Credits" + Path.DirectorySeparatorChar.ToString() + creditsFile + ".txt");

                    string[] text;
                    text = File.Exists(path) ? File.ReadAllLines(path) : [];
                    float illustrationOffset = 0;
                    for (int i = 0; i < text.Length; i++)
                    {
                        string line = text[i];
                        line.Replace("<LINE>", "\r\n");
                        if (line.StartsWith("<I>"))
                        {
                            var illustration = new MenuIllustration(menu, this, "illustrations/rainmeadowcredits", line.Substring(3), new(683f, 0f), true, true);
                            illustrationOffset += illustration.texture.height / 2;
                            illustration.pos.y = -i * 40 - illustrationOffset;
                            subObjects.Add(illustration);
                        }
                        else
                        {
                            if (line.StartsWith("<T>"))
                            {
                                line = menu.Translate(line.Substring(3));
                            }
                            var label = new MenuLabel(menu, this, line, new(433f, -i * 40f - illustrationOffset), new(500f, 30f), false);
                            label.label.alignment = FLabelAlignment.Center;
                            label.label.x = -1000f;
                            subObjects.Add(label);
                        }
                    }
                }

                scroll = -100f;
                pos.y = scroll;
                lastPos.y = pos.y;
            }

            public override void Update()
            {
                base.Update();

                lastScroll = scroll;
                scroll += (menu as MeadowCredits).scrollSpeed;
                scroll = Mathf.Max(scroll, -1000f);
                age++;

                pos.y = scroll;

                if (stage == CreditsStage.RM_Logo)
                {
                    float fade = 60;
                    if (age < fade + 20f)
                    {
                        pos.y = 0f;
                        scroll = 0f;
                        (menu as MeadowCredits).scrollSpeed = 0f;
                    }
                    (subObjects[0] as MenuIllustration).alpha = Custom.SCurve(Mathf.InverseLerp(0f, fade, age), 0.65f);
                }
            }
        }

        public enum CreditsStage
        {
            Wait,
            RM_Logo,
            Contributors,
            Playtesters,
            End,
        }
    }
}
