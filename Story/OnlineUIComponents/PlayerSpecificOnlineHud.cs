using System.Collections.Generic;
using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class PlayerSpecificOnlineHud : HudPart
    {
        public StoryGameMode storyGameMode;
        public StoryClientSettings clientSettings;

        public AbstractCreature abstractPlayer;
        //public int flip = 1;
        public Vector2 cornerPos;
        public OnlinePlayerArrow playerArrow;
        public OnlinePlayerDeathBump deathBump;
        public int deadCounter = -1;
        public int antiDeathBumpFlicker;
        private List<OnlinePlayerHudPart> parts;

        public bool lastDead;
        public Player RealizedPlayer
        {
            get
            {
                return this.abstractPlayer.realizedCreature as Player;
            }
        }

        public RoomCamera camera;

        public float DeadFade
        {
            get
            {
                return Mathf.InverseLerp(40f, 0f, (float)this.deadCounter);
            }
        }


        // todo tracking of player
        public PlayerSpecificOnlineHud(HUD.HUD hud, StoryGameMode storyGameMode, StoryClientSettings clientSettings) : base(hud)
        {
            camera = (hud.owner as RoomCamera);
            this.cornerPos = new Vector2(hud.rainWorld.options.SafeScreenOffset.x, 20f + hud.rainWorld.options.SafeScreenOffset.y);

            this.parts = new List<OnlinePlayerHudPart>();
            this.playerArrow = new OnlinePlayerArrow(this);
            this.parts.Add(this.playerArrow);
            this.storyGameMode = storyGameMode;
            this.clientSettings = clientSettings;
        }

        public bool PlayerConsideredDead
        {
            get
            {
                return clientSettings.inGame && abstractPlayer != null && abstract;
            }
        }

        public override void Update()
        {
            base.Update();
            for (int i = this.parts.Count - 1; i >= 0; i--)
            {
                if (this.parts[i].slatedForDeletion)
                {
                    if (this.parts[i] == this.playerArrow)
                    {
                        this.playerArrow = null;
                    }
                    else if (this.parts[i] == this.deathBump)
                    {
                        this.deathBump = null;
                    }
                    this.parts[i].ClearSprites();
                    this.parts.RemoveAt(i);
                }
                else
                {
                    this.parts[i].Update();
                }
            }
            if (this.antiDeathBumpFlicker > 0)
            {
                this.antiDeathBumpFlicker--;
            }
            if (this.PlayerConsideredDead)
            {
                if (this.antiDeathBumpFlicker < 1)
                {
                    this.deadCounter++;
                    if (this.deadCounter == 10 && this.storyGameMode.SessionStillGoing)
                    {
                        this.antiDeathBumpFlicker = 80;
                        this.deathBump = new OnlinePlayerDeathBump(this);
                        this.parts.Add(this.deathBump);
                    }
                }
            }
            else if (this.lastDead && this.storyGameMode.SessionStillGoing)
            {
                Debug.Log("revivePlayer");
                this.antiDeathBumpFlicker = 80;
                if (this.deathBump != null)
                {
                    this.deathBump.removeAsap = true;
                }
                this.deadCounter = -1;
                this.hud.PlaySound(SoundID.UI_Multiplayer_Player_Revive);
                if (this.RealizedPlayer != null)
                {
                    this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 30f, 4f, this.RealizedPlayer.mainBodyChunk.pos, this.hud.fContainers[1]));
                }
                if (this.playerArrow == null)
                {
                    this.playerArrow = new OnlinePlayerArrow(this);
                    this.parts.Add(this.playerArrow);
                }
            }
            this.lastDead = this.PlayerConsideredDead;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            for (int i = 0; i < this.parts.Count; i++)
            {
                this.parts[i].Draw(timeStacker);
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            for (int i = 0; i < this.parts.Count; i++)
            {
                this.parts[i].ClearSprites();
            }
        }
    }
}
