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
        public OnlineManager manager;
        public Lobby lobby;
        public OnlinePlayer me;
        public OnlineSessionJoinType joinType;
        public Dictionary<Region, WorldSession> worldSessions = new();
        public SaveState saveState;
        public int playerCharacter;

        public OnlineSession(RainWorldGame game) : base(game){
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
            playerCharacter = 0;
            saveState = new SaveState(-1, game.rainWorld.progression);
        }

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

        internal void Waiting()
        {
            throw new NotImplementedException();
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
