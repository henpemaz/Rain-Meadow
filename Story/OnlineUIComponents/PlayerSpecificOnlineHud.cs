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
        public OnlinePlayerArrow playerArrow;
        public OnlinePlayerDeathBump deathBump;
        public int deadCounter = -1;
        public int antiDeathBumpFlicker;
        private List<OnlinePlayerHudPart> parts = new();

        public bool lastDead;
        public Player RealizedPlayer => this.abstractPlayer.realizedCreature as Player;

        public RoomCamera camera;
        public Vector2 drawpos;
        public bool found;
        private Vector2 pointDir;
        internal bool needed;

        public float DeadFade
        {
            get
            {
                return Mathf.InverseLerp(40f, 0f, (float)this.deadCounter);
            }
        }


        // todo tracking of player
        public PlayerSpecificOnlineHud(HUD.HUD hud, RoomCamera camera, StoryGameMode storyGameMode, StoryClientSettings clientSettings) : base(hud)
        {
            RainMeadow.Debug("Adding PlayerSpecificOnlineHud for " + clientSettings.owner);
            this.camera = camera;
            this.storyGameMode = storyGameMode;
            this.clientSettings = clientSettings;
            this.playerArrow = new OnlinePlayerArrow(this);
            this.parts.Add(this.playerArrow);

            needed = true;
        }

        public bool PlayerConsideredDead
        {
            get
            {
                return clientSettings.inGame && abstractPlayer != null 
                    && (
                        abstractPlayer.state.dead ||
                        (
                            abstractPlayer.realizedCreature is Player player &&
                            player.dangerGrasp != null && player.dangerGraspTime > 20
                        )
                    );
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

            if (camera.room == null) return;
            if (!clientSettings.inGame) return;
            if (abstractPlayer == null)
            {
                RainMeadow.Debug("finding player abscrt for " + clientSettings.owner);
                if(clientSettings.avatarId.FindEntity(true) is OnlineCreature oc)
                {
                    abstractPlayer = oc.abstractCreature;
                }
                return;
            }
            if (this.playerArrow == null)
            {
                RainMeadow.Debug("adding player arrow for " + clientSettings.owner);
                this.playerArrow = new OnlinePlayerArrow(this);
                this.parts.Add(this.playerArrow);
            }

            // tracking
            this.found = false;
            this.pointDir = Vector2.down;
            Vector2 rawPos = new();
            // in this room
            if (abstractPlayer.Room == camera.room.abstractRoom)
            {
                // in room or in shortcut
                if(abstractPlayer.realizedCreature is Player player)
                {
                    if (player.room == camera.room)
                    {
                        found = true;
                        rawPos = Vector2.Lerp(player.bodyChunks[0].pos, player.bodyChunks[1].pos, 0.33333334f) - camera.pos;
                    }
                    else
                    {
                        Vector2? shortcutpos = camera.game.shortcuts.OnScreenPositionOfInShortCutCreature(camera.room, player) - camera.pos;
                        if (shortcutpos != null)
                        {
                            found = true;
                            rawPos = shortcutpos.Value - camera.pos;
                        }
                    }
                }
            }
            else // neighbor room
            {
                var connections = camera.room.abstractRoom.connections;
                for (int i = 0; i < connections.Length; i++)
                {
                    if(abstractPlayer.pos.room == connections[i])
                    {
                        found = true;
                        rawPos = camera.room.MiddleOfTile(camera.room.LocalCoordinateOfNode(i)) - camera.pos;
                        break;
                    }
                }
                // not found, do not render?
                // todo use worldpos OR find shortcut that least to room player is in with pathfinding
            }

            this.drawpos = new Vector2(Mathf.Clamp(rawPos.x, 30f, this.camera.sSize.x - 30f), Mathf.Clamp(rawPos.y, 30f, this.camera.sSize.y - 30f));


            if (this.antiDeathBumpFlicker > 0)
            {
                this.antiDeathBumpFlicker--;
            }
            if (this.PlayerConsideredDead)
            {
                if (this.antiDeathBumpFlicker < 1)
                {
                    this.deadCounter++;
                    if (this.deadCounter == 10)
                    {
                        this.antiDeathBumpFlicker = 80;
                        this.deathBump = new OnlinePlayerDeathBump(this);
                        this.parts.Add(this.deathBump);
                    }
                }
            }
            else if (this.lastDead)
            {
                //Debug.Log("revivePlayer");
                this.antiDeathBumpFlicker = 80;
                if (this.deathBump != null)
                {
                    this.deathBump.removeAsap = true;
                }
                this.deadCounter = -1;
                this.hud.PlaySound(SoundID.UI_Multiplayer_Player_Revive);
                this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 30f, 4f, this.drawpos, this.hud.fContainers[1]));
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
