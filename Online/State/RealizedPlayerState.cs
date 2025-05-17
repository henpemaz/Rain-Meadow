using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class VinePositionState : OnlineState
    {
        [OnlineField]
        ushort index;
        [OnlineFieldHalf]
        float floatPos;

        public VinePositionState() { }
        public VinePositionState(ClimbableVinesSystem.VinePosition vinePos, int index)
        {
            this.index = (ushort)index;
            floatPos = vinePos.floatPos;
        }

        public ClimbableVinesSystem.VinePosition GetVinePosition(ClimbableVinesSystem system) => new(system.vines[index], floatPos);
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class PlayerInAntlersState : OnlineState
    {
        [OnlineField(nullable = true)]
        public OnlinePhysicalObject? onlineDeer;
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
        private VinePositionState? vinePosState;
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
        private bool flipDirection;
        [OnlineField]
        private bool glowing;
        [OnlineField(nullable = true)]
        private OnlineEntity.EntityId? spearOnBack;
        [OnlineField(nullable = true)]
        private OnlineEntity.EntityId? slugcatRidingOnBack;
        private Player? slugcatOnBackTemp; // need this for clients to fix their overlap when slugpup is dropped
        [OnlineField(group = "inputs")]
        private ushort inputs;
        [OnlineFieldHalf(group = "inputs")]
        private float analogInputX;
        [OnlineFieldHalf(group = "inputs")]
        private float analogInputY;
        [OnlineFieldHalf(group = "saint")]
        private float burstX;
        [OnlineFieldHalf(group = "saint")]
        private float burstY;
        [OnlineField(group = "saint")]
        public bool monkAscension;

        [OnlineField(group = "saint", nullable = true)]
        TongueState? tongueState;

        [OnlineField(nullable = true)]
        TongueState? tongueStateOnBack; // for saint on back behavior

        [OnlineField(group = "watcher")]
        public bool isCamo;

        [OnlineFieldHalf(nullable = true)]
        private Vector2? pointingDir;

        public RealizedPlayerState() { }
        public RealizedPlayerState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            RainMeadow.Trace(this + " - " + onlineEntity);
            Player p = onlineEntity.apo.realizedObject as Player;
            isCamo = p.isCamo; // watcher

            monkAscension = p.monkAscension;
            animationIndex = (byte)p.animation.Index;
            animationFrame = (short)p.animationFrame;
            bodyModeIndex = (byte)p.bodyMode.Index;
            standing = p.standing;
            flipDirection = p.flipDirection > 0;
            glowing = p.glowing;
            burstX = p.burstX;
            burstY = p.burstY;
            spearOnBack = (p.spearOnBack?.spear?.abstractPhysicalObject is AbstractPhysicalObject apo
                && OnlinePhysicalObject.map.TryGetValue(apo, out var oe)) ? oe.id : null;
            slugcatRidingOnBack = (p.slugOnBack?.slugcat?.abstractPhysicalObject is AbstractPhysicalObject apo0
                && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe0)) ? oe0.id : null;
            if (p.tongue is Player.Tongue tongue)
            {
                tongueState = new TongueState(tongue);
            }

            if (p.onBack is null)
            {
                if (RainMeadow.HasSlugcatClassOnBack(p, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint, out var saint_player) &&
                        saint_player?.tongue is Player.Tongue tongue2)
                {
                    tongueStateOnBack = new TongueState(tongue2);
                }
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

            vinePosState = p.animation != Player.AnimationIndex.VineGrab || p.vinePos is null || p.room is null ? null : new VinePositionState(p.vinePos, p.room.climbableVines.vines.IndexOf(p.vinePos.vine));

            playerInAntlersState = p.playerInAntlers is null ? null : new PlayerInAntlersState(p.playerInAntlers);

            analogInputX = i.analogueDir.x;
            analogInputY = i.analogueDir.y;

            // Pointing
            if (p.graphicsModule is PlayerGraphics playerGraphics)
            {
                int handIndex = Pointing.GetHandIndex(p); //I don't trust this check to be fast
                if (handIndex >= 0 && playerGraphics.hands[handIndex].reachingForObject)
                    pointingDir = playerGraphics.hands[handIndex].absoluteHuntPos;
            }
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

        override public bool ShouldPosBeLenient(PhysicalObject po)
        {
            if (po is not Player p) { RainMeadow.Error("target is wrong type: " + po); return false; }

            if (p.onBack != null) return true;
            if (vinePosState is not null && p.animation == Player.AnimationIndex.VineGrab) return true;
            if (p.playerInAntlers is not null && p.playerInAntlers.deer == playerInAntlersState?.onlineDeer?.apo.realizedObject) return true;
            if (p.grabbedBy is not null && p.grabbedBy.Any(x => x.grabber is Player)) return true;
            return base.ShouldPosBeLenient(po);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            RainMeadow.Trace(this + " - " + onlineEntity);

            var oc = onlineEntity as OnlineCreature;
            var p = oc?.apo.realizedObject as Player;
            base.ReadTo(onlineEntity);
            if (p is null) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            //watcher
            p.isCamo = isCamo;
            p.monkAscension = monkAscension;
            p.burstY = burstY;
            p.burstX = burstX;
            var wasAnimation = p.animation;
            p.animation = new Player.AnimationIndex(Player.AnimationIndex.values.GetEntry(animationIndex));
            if (wasAnimation != p.animation) p.animationFrame = animationFrame;
            p.bodyMode = new Player.BodyModeIndex(Player.BodyModeIndex.values.GetEntry(bodyModeIndex));
            p.standing = standing;
            p.flipDirection = flipDirection ? 1 : -1;
            p.glowing = glowing;


            if (p.spearOnBack != null)
                p.spearOnBack.spear = (spearOnBack?.FindEntity() as OnlinePhysicalObject)?.apo?.realizedObject as Spear;

            if (p.slugOnBack != null)
            {
                if (p.slugOnBack.slugcat != null)
                {
                    slugcatOnBackTemp = p.slugOnBack.slugcat;
                    p.slugOnBack.slugcat.onBack = p;
                }

                p.slugOnBack.slugcat = (slugcatRidingOnBack?.FindEntity() as OnlinePhysicalObject)?.apo?.realizedObject as Player;

                if (p.slugOnBack.slugcat == null && slugcatOnBackTemp != null)
                {
                    p.slugOnBack.slugcat = slugcatOnBackTemp;
                    slugcatOnBackTemp.onBack = p;

                    p.slugOnBack.DropSlug();
                    slugcatOnBackTemp.onBack = null;
                    slugcatOnBackTemp = null;
                }
            }

            bool has_saint_on_back = RainMeadow.HasSlugcatClassOnBack(p, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint, out var saint_player);
            if (has_saint_on_back && p.onBack == null)
            {
                if (saint_player?.tongue is Player.Tongue tongue && tongueStateOnBack is not null)
                {
                    tongueStateOnBack.ReadTo(tongue);
                }
            }

            if (p.onBack == null || has_saint_on_back)
            {
                if (p.tongue is Player.Tongue tongue && tongueState is not null)
                {
                    tongueState.ReadTo(tongue);
                }
            }


            if (p.room?.climbableVines != null)
            {
                p.vinePos = vinePosState?.GetVinePosition(p.room.climbableVines);
                if (vinePosState is not null)
                {
                    p.room.climbableVines.ConnectChunkToVine(p.bodyChunks[0], p.vinePos, p.room.climbableVines.VineRad(p.vinePos));
                }
            }

            if (playerInAntlersState is not null)
            {
                playerInAntlersState.ReadTo(p);
            }
            else if (p.playerInAntlers is not null)
            {
                p.playerInAntlers.playerDisconnected = true;
                p.playerInAntlers = null;
            }

            // Pointing
            if (p.graphicsModule is PlayerGraphics playerGraphics)
            {
                p.handPointing = -1;
                int handIndex = Pointing.GetHandIndex(p); //I don't trust this check to be fast
                if (handIndex >= 0 && pointingDir is not null)
                {
                    playerGraphics.LookAtPoint(pointingDir.Value, Pointing.LookInterest);
                    playerGraphics.hands[handIndex].reachingForObject = true;
                    playerGraphics.hands[handIndex].absoluteHuntPos = pointingDir.Value;
                    p.handPointing = handIndex; //important! - check != -1 so we know we are in a "pointing state"
                }
            }
        }
    }
}
