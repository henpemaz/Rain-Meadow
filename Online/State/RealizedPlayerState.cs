using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class PlayerInAntlersState : OnlineState
    {
        [OnlineField(nullable = true)]
        OnlinePhysicalObject? onlineDeer;
        [OnlineField]
        bool dangle;
        [OnlineField(nullable = true)]
        OnlineAntlerPoint? upperAntlerPoint;
        [OnlineField(nullable = true)]
        OnlineAntlerPoint? lowerAntlerPoint;

        [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
        public class OnlineAntlerPoint : OnlineState
        {
            [OnlineField]
            int part;
            [OnlineField]
            int segment;
            [OnlineFieldHalf]
            float side;

            public OnlineAntlerPoint() { }
            public OnlineAntlerPoint(Deer.PlayerInAntlers.AntlerPoint antlerPoint)
            {
                part = antlerPoint.part;
                segment = antlerPoint.segment;
                side = antlerPoint.side;
            }

            public Deer.PlayerInAntlers.AntlerPoint GetAntlerPoint() => new(part, segment, side);
        }

        public PlayerInAntlersState() { }
        public PlayerInAntlersState(Deer.PlayerInAntlers playerInAntlers)
        {
            onlineDeer = playerInAntlers.deer?.abstractPhysicalObject.GetOnlineObject();
            dangle = playerInAntlers.dangle;
            if (playerInAntlers.stance is Deer.PlayerInAntlers.GrabStance stance)
            {
                upperAntlerPoint = stance.upper is null ? null : new(stance.upper);
                lowerAntlerPoint = stance.lower is null ? null : new(stance.lower);
            }
        }

        public void ReadTo(Deer.PlayerInAntlers playerInAntlers)
        {
            var deer = playerInAntlers.deer;
            if (!deer.playersInAntlers.Contains(playerInAntlers))
                deer.playersInAntlers.Add(playerInAntlers);
            playerInAntlers.dangle = dangle;
            if (playerInAntlers.stance is Deer.PlayerInAntlers.GrabStance stance)
            {
                stance.upper = upperAntlerPoint?.GetAntlerPoint();
                stance.lower = lowerAntlerPoint?.GetAntlerPoint();
            }
        }

        public void ReadTo(Player player)
        {
            if (onlineDeer?.apo.realizedObject is not Deer deer) { RainMeadow.Error("deer not found: " + onlineDeer); return; }
            if (player.playerInAntlers is not null && player.playerInAntlers.deer != deer)  // we are on the wrong deer
            {
                player.playerInAntlers.playerDisconnected = true;
                player.playerInAntlers = null;
            }
            player.playerInAntlers ??= new Deer.PlayerInAntlers(player, deer);
            this.ReadTo(player.playerInAntlers);
        }
    }

    public class RealizedPlayerState : RealizedCreatureState
    {
        [OnlineField(nullable = true)]
        private PlayerInAntlersState? playerInAntlersState;
        [OnlineField]
        private byte animationIndex;
        [OnlineField]
        private short animationFrame;
        [OnlineField]
        private byte bodyModeIndex;
        [OnlineField]
        private bool standing;
        [OnlineField]
        private bool glowing;
        [OnlineField]
        private bool isPup;
        [OnlineField(nullable = true)]
        private OnlineEntity.EntityId? spearOnBack;
        //[OnlineField(nullable = true)]
        //private OnlineEntity.EntityId? slugOnBack;
        [OnlineField(group = "inputs")]
        private ushort inputs;
        [OnlineFieldHalf(group = "inputs")]
        private float analogInputX;
        [OnlineFieldHalf(group = "inputs")]
        private float analogInputY;

        [OnlineField(group = "tongue")]
        public byte tongueMode;
        [OnlineField(group = "tongue")]
        public Vector2 tonguePos;
        [OnlineFieldHalf(group = "tongue")]
        public float tongueIdealLength;
        [OnlineFieldHalf(group = "tongue")]
        public float tongueRequestedLength;
        [OnlineField(group = "tongue", nullable = true)]
        public BodyChunkRef tongueAttachedChunk;

        public RealizedPlayerState() { }
        public RealizedPlayerState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            RainMeadow.Trace(this + " - " + onlineEntity);
            Player p = onlineEntity.apo.realizedObject as Player;
            animationIndex = (byte)p.animation.Index;
            animationFrame = (short)p.animationFrame;
            bodyModeIndex = (byte)p.bodyMode.Index;
            standing = p.standing;
            glowing = p.glowing;
            isPup = p.playerState.isPup;
            spearOnBack = (p.spearOnBack?.spear?.abstractPhysicalObject is AbstractPhysicalObject apo
                && OnlinePhysicalObject.map.TryGetValue(apo, out var oe)) ? oe.id : null;
            //slugOnBack = (p.slugOnBack?.slugcat?.abstractPhysicalObject is AbstractPhysicalObject apo0
            //    && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe0)) ? oe0.id : null;
            if (p.tongue is Player.Tongue tongue)
            {
                tongueMode = (byte)tongue.mode;
                tonguePos = tongue.pos;
                tongueIdealLength = tongue.idealRopeLength;
                tongueRequestedLength = tongue.requestedRopeLength;
                tongueAttachedChunk = BodyChunkRef.FromBodyChunk(tongue.attachedChunk);
            }
            var i = p.input[0];
            inputs = (ushort)(
                  (i.x == 1 ? 1 << 0 : 0)
                | (i.x == -1 ? 1 << 1 : 0)
                | (i.y == 1 ? 1 << 2 : 0)
                | (i.y == -1 ? 1 << 3 : 0)
                | (i.downDiagonal == 1 ? 1 << 4 : 0)
                | (i.downDiagonal == -1 ? 1 << 5 : 0)
                | (i.pckp ? 1 << 6 : 0)
                | (i.jmp ? 1 << 7 : 0)
                | (i.thrw ? 1 << 8 : 0)
                | (i.mp ? 1 << 9 : 0));

            playerInAntlersState = p.playerInAntlers is null ? null : new PlayerInAntlersState(p.playerInAntlers);

            analogInputX = i.analogueDir.x;
            analogInputY = i.analogueDir.y;
        }
        public Player.InputPackage GetInput()
        {
            RainMeadow.Trace(inputs);
            Player.InputPackage i = default;
            if (((inputs >> 0) & 1) != 0) i.x = 1;
            if (((inputs >> 1) & 1) != 0) i.x = -1;
            if (((inputs >> 2) & 1) != 0) i.y = 1;
            if (((inputs >> 3) & 1) != 0) i.y = -1;
            if (((inputs >> 4) & 1) != 0) i.downDiagonal = 1;
            if (((inputs >> 5) & 1) != 0) i.downDiagonal = -1;
            if (((inputs >> 6) & 1) != 0) i.pckp = true;
            if (((inputs >> 7) & 1) != 0) i.jmp = true;
            if (((inputs >> 8) & 1) != 0) i.thrw = true;
            if (((inputs >> 9) & 1) != 0) i.mp = true;
            i.analogueDir.x = analogInputX;
            i.analogueDir.y = analogInputY;
            return i;
        }
        
        public override void ReadTo(OnlineEntity onlineEntity)
        {
            RainMeadow.Trace(this + " - " + onlineEntity);
            if (playerInAntlersState != null) this.chunkPosLeniency = 20f * 4;
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is Player pl)
            {
                var wasAnimation = pl.animation;
                pl.animation = new Player.AnimationIndex(Player.AnimationIndex.values.GetEntry(animationIndex));
                if (wasAnimation != pl.animation) pl.animationFrame = animationFrame;
                pl.bodyMode = new Player.BodyModeIndex(Player.BodyModeIndex.values.GetEntry(bodyModeIndex));
                pl.standing = standing;
                pl.glowing = glowing;
                if (pl.playerState.isPup != isPup)
                    pl.playerState.isPup = isPup;
                if (pl.spearOnBack != null)
                    pl.spearOnBack.spear = (spearOnBack?.FindEntity() as OnlinePhysicalObject)?.apo?.realizedObject as Spear;
                //if (pl.slugOnBack != null)
                //    pl.slugOnBack.slugcat = (slugOnBack?.FindEntity() as OnlinePhysicalObject)?.apo?.realizedObject as Player;

                if (pl.tongue is Player.Tongue tongue)
                {
                    tongue.mode = new Player.Tongue.Mode(Player.Tongue.Mode.values.GetEntry(tongueMode));
                    tongue.pos = tonguePos;
                    tongue.idealRopeLength = tongueIdealLength;
                    tongue.requestedRopeLength = tongueRequestedLength;
                    if (tongue.mode == Player.Tongue.Mode.AttachedToTerrain)
                    {
                        tongue.terrainStuckPos = tongue.pos;
                    }
                    else if (tongue.mode == Player.Tongue.Mode.AttachedToObject)
                    {
                        tongue.attachedChunk = tongueAttachedChunk.ToBodyChunk();
                    }
                }

                if (playerInAntlersState is not null) playerInAntlersState.ReadTo(pl);
                else if (pl.playerInAntlers is not null)
                {
                    pl.playerInAntlers.playerDisconnected = true;
                    pl.playerInAntlers = null;
                }
            }
            else
            {
                RainMeadow.Error("target not realized: " + onlineEntity);
            }
        }
    }
}
