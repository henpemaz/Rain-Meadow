using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RainMeadow
{
    static class OnlineExtensions
    {
        public static OnlineSession getOnlineSession(this RainWorldGame self)
        {
            return self.session as OnlineSession;
        }
        public static bool isOnlineSession(this RainWorldGame self)
        {
            return self.session is OnlineSession;
        }
    }

    // OnlineSession is tightly coupled to a lobby, and the highest ownership level
    public class OnlineSession : GameSession
    {
        public OnlineSession(RainWorldGame game) : base(game){ }

        public class EnumExt_OnlineSession
        {
            public static ProcessManager.MenuSetup.StoryGameInitCondition Online;
        }

        public enum OnlineSessionJoinType
        {
            None = 0,
            Host,
            Sync,
            Late
        }

        public void Update()
        {
            
        }

        public Lobby lobby;

        public OnlinePlayer me;

        public List<WorldSession> worldSessions;
    }

    // SubSessions are transferible sessions, limited to a resource that others can consume (world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    public abstract class SubSession
    {
        public OnlinePlayer owner;
        public bool loaded;
    }

    public class WorldSession : SubSession
    {
        public Region region;

        public World world;

        public List<RoomSession> rooms;
    }

    public class RoomSession : SubSession
    {
        public AbstractRoom absroom;

        public Room room;

        public List<NwEntity> entities;
    }


    public class NwEntity // :SubSession but renamed?
    {
        public OnlinePlayer owner;
        public bool loaded;
    }
}
