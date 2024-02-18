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
using RWCustom;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using HUD;
using RWCustom;
using UnityEngine;
using RainMeadow.Story.OnlineUIComponents;

namespace RainMeadow
{
    public partial class OnlinePlayerSpecificHud : HudPart
    {
        public abstract partial class OnlinePointer : OnlinePlayerHudPart
        {
            public FSprite gradient;

            public FSprite mainSprite;

            public float lastAlpha;

            public float alpha;

            public int timer;

            public int screenEdge = 25;

            public IntVector2 size;

            public int collidingCounter;

            public bool nearHorizontalEdge;

            public bool nearVerticalEdge;

            public bool nearEdge;

            public bool nearXEdgeR;

            public bool nearXEdgeL;

            public bool nearYEdgeU;

            public bool nearYEdgeB;
            public Dictionary<OnlinePlayerSpecificHud.OnlinePointer, Vector2> pointerPositions;
            public PlayerState PlayerState => jollyHud.PlayerState;

            public OnlinePointer(OnlinePlayerSpecificHud jollyHud)
                : base(jollyHud)
            {


                size = new IntVector2(30, 20);
                targetPos = bodyPos;
                pointerPositions = new Dictionary<OnlinePointer, Vector2>();


            }

            public override void Update()
            {
                base.Update();


                knownPos = false;
                forceHide = false;
                lastBodyPos = bodyPos;
                lastTargetPos = targetPos;
                lastAlpha = alpha;
                lastHidden = hidden;
                if (jollyHud.RealizedPlayer == null)
                {
                    return;
                }

                if (jollyHud.RealizedPlayer.room == null)
                {
                    Vector2? vector = jollyHud.Camera.game.shortcuts.OnScreenPositionOfInShortCutCreature(jollyHud.Camera.room, jollyHud.RealizedPlayer);
                    if (vector.HasValue)
                    {
                        bodyPos = vector.Value - jollyHud.Camera.pos;
                        knownPos = true;
                    }
                }
                else
                {
                    if (this is OnlinePlayerArrow && jollyHud.RealizedPlayer.objectPointed != null && jollyHud.RealizedPlayer.objectPointed.jollyBeingPointedCounter > 35)
                    {
                        bodyPos = jollyHud.RealizedPlayer.objectPointed.bodyChunks[0].pos - jollyHud.Camera.pos;
                    }
                    else
                    {
                        bodyPos = Vector2.Lerp(jollyHud.RealizedPlayer.bodyChunks[0].pos, jollyHud.RealizedPlayer.bodyChunks[1].pos, 0.333333343f) - jollyHud.Camera.pos;
                    }

                    knownPos = true;
                }

                nearXEdgeR = (double)(bodyPos.x - jollyHud.hud.rainWorld.options.ScreenSize.x) > -0.1;
                nearXEdgeL = (double)bodyPos.x < 0.1;
                nearHorizontalEdge = nearXEdgeR || nearXEdgeL;
                nearYEdgeU = (double)(bodyPos.y - jollyHud.hud.rainWorld.options.ScreenSize.y) > -0.1;
                nearYEdgeB = (double)bodyPos.y < 0.1;
                nearVerticalEdge = nearYEdgeU || nearYEdgeB;
                nearEdge = nearHorizontalEdge || nearVerticalEdge;
                if (hidden)
                {
                    targetPos = bodyPos;
                    lastTargetPos = targetPos;
                    return;
                }

                List<KeyValuePair<OnlinePlayerSpecificHud.OnlinePointer, Vector2>> list = pointerPositions.Where((KeyValuePair<OnlinePointer, Vector2> e) => !e.Key.Equals(this)).ToList();
                float num = bodyPos.y;
                bool flag = false;
                foreach (KeyValuePair<OnlinePointer, Vector2> item in list)
                {
                    OnlinePointer key = item.Key;
                    Vector2 value = item.Value;
                    if (key.hidden)
                    {
                        continue;
                    }

                    int num2 = (int)bodyPos.x;
                    int num3 = (int)value.x;
                    if (key.jollyHud.playerNumber > jollyHud.playerNumber)
                    {
                        targetPos = bodyPos;
                        continue;
                    }

                    bool num4 = num2 < num3 + key.size.x && num2 + size.x > num3;
                    bool flag2 = (nearXEdgeL && key.nearXEdgeL) || (nearXEdgeR && key.nearXEdgeR);
                    bool flag3 = (nearYEdgeB && key.nearYEdgeB) || (nearYEdgeU && key.nearYEdgeU);
                    if (num4 || flag2 || flag3)
                    {
                        int num5 = (int)value.y + key.size.y;
                        int num6 = (int)bodyPos.y;
                        if ((num6 < num5 && (float)(num6 + size.y) > value.y) || flag3 || flag2)
                        {
                            flag = true;
                            collidingCounter++;
                            num = Mathf.Max(num6, num5);
                            pointerPositions[this] = new Vector2(targetPos.x, num);
                        }
                    }
                }

                float t = ((collidingCounter > 5) ? (Mathf.Pow(collidingCounter, 1.4f) / 10f) : 0f);
                targetPos.y = Mathf.SmoothStep(bodyPos.y, num, t);
                if (!flag)
                {
                    collidingCounter = Mathf.Max(0, collidingCounter / 4);
                }
            }
        }
    }
}