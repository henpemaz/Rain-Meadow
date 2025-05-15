using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public partial class WorldSession : OnlineResource
    {
        public Region region;
        public World world;
        public static ConditionalWeakTable<World, WorldSession> map = new();
        public Dictionary<string, RoomSession> roomSessions = new();
        public World World => world;

        public WorldSession(Region region, Lobby lobby) : base(lobby)
        {
            this.region = region;
        }

        public void BindWorld(World world)
        {
            this.world = world;
            if (RainMeadow.isArenaMode(out var _))
            {

                world.region = new Region("arena", 0, 0, SlugcatStats.SlugcatToTimeline(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer));
            }
            map.Add(world, this);
        }

        protected override void AvailableImpl()
        {

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
        }

        protected override void DeactivateImpl()
        {
            this.roomSessions.Clear();
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
            return region.name;
        }

        public override ushort ShortId()
        {
            return (ushort)region.regionNumber;
        }

        public override OnlineResource SubresourceFromShortId(ushort shortId)
        {
            return this.subresources[shortId - region.firstRoomIndex];
        }

        public class WorldState : ResourceWithSubresourcesState
        {
            [OnlineField]
            public RainCycleData rainCycleData;
            [OnlineField(nullable: true)]
            public Generics.DynamicOrderedUshorts realizedRooms;
            public WorldState() : base() { }
            public WorldState(WorldSession resource, uint ts) : base(resource, ts)
            {
                if (resource.world != null)
                {
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
                    RainCycle cycle = ws.world.rainCycle;
                    cycle.preTimer = rainCycleData.preTimer;
                    cycle.timer = rainCycleData.timer;
                    cycle.cycleLength = rainCycleData.cycleLength;

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