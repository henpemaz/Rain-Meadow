using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HUD;
using RainMeadow;
using RWCustom;
using UnityEngine;
using RainMeadow;

namespace RainMeadow
{
    public partial class OnlinePlayerSpecificHud
    {
        public partial class OnlineDeathBump : OnlinePointer
        {
            public FSprite symbolSprite;

            public int counter = -20;

            public float blink;

            public float lastBlink;

            public StoryAvatarSettings personaSettings;

            public bool removeAsap;

            public Vector2 pingPosition;

            public Vector2 lastPingPosition;

            public bool PlayerHasExplosiveSpearInThem
            {
                get
                {
                    if (jollyHud.RealizedPlayer == null)
                    {
                        return false;
                    }

                    if (jollyHud.RealizedPlayer.abstractCreature.stuckObjects.Count == 0)
                    {
                        return false;
                    }

                    for (int i = 0; i < jollyHud.RealizedPlayer.abstractCreature.stuckObjects.Count; i++)
                    {
                        if (jollyHud.RealizedPlayer.abstractCreature.stuckObjects[i].A is AbstractSpear && (jollyHud.RealizedPlayer.abstractCreature.stuckObjects[i].A as AbstractSpear).explosive)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public OnlineDeathBump(OnlinePlayerSpecificHud jollyHud)
                : base(jollyHud)
            {
                base.jollyHud = jollyHud;
                this.personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;

                SetPosToPlayer();
                gradient = new FSprite("Futile_White");
                gradient.shader = jollyHud.hud.rainWorld.Shaders["FlatLight"];
                if ((jollyHud.abstractPlayer.state as PlayerState).slugcatCharacter != SlugcatStats.Name.Night)
                {
                    gradient.color = new Color(0f, 0f, 0f);
                }

                jollyHud.hud.fContainers[0].AddChild(gradient);
                gradient.alpha = 0f;
                gradient.x = -1000f;
                symbolSprite = new FSprite("Multiplayer_Death");
                symbolSprite.color = personaSettings.bodyColor;
                jollyHud.hud.fContainers[0].AddChild(symbolSprite);
                symbolSprite.alpha = 0f;
                symbolSprite.x = -1000f;
            }

            public void SetPosToPlayer()
            {
                lastPingPosition = pingPosition;
            }

            public override void Update()
            {
                base.Update();

                lastAlpha = alpha;
                lastBlink = blink;
                lastPingPosition = pingPosition;
                pingPosition = bodyPos + new Vector2(0f, 10f);
                if (!jollyHud.PlayerRoomBeingViewed)
                {
                    slatedForDeletion = true;
                }

                if (counter < 0)
                {
                    SetPosToPlayer();
                    if (jollyHud.RealizedPlayer == null || jollyHud.RealizedPlayer.room == null || !jollyHud.RealizedPlayer.room.ViewedByAnyCamera(jollyHud.RealizedPlayer.mainBodyChunk.pos, 200f) || removeAsap || jollyHud.RealizedPlayer.grabbedBy.Count > 0)
                    {
                        counter = 0;
                        removeAsap = true;
                        jollyHud.hud.PlaySound(SoundID.UI_Multiplayer_Player_Dead_A);
                        jollyHud.hud.PlaySound(SoundID.UI_Multiplayer_Player_Dead_B);
                    }
                    else if (Custom.DistLess(jollyHud.RealizedPlayer.bodyChunks[0].pos, jollyHud.RealizedPlayer.bodyChunks[0].lastLastPos, 6f) && Custom.DistLess(jollyHud.RealizedPlayer.bodyChunks[1].pos, jollyHud.RealizedPlayer.bodyChunks[1].lastLastPos, 6f) && !PlayerHasExplosiveSpearInThem)
                    {
                        counter++;
                    }
                }

                counter++;
                if (removeAsap)
                {
                    counter += 10;
                }

                if (counter < 40)
                {
                    alpha = Mathf.Sin(Mathf.InverseLerp(0f, 40f, counter) * (float)Math.PI);
                    blink = Custom.LerpAndTick(blink, 1f, 0.07f, 71f / (678f * (float)Math.PI));
                    if (counter == 5)
                    {
                        if (!removeAsap)
                        {
                            jollyHud.hud.fadeCircles.Add(new FadeCircle(jollyHud.hud, 10f, 10f, 0.82f, 30f, 4f, bodyPos, jollyHud.hud.fContainers[1]));
                        }

                        jollyHud.hud.PlaySound(SoundID.UI_Multiplayer_Player_Dead_A);
                    }
                }
                else if (counter == 40)
                {
                    if (!removeAsap)
                    {
                        FadeCircle fadeCircle = new FadeCircle(jollyHud.hud, 20f, 30f, 0.94f, 60f, 4f, bodyPos, jollyHud.hud.fContainers[1]);
                        fadeCircle.alphaMultiply = 0.5f;
                        fadeCircle.fadeThickness = false;
                        jollyHud.hud.fadeCircles.Add(fadeCircle);
                        alpha = 1f;
                        blink = 0f;
                    }

                    jollyHud.hud.PlaySound(SoundID.UI_Multiplayer_Player_Dead_B);
                }
                else if (counter <= 220)
                {
                    alpha = Mathf.InverseLerp(220f, 110f, counter);
                }
                else if (counter > 220)
                {
                    slatedForDeletion = true;
                }
            }

            public override void Draw(float timeStacker)
            {
                this.personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;

                Vector2 vector = Vector2.Lerp(lastPingPosition, pingPosition, timeStacker) + new Vector2(0.01f, 0.01f);
                float num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lastAlpha, alpha, timeStacker)), 0.7f);
                gradient.x = vector.x;
                gradient.y = vector.y + 10f;
                gradient.scale = Mathf.Lerp(80f, 110f, num) / 16f;
                gradient.alpha = 0.17f * Mathf.Pow(num, 2f);
                symbolSprite.x = vector.x;
                symbolSprite.y = Mathf.Min(vector.y + Custom.SCurve(Mathf.InverseLerp(40f, 130f, (float)counter + timeStacker), 0.8f) * 80f, jollyHud.Camera.sSize.y - 30f);
                Color color = personaSettings.bodyColor;
                if (counter % 6 < 2 && lastBlink > 0f)
                {
                    color = Color.red; // Think this is for notifying when a slugcat is missing at Gate
                }

                symbolSprite.color = color;
                symbolSprite.alpha = num;
            }

            public override void ClearSprites()
            {
                base.ClearSprites();
                gradient.RemoveFromContainer();
                symbolSprite.RemoveFromContainer();
            }
        }
    }
}