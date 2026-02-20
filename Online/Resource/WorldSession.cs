using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
namespace RainMeadow
{
    public partial class WorldSession : OnlineResource
    {
        public World world;
        public WorldLoader worldLoader;
        public static ConditionalWeakTable<World, WorldSession> map = new();

        
        
        /// <summary>
        /// A centralized coroutine helper that waits for a WorldSession's participants to clear before proceeding.
        /// It enforces a 5-second safety timeout to prevent softlocks (deadlocks) if entities fail to remove.
        /// </summary>
        /// <param name="session">The WorldSession currently transitioning.</param>
        /// <param name="extraWaitCondition">Optional: An additional condition that must remain true to keep waiting (e.g., !newWorldSession.isAvailable). Pass null if not needed.</param>
        /// <param name="onComplete">The action/method to execute once the wait finishes or times out (e.g., orig(self, ...)).</param>
        public static System.Collections.IEnumerator WaitAndExecuteSession(
            WorldSession session, 
            System.Func<bool> extraWaitCondition, 
            System.Action onComplete)
        {
            float startTime = UnityEngine.Time.time;
            float timeoutSeconds = 5f;
            session.transitionInProgress = true;

            if (OnlineManager.lobby.gameMode is not MeadowGameMode) {
                while (session.participants.Count > 0 && 
                    (extraWaitCondition == null || extraWaitCondition()) && 
                    (UnityEngine.Time.time - startTime < timeoutSeconds))
                {
                    RainMeadow.Debug($"Waiting for {session.participants.Count} to leave...");
                    yield return null;
                }
            } else
            {
                 while ((extraWaitCondition == null || extraWaitCondition()) && 
                    (UnityEngine.Time.time - startTime < timeoutSeconds))
                {
                    RainMeadow.Debug($"Waiting for {session.participants.Count} to leave...");
                    yield return null;
                }
            }

            if (UnityEngine.Time.time - startTime >= timeoutSeconds)
            {
                RainMeadow.Debug("WaitLoop timed out after 5 seconds. Proceeding anyway to prevent deadlock.");
            }
            else
            {
                RainMeadow.Debug("Entities removed. Proceeding...");
            }

            session.transitionInProgress = false;
            onComplete?.Invoke();
        }
        public Dictionary<string, RoomSession> roomSessions = new();
        public World World => world;
        public OverworldSession overworldSession => (OverworldSession)super;

        public string worldID;
        public ushort shortWorldID;

        public WorldSession(string worldID, ushort shortWorldID, OverworldSession overworld) : base(overworld)
        {
            this.worldID = worldID;
            this.shortWorldID = shortWorldID;
        }

        public void BindWorld(WorldLoader worldLoader, World world)
        {
            this.world = world;
            this.worldLoader = worldLoader;
            map.Add(world, this);
        }

        protected override void AvailableImpl()
        {
            if (worldLoader != null)
            {
                worldLoader.setupValues.worldCreaturesSpawn = OnlineManager.lobby.gameMode.ShouldLoadCreatures(worldLoader.game, this);
            }
        }

        protected override void ActivateImpl()
        {
            if (world == null) throw new InvalidOperationException("world not set");
            if (world.abstractRooms == null) throw new InvalidOperationException("world.abstractRooms is null");
            foreach (var room in world.abstractRooms)
            {
                var rs = new RoomSession(this, room);
                try
                {
                    roomSessions.Add(room.name, rs);
                }
                catch (Exception)
                {
                    RainMeadow.Error($"duplicate room {room.name} for rs {rs}");
                    var name = "";
                    for (var i = 0; roomSessions.Keys.Contains((name = room.name + "." + i)); i++) ;
                    RainMeadow.Error($"adding as {name}");
                    roomSessions.Add(name, rs);
                }
                subresources.Add(rs);
            }
            foreach (var item in earlyApos)
            {
                ApoEnteringWorld(item);
            }
            earlyApos.Clear();

            worldLoader = null;
        }

        protected override void DeactivateImpl()
        {
            this.roomSessions.Clear();
            if (world != null && map.TryGetValue(world, out var ws) && ws == this) map.Remove(world);
            world = null;
        }

        protected override void UnavailableImpl()
        {

        }

        protected override ResourceState MakeState(uint ts)
        {
            return new WorldState(this, ts);
        }

        public override string Id()
        {
            return worldID;
        }

        public override ushort ShortId()
        {
            return shortWorldID;
        }

        public override OnlineResource SubresourceFromShortId(ushort shortId)
        {
            return this.subresources[shortId];
        }

        public class WorldState : ResourceWithSubresourcesState
        {
            [OnlineField]
            public RainCycleData rainCycleData;
            [OnlineField(nullable: true)]
            public Generics.DynamicOrderedUshorts realizedRooms;

            // Used to sync RainWorldGame.clock which is used for a few APOs introduced in the Watcher as well as a few
            // other functions in the game. Should be safe to update this way but I would keep a watch on it for a little bit.
            [OnlineField]
            public int clock;
            public WorldState() : base() { }
            public WorldState(WorldSession resource, uint ts) : base(resource, ts)
            {
                if (resource.world != null)
                {
                    clock = resource.world.game.clock;

                    RainCycle rainCycle = resource.world.rainCycle;
                    if (rainCycle.brokenAntiGrav == null)
                    {
                        rainCycle.brokenAntiGrav = new AntiGravity.BrokenAntiGravity(
                            resource.world.game.setupValues.gravityFlickerCycleMin,
                            resource.world.game.setupValues.gravityFlickerCycleMax,
                            resource.world.game);
                    }
                    rainCycleData = new RainCycleData(rainCycle);
                    realizedRooms = new(resource.world.abstractRooms.Where(s => s.firstTimeRealized == false).Select(r => (ushort)r.index).ToList());
                }
            }

            public override void ReadTo(OnlineResource resource)
            {
                if (resource.isActive)
                {
                    var ws = (WorldSession)resource;

                    ws.world.game.clock = clock;

                    RainCycle cycle = ws.world.rainCycle;
                    cycle.preTimer = rainCycleData.preTimer;
                    cycle.timer = rainCycleData.timer;
                    cycle.cycleLength = rainCycleData.cycleLength;

                    cycle.waterCycle.timeInStage = rainCycleData.timeInStage;
                    cycle.waterCycle.stageDuration = rainCycleData.stageDuration;
                    cycle.waterCycle.stage = (WaterLevelCycle.Stage)rainCycleData.stage;

                    ws.world.game.globalRain.waterFluxTicker = rainCycleData.waterFluxTicker;

                    if (realizedRooms != null)
                    {
                        foreach (var index in realizedRooms.list)
                        {
                            var abstractRoom = ws.world.GetAbstractRoom(index);
                            if (abstractRoom != null)
                                abstractRoom.firstTimeRealized = false;
                            else
                            {
                                if (!ws.alreadyLogged.Contains(index))
                                {
                                    RainMeadow.Error($"Room not found in region: {index} in {ws}");
                                    RainMeadow.Error($"Region spans indexes: {ws.world.firstRoomIndex} to {ws.world.firstRoomIndex + ws.world.NumberOfRooms}");

                                    ws.alreadyLogged.Add(index);
                                }
                            }
                        }
                    }
                }

                base.ReadTo(resource);
            }
        }
        HashSet<int> alreadyLogged = new();

        public override string ToString()
        {
            return "Region " + Id();
        }
    }
}
