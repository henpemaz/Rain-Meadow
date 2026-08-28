using System;
using HUD;
using RainMeadow.Arena.Nightcat;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public class PlayerSpecificOnlineHud : HudPart
    {
        public OnlineHUD owner;
        public OnlineGameMode onlineGameMode;
        public ClientSettings clientSettings;


        public OnlineEntity.EntityId playerId;
        public AbstractCreature abstractPlayer;
        private SlugcatCustomization customization;
        public OnlinePlayerDisplay playerDisplay;
        public OnlinePlayerDeathBump deathBump;
        //public NightcatHUD nightcatBump;

        public int deadCounter = -1;
        public int nightcatCounter = -1;

        public int antiDeathBumpFlicker;
        //public int antiNightcatFlicker;

        public List<OnlinePlayerHudPart> parts = new();
        public List<OnlineEntity.EntityId> killFeed = new();

        public bool lastDead;
        public Player RealizedPlayer => this.abstractPlayer.realizedCreature as Player;

        public RoomCamera camera;
        public Vector2 drawpos;
        private PositionState? _targetState;
        private PositionState? _prevTargetState;
        internal bool needed;
        private Vector2 _cameraRoomWorldPosInPixels;
        private Vector2 _targetRoomWorldPosInPixels;
        private int _prevCameraRoomIndex = -1;
        private int _prevTargetRoomIndex = -1;
        private static readonly IntVector2 outsideArenaDenPos = new IntVector2(-1, -1);

        public float DeadFade
        {
            get
            {
                return Mathf.InverseLerp(40f, 0f, (float)this.deadCounter);
            }
        }

        //public float NightcatFade
        //{
        //    get
        //    {
        //        return Mathf.InverseLerp(40f, 0f, (float)this.nightcatCounter);
        //    }
        //}

        public PlayerSpecificOnlineHud(OnlineHUD owner, RoomCamera camera, OnlineGameMode onlineGameMode, ClientSettings clientSettings, OnlineEntity.EntityId playerId) : base(owner.hud)
        {
            RainMeadow.Debug("Adding PlayerSpecificOnlineHud for " + clientSettings.owner);
            this.owner = owner;
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;
            this.clientSettings = clientSettings;
            this.playerId = playerId;

            needed = true;
        }

        public bool PlayerConsideredDead
        {
            get
            {
                return clientSettings.inGame && abstractPlayer != null && (
                    abstractPlayer.state.dead
                    || (RealizedPlayer?.dangerGrasp != null && RealizedPlayer.dangerGraspTime > 20)
                    );
            }
        }

        public bool PlayerInShelter
        {
            get
            {
                return abstractPlayer?.Room?.shelter ?? false;
            }
        }

        public bool PlayerInAncientShelter
        {
            get
            {
                return abstractPlayer?.Room?.isAncientShelter ?? false;
            }
        }

        public bool PlayerInGate
        {
            get
            {
                return abstractPlayer?.Room?.gate ?? false;
            }
        }

        private void UpdateParts()
        {
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

                    //else if (this.parts[i] == this.nightcatBump)
                    //{
                    //    this.nightcatBump = null;
                    //}

                    this.parts[i].ClearSprites();
                    this.parts.RemoveAt(i);
                }
                else
                {
                    this.parts[i].Update();
                }
            }
        }

        private void UpdatePlayer()
        {
            _prevTargetState = _targetState;
            _targetState = null;

            if (camera.room == null || !camera.room.shortCutsReady) return;
            if (!clientSettings.inGame) return;
            if (playerId.FindEntity(true) is OnlineCreature oc)
            {
                abstractPlayer = oc.abstractCreature;
                oc.TryGetData<SlugcatCustomization>(out customization);
            }
            else
            {
                return;
            }
            if (this.playerDisplay == null && customization != null)
            {
                RainMeadow.Debug("adding player arrow for " + clientSettings.owner);
                this.playerDisplay = new OnlinePlayerDisplay(this, customization, clientSettings.owner);
                this.parts.Add(this.playerDisplay);
            }

            if (abstractPlayer.pos.room != _prevTargetRoomIndex)
            {
                AbstractRoom? abstractRoom = camera.game.world.GetAbstractRoom(abstractPlayer.pos.room);
                if (abstractRoom is not null)
                    _targetRoomWorldPosInPixels = GetAbstractRoomWorldPosInPixels(abstractRoom);
                _prevTargetState = null;
            }
            _prevTargetRoomIndex = abstractPlayer.pos.room;

            if (camera.room.abstractRoom.index != _prevCameraRoomIndex)
            {
                _cameraRoomWorldPosInPixels = GetAbstractRoomWorldPosInPixels(camera.room.abstractRoom);
                _prevTargetState = null;
            }
            _prevCameraRoomIndex = camera.room.abstractRoom.index;

            bool isTargetInSameRoom = abstractPlayer.Room == camera.room.abstractRoom;

            if (!isTargetInSameRoom)
            {
                // try to find target in neighbor room
                int[] connections = camera.room.abstractRoom.connections;
                for (int i = 0; i < connections.Length; i++)
                {
                    if (abstractPlayer.pos.room != connections[i])
                        continue;
                    WorldCoordinate shortcutPos = camera.room.LocalCoordinateOfNode(i);
                    Vector2 dir = camera.room.ShorcutEntranceHoleDirection(shortcutPos.Tile).ToVector2();
                    Vector2 pos = camera.ApplyDepth(camera.room.MiddleOfTile(shortcutPos) + dir * 15f, -5f);
                    _targetState = new PositionState(
                        Position: _cameraRoomWorldPosInPixels + pos,
                        Direction: dir,
                        VisibilityOffset: 20f,
                        InvertArrow: true, // Point away from the shortcut entrance
                        PointTowardsDirection: false
                    );
                    break;
                }
            }
            bool isTargetInNeighborRoom = _targetState.HasValue;

            // in same or far room
            if (!isTargetInNeighborRoom)
            {
                Vector2? pos = null;

                Player? player = (Player?)abstractPlayer.realizedCreature;
                if (player is not null && (isTargetInSameRoom || DoesPlayerPosMatchPlayerACPos(player)))
                {
                    if (player is { inShortcut: true, inShortcutVessel: not null }
                        && player.inShortcutVessel.pos != outsideArenaDenPos) // avoiding that 0,0 shortcut
                    {
                        pos = _targetRoomWorldPosInPixels + camera.room.MiddleOfTile(player.inShortcutVessel.pos);
                    }
                    else {
                        BodyChunk[] chunks = player.bodyChunks;
                        pos = _targetRoomWorldPosInPixels + Vector2.Lerp(chunks[0].pos, chunks[1].pos, 1f / 3f);
                    }
                }

                if (!pos.HasValue && abstractPlayer.pos.TileDefined)
                {
                    pos = _targetRoomWorldPosInPixels + camera.room.MiddleOfTile(abstractPlayer.pos);
                }

                if (pos.HasValue)
                {
                    _targetState = new PositionState(
                        Position: pos.Value,
                        Direction: Vector2.down,
                        VisibilityOffset: 45f,
                        InvertArrow: false,
                        PointTowardsDirection: !isTargetInSameRoom
                    );
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
                    if (this.deadCounter == 10)
                    {
                        this.antiDeathBumpFlicker = 80;
                        this.deathBump = new OnlinePlayerDeathBump(this, customization);
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

                Vector2? drawPos = GetTargetDrawPos(1f)?.pos;
                if (drawPos.HasValue)
                    this.hud.fadeCircles.Add(new FadeCircle(this.hud, 10f, 10f, 0.82f, 30f, 4f, drawPos.Value, this.hud.fContainers[1]));
            }

            this.lastDead = this.PlayerConsideredDead;

            //if (this.antiNightcatFlicker > 0)
            //{
            //    this.antiNightcatFlicker--;
            //}

            //if (Nightcat.cooldownTimer == 0 && !Nightcat.notifiedPlayer && !Nightcat.firstTimeInitiating && RealizedPlayer != null && RealizedPlayer.SlugCatClass == SlugcatStats.Name.Night)
            //{
            //    if (this.antiNightcatFlicker < 1)
            //    {
            //        this.nightcatCounter++;
            //        if (this.nightcatCounter == 10)
            //        {
            //            this.antiNightcatFlicker = 80;
            //            this.nightcatBump = new NightcatHUD(this);
            //            this.parts.Add(this.nightcatBump);
            //            Nightcat.notifiedPlayer = true;
            //        }
            //    }
            //}

            //if (Nightcat.notifiedPlayer)
            //{
            //    if (this.nightcatBump != null)
            //    {
            //        this.nightcatBump.removeAsap = true;
            //    }
            //    this.nightcatCounter = -1;
            //}

        }

        public (Vector2 pos, Vector2 dir)? GetTargetDrawPos(float timeStacker)
        {
            PositionState? currentState = PositionState.Lerp(_prevTargetState, _targetState, timeStacker);
            if (!currentState.HasValue)
                return null;
            PositionState state = currentState.Value;

            Vector2 cameraPos = Vector2.Lerp(camera.lastPos, camera.pos, timeStacker);
            Vector2 cameraWorldPos = GetAbstractRoomWorldPosInPixels(camera.room.abstractRoom) + cameraPos;

            Vector2 targetPos = state.Position - cameraWorldPos;

            Rect cameraBounds = new(Vector2.zero, camera.sSize);
            Rect positionBounds = cameraBounds.CloneWithExpansion(-30f);

            if (state.PointTowardsDirection)
            {
                return (
                    pos: positionBounds.GetClosestPointOnEdgeAlongLineFromCenter(targetPos),
                    dir: (targetPos - positionBounds.center).normalized
                );
            }

            bool isTargetOnScreen = positionBounds.Contains(targetPos);

            Vector2 dir = isTargetOnScreen
                ? state.Direction
                : (targetPos - positionBounds.GetClosestInteriorPoint(targetPos)).normalized;

            targetPos -= dir * state.VisibilityOffset;

            return (
                pos: positionBounds.GetClosestInteriorPoint(targetPos),
                dir: isTargetOnScreen ? dir * (state.InvertArrow ? -1 : 1) : dir
            );
        }

        public override void Update()
        {
            base.Update();
            UpdatePlayer();
            UpdateParts();
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

        private static bool DoesPlayerPosMatchPlayerACPos(Player player)
        {
            AbstractCreature ac = player.abstractCreature;

            if (player is { inShortcut: true, inShortcutVessel: not null }
                && player.inShortcutVessel.pos != outsideArenaDenPos)
            {
                return player.inShortcutVessel.room.index == ac.pos.room;
            }

            if (!ac.pos.TileDefined || player.room?.abstractRoom.index != ac.pos.room)
                return false;

            // realizedCreature of a remote player can sometimes not be null when its actually not realized
            // so no new state for it is being sent, while new state for the abstractPlayer is always sent
            // so until this is fixed somewhere else in rain meadow,
            // we just check if the realized player's tile position matches what it should be
            const float toleranceInPixels = 20f; // up to one tile in each direction
            Vector2 posDiff = player.room.MiddleOfTile(ac.pos) - GetReferencePlayerACPosInPixels(player);
            return Math.Abs(posDiff.x) <= toleranceInPixels && Math.Abs(posDiff.y) <= toleranceInPixels;
        }

        /// <returns>
        /// The position of <paramref name="player"/>
        /// that is used to set <paramref name="player"/>'s <see cref="AbstractCreature.pos"/>.
        /// </returns>
        private static Vector2 GetReferencePlayerACPosInPixels(Player player)
        {
            // from PhysicalObject.Update
            Vector2 pos = player.FirstChunk().pos;

            if (!ModManager.MSC)
                return pos;

            // from Player.Update
            if (player.animation != Player.AnimationIndex.HangFromBeam
                && player.animation != Player.AnimationIndex.DeepSwim)
            {
                pos = player.bodyChunks[1].pos;
            }
            else if (player.animation == Player.AnimationIndex.BeamTip
                || player.animation == Player.AnimationIndex.StandOnBeam)
            {
                pos = player.bodyChunks[1].pos - new Vector2(0f, 20f);
            }
            else if (player.animation == Player.AnimationIndex.HangUnderVerticalBeam)
            {
                pos = player.bodyChunks[0].pos + new Vector2(0f, 20f);
            }

            return pos;
        }

        private static Vector2 GetAbstractRoomWorldPosInPixels(AbstractRoom abstractRoom)
        {
            return (abstractRoom.mapPos / 3f + new Vector2(10f, 10f) - abstractRoom.size.ToVector2() / 2f) * 20f;
        }

        private readonly record struct PositionState(
            Vector2 Position,
            Vector2 Direction,
            float VisibilityOffset,
            bool InvertArrow,
            bool PointTowardsDirection)
        {
            public Vector2 Position { get; } = Position;
            public Vector2 Direction { get; } = Direction;
            public float VisibilityOffset { get; } = VisibilityOffset;
            public bool InvertArrow { get; } = InvertArrow;
            public bool PointTowardsDirection { get; } = PointTowardsDirection;

            public static PositionState? Lerp(PositionState? a, PositionState? b, float t) => t switch {
                <= 0f => a ?? b,
                >= 1f => b,
                _     => LerpUnclamped(a, b, t)
            };

            private static PositionState? LerpUnclamped(PositionState? a, PositionState? b, float t)
            {
                if (!b.HasValue)
                    return null;
                if (!a.HasValue)
                    return b;
                PositionState from = a.Value;
                PositionState to = b.Value;
                return new PositionState(
                    Position: Vector2.LerpUnclamped(from.Position, to.Position, t),
                    Direction: Vector2.LerpUnclamped(from.Direction, to.Direction, t),
                    VisibilityOffset: Mathf.LerpUnclamped(from.VisibilityOffset, to.VisibilityOffset, t),
                    InvertArrow: to.InvertArrow,
                    PointTowardsDirection: to.PointTowardsDirection
                );
            }
        }
    }
}
