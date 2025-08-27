
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public partial class OverworldSession : OnlineResource
    {
        public OverWorld? overWorld = null;
        public Dictionary<string, WorldSession> worldSessions = new();
        public OverworldSession(Lobby lobby) : base(lobby) { }
        public void BindOverworld(OverWorld overWorld)
        {
            RainMeadow.DebugMe();
            if (this.overWorld is not null) RainMeadow.Error("overWorld is not null, Overriding...");
            this.overWorld = overWorld;
        }

        protected override void AvailableImpl()
        {

        }

        protected override void ActivateImpl()
        {
            if (overWorld is null) throw new InvalidProgrammerException("overWorld is null");
            if (RainMeadow.isArenaMode(out var _)) // Arena
            {
                Region arenaRegion = new Region("arena", 0, 0, RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer);

                var ws = new WorldSession(arenaRegion, this);
                worldSessions.Add(arenaRegion.name, ws);
                subresources.Add(ws);

                RainMeadow.Debug(subresources.Count);
            }
            else
            {
                foreach (var r in overWorld.regions)
                {
                    RainMeadow.Debug(r.name);
                    var ws = new WorldSession(r, this);
                    worldSessions.Add(r.name, ws);
                    subresources.Add(ws);
                }
            }
        }

        protected override void DeactivateImpl()
        {
            overWorld = null;
            worldSessions.Clear();
            WorldSession.map = new();
            RoomSession.map = new();
            OnlinePhysicalObject.map = new();
        }

        protected override void UnavailableImpl()
        {

        }

        public override string Id()
        {
            return "@overworld";
        }

        public override ushort ShortId() => (ushort)super.subresources.IndexOf(this);
        public override OnlineResource SubresourceFromShortId(ushort shortId)
        {
            return worldSessions.Select(x => x.Value).FirstOrDefault(x => x.region.regionNumber == shortId);
        }

        protected override ResourceState MakeState(uint ts)
        {
            return new OverworldState(this, ts);
        }

        public class OverworldState : ResourceWithSubresourcesState
        {
            public OverworldState() : base() { }
            public OverworldState(OverworldSession resource, uint ts) : base(resource, ts) { }
        }
    }
}
