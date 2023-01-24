using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow
{
    // OnlineSession is tightly coupled to a lobby, and the highest ownership level
    public class OnlineSession : StoryGameSession
    {
        public OnlineManager manager;
        public Lobby lobby;
        public OnlinePlayer me;
        public OnlineSessionJoinType joinType;
        public Dictionary<Region, WorldSession> worldSessions = new();

        public OnlineSession(RainWorldGame game) : base(RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer, game)
        {
            RainMeadow.sLogger.LogInfo("OnlineSession created");
            manager = OnlineManager.instance;
            lobby = manager.lobby;
            me = manager.me;
            if(lobby.owner == me)
            {
                joinType = OnlineSessionJoinType.Host;
            }
            else
            {
                joinType = OnlineSessionJoinType.Sync;
            }
        }

        public enum OnlineSessionJoinType
        {
            None = 0,
            Host,
            Sync,
            Late
        }

        internal bool ShouldWorldLoadCreatures(RainWorldGame game)
        {
            return false;
        }
    }

    // SubSessions are transferible sessions, limited to a resource that others can consume (world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract class SubSession
    {
        public OnlinePlayer owner;
        public OnlineSession session;
        public bool pendingOwnership;

        protected SubSession(OnlineSession session, OnlinePlayer owner)
        {
            this.session = session;
            this.owner = owner;
        }

        public void RequestOwnership()
        {
            if (owner == session.me) return;
            pendingOwnership = true;
        }
    }

    public class WorldSession : SubSession
    {
        public Region region;
        public World world;
        public List<RoomSession> rooms;

        public WorldSession(OnlineSession session, OnlinePlayer owner, Region region) : base(session,owner)
        {
            this.region = region;
        }
    }

    public class RoomSession : SubSession
    {
        public AbstractRoom absroom;

        public Room room;

        public List<NwEntity> entities;

        public RoomSession(OnlineSession session, OnlinePlayer owner, AbstractRoom absroom) : base(session, owner)
        {
            this.absroom = absroom;
        }
    }

    public class NwEntity // :SubSession but renamed?
    {
        public OnlinePlayer owner;
        public bool loaded;
    }
}
