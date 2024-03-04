using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HUD;
using IL.HUD;
using On.HUD;
using System.Security.AccessControl;
using JollyCoop;

namespace RainMeadow
{
    public class OnlinePlayerIcon : OnlinePlayerHudPart
    {


        public FSprite iconSprite;

        public Color color;

        public Vector2 pos;

        public Vector2 lastPos;

        public float blink;

        public int blinkRed;

        public bool dead;

        public float lastBlink;



        public AbstractCreature player;

        public float rad;
        public float alpha;
        public float lastAlpha;
        public int counter;
        public int fadeAwayCounter;


        public PlayerState playerState => player.state as PlayerState;

        public Vector2 DrawPos()
        {
            return pos;
        }
        public OnlinePlayerIcon(PlayerSpecificOnlineHud owner) : base(owner)
        {
            this.owner = owner;
           
            RainWorldGame rainWorldGame = owner.hud.rainWorld.processManager.currentMainLoop as RainWorldGame;

            this.pos = new Vector2(rainWorldGame.rainWorld.options.ScreenSize.x - owner.hud.foodMeter.pos.x - 25f + (float)(3 - OnlineManager.players.Count), owner.hud.foodMeter.pos.y);
            this.iconSprite = new FSprite("Kill_Slugcat");

            this.color = owner.clientSettings.SlugcatColor();
            owner.hud.fContainers[0].AddChild(iconSprite);

        }

        public override void Draw(float timeStacker)
        {
            //float num = RWCustom.Custom.LerpAndTick(this.alpha, owner.needed ? 1 : 0, 0.08f, 0.033333335f);
            iconSprite.alpha = 1f;
            iconSprite.y = owner.hud.foodMeter.pos.y;
                // + (float)(dead ? 7 : 0);

            iconSprite.x = DrawPos().x;

            
/*            if (this.counter % 6 < 2 && this.lastBlink > 0f)
            {
                if (((Vector3)(Vector4)color).magnitude > 1.56f)
                {
                    color = Color.Lerp(color, new Color(0.9f, 0.9f, 0.9f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
                }
                else
                {
                    color = Color.Lerp(color, new Color(1f, 1f, 1f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
                }
            }*/

            iconSprite.color = owner.clientSettings.SlugcatColor();
        }

        public override void Update()
        {
            iconSprite.color = owner.clientSettings.SlugcatColor();

   /*         blink = Mathf.Max(0f, blink - 0.05f);
            lastBlink = blink;
            iconSprite.scale = 1f;*/
            /*            if (playerState.permaDead || playerState.dead)
                        {
                            color = Color.gray;
                            if (!dead)
                            {
                                iconSprite.RemoveFromContainer();
                                iconSprite = new FSprite("Multiplayer_Death");
                                iconSprite.scale *= 0.8f;
                                owner.hud.fContainers[0].AddChild(iconSprite);
                                dead = true;
                                blink = 3f;
                            }
                        }*/

        }
    }

}
