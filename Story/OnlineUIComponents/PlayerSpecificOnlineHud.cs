using System.Collections.Generic;
using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class PlayerSpecificOnlineHud : HudPart
    {
        public OnlineGameMode onlineGameMode;
        public ClientSettings clientSettings;

        public AbstractCreature abstractPlayer;
        public OnlinePlayerDisplay playerDisplay;
        public OnlinePlayerDeathBump deathBump;
        public int deadCounter = -1;
        public int antiDeathBumpFlicker;
        private List<OnlinePlayerHudPart> parts = new();

        public bool lastDead;
        public Player RealizedPlayer => this.abstractPlayer.realizedCreature as Player;

        public RoomCamera camera;
        private Rect camrect;
        public Vector2 drawpos;
        public bool found;
        public Vector2 pointDir;
        internal bool needed;
        private WorldCoordinate lastWorldPos;
        private int lastCameraPos;
        private int lastAbstractRoom;

        public float DeadFade
        {
            get
            {
                return Mathf.InverseLerp(40f, 0f, (float)this.deadCounter);
            }
        }

        public PlayerSpecificOnlineHud(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode, ClientSettings clientSettings) : base(hud)
        {
            RainMeadow.Debug("Adding PlayerSpecificOnlineHud for " + clientSettings.owner);
            this.camera = camera;
            camrect = new Rect(Vector2.zero, this.camera.sSize).CloneWithExpansion(-30f);
            this.onlineGameMode = onlineGameMode;
            this.clientSettings = clientSettings;

            this.playerDisplay = new OnlinePlayerDisplay(this);

            this.parts.Add(this.playerDisplay);

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
                    if (this.parts[i] == this.playerDisplay)
                    {
                        this.playerDisplay = null;
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
                if (clientSettings.avatarId.FindEntity(true) is OnlineCreature oc)
                {
                    abstractPlayer = oc.abstractCreature;
                }
                return;
            }
            if (this.playerDisplay == null)
            {
                RainMeadow.Debug("adding player arrow for " + clientSettings.owner);
                this.playerDisplay = new OnlinePlayerDisplay(this);
                this.parts.Add(this.playerDisplay);
            }


            // tracking
            this.found = false;

            Vector2 rawPos = new();
            // in this room
            if (abstractPlayer.Room == camera.room.abstractRoom)
            {
                // in room or in shortcut
                if (abstractPlayer.realizedCreature is Player player)
                {
                    if (player.room == camera.room)
                    {
                        found = true;
                        rawPos = Vector2.Lerp(player.bodyChunks[0].pos, player.bodyChunks[1].pos, 0.33333334f) - camera.pos;
                        this.pointDir = Vector2.down;
                    }
                    else
                    {
                        Vector2? shortcutpos = camera.game.shortcuts.OnScreenPositionOfInShortCutCreature(camera.room, player);
                        if (shortcutpos != null)
                        {
                            found = true;
                            rawPos = shortcutpos.Value - camera.pos;
                            this.pointDir = Vector2.down;
                        }
                    }
                }

                if (found)
                {
                    this.drawpos = camrect.GetClosestInteriorPoint(rawPos); // gives straight arrows
                    if (drawpos != rawPos)
                    {
                        pointDir = (rawPos - drawpos).normalized;
                    }
                }
            }
            else // different room
            {
                // neighbor
                var connections = camera.room.abstractRoom.connections;
                for (int i = 0; i < connections.Length; i++)
                {
                    if (abstractPlayer.pos.room == connections[i])
                    {
                        found = true;
                        var shortcutpos = camera.room.LocalCoordinateOfNode(i);
                        rawPos = camera.room.MiddleOfTile(shortcutpos) - camera.pos;
                        pointDir = camera.room.ShorcutEntranceHoleDirection(shortcutpos.Tile).ToVector2() * -1;
                        break;
                    }
                }
                if (found)
                {
                    this.drawpos = camrect.GetClosestInteriorPoint(rawPos);
                    Vector2 translation = pointDir * 10f; // Vector shift for shortcut viewability
                    this.drawpos += translation;

                    if (drawpos != rawPos)
                    {
                        pointDir = (rawPos - drawpos).normalized * -1; // Point away from the shortcut entrance
                    }
                }
                else // elsewhere, use world pos
                {
                    var world = camera.game.world;
                    if (world.GetAbstractRoom(abstractPlayer.pos.room) is AbstractRoom abstractRoom) // room in region
                    {
                        found = true;
                        if (abstractPlayer.pos != lastWorldPos || camera.currentCameraPosition != lastCameraPos || camera.room.abstractRoom.index != lastAbstractRoom) // cache these maths
                        {
                            var worldpos = (abstractRoom.mapPos / 3f + new Vector2(10f, 10f)) * 20f;
                            if (this.abstractPlayer.realizedCreature is Creature creature) worldpos += creature.mainBodyChunk.pos - abstractRoom.size.ToVector2() * 20f / 2f;
                            else if (abstractPlayer.pos.TileDefined) worldpos += abstractPlayer.pos.Tile.ToVector2() * 20f - abstractRoom.size.ToVector2() * 20f / 2f;

                            var viewpos = (camera.room.abstractRoom.mapPos / 3f + new Vector2(10f, 10f)) * 20f + camera.pos + this.camera.sSize / 2f - camera.room.abstractRoom.size.ToVector2() * 20f / 2f;

                            pointDir = (worldpos - viewpos).normalized;
                            drawpos = camrect.GetClosestInteriorPointAlongLineFromCenter(this.camera.sSize / 2f + pointDir * 2048f); // gives angled arrows
                        }
                    }
                }
            }

            lastWorldPos = abstractPlayer.pos;
            lastCameraPos = camera.currentCameraPosition;
            lastAbstractRoom = camera.room.abstractRoom.index;

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
