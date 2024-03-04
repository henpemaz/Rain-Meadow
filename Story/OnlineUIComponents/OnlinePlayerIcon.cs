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
        public AbstractCreature abstractPlayer;

        public Player RealizedPlayer => this.abstractPlayer.realizedCreature as Player;

        public float lastBlink;
        public float rad;
        public float alpha;
        public float lastAlpha;
        public int counter;
        public int fadeAwayCounter;

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

            iconSprite.alpha = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(this.lastAlpha, this.alpha, timeStacker)), 0.7f);
            iconSprite.y = owner.hud.foodMeter.pos.y;
            iconSprite.x = DrawPos().x;
            iconSprite.color = owner.clientSettings.SlugcatColor();
        }

        public override void Update()
        {
            base.Update();
            if (Input.GetKey(RainMeadow.rainMeadowOptions.FriendsListKey.Value))
            {
                this.lastAlpha = this.alpha;
                this.alpha = RWCustom.Custom.LerpAndTick(this.alpha, owner.needed ? 1 : 0, 0.08f, 0.033333335f);

            }
            else
            {
                this.alpha = RWCustom.Custom.LerpAndTick(this.alpha, owner.needed ? 0 : 1, 0.08f, 0.033333335f);
                this.lastAlpha = this.alpha;

            }
            iconSprite.color = color;
/*            if (RealizedPlayer.playerState.permaDead || (RealizedPlayer.playerState.dead))
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
