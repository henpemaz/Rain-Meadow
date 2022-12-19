using System;
using System.Collections.Generic;

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
    class OnlineSession : GameSession
    {
        public OnlineSession(RainWorldGame game) : base(game){ }

        public class EnumExt_OnlineSession
        {
            public static ProcessManager.MenuSetup.StoryGameInitCondition Online;
        }

        public static void Apply()
        {

        }

        public Lobby lobby;

        public OnlinePlayer me;

        public List<WorldSession> worldSessions;
    }

    // SubSessions are transferible sessions, limited to a resource that others can consume (world, room)
    // The owner of the resource coordinates states, distributes subresources and solves conflicts
    abstract class SubSession
    {
        OnlinePlayer owner;
        bool loaded;
    }

    class WorldSession : SubSession
    {
        Region region;

        World world;

        List<RoomSession> rooms;
    }

    class RoomSession : SubSession
    {
        AbstractRoom absroom;

        Room room;

        List<NwEntity> entities;
    }


    class NwEntity // :SubSession but renamed?
    {
        OnlinePlayer owner;
        bool loaded;
    }
}
