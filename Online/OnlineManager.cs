using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // Static/singleton class for online features and callbacks
    // is a mainloopprocess so update bound to game update? worth it? idk
    public class OnlineManager : MainLoopProcess
    {
        public static OnlineManager instance;
        public static Serializer serializer = new Serializer(16000);
        public static List<ResourceSubscription> subscriptions;
        public static List<EntityFeed> feeds;
        public static Dictionary<OnlineEntity.EntityId, OnlineEntity> recentEntities;
        public static HashSet<OnlineEvent> waitingEvents;
        public static float lastDt;
        public static OnlinePlayer mePlayer;
        public static List<OnlinePlayer> players;
        public static Lobby lobby;

        public static uint tickOffsetDefault = 2; //each unit is 50ms of added latency
        public static uint tickOffset;
        public static uint deltaTick;
        public static uint lastDeltaTick;
        public static uint lastStateTimestamp;
        public static Queue<QueuedState> QueuedStates;
        public class QueuedState
        {
            public OnlineState state;
            public uint SendWhen;

            public bool timeToSend => mePlayer.tick > SendWhen;

            public QueuedState(OnlineState state, uint delay)
            {
                this.state = state;
                this.SendWhen = mePlayer.tick + delay;
            }
        }

        public OnlineManager(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.OnlineManager)
        {
            instance = this;
            framesPerSecond = 20; // alternatively, run as fast as we can for the receiving stuff, but send on a lower tickrate?

            MatchmakingManager.InitLobbyManager();
            Reset();
            RainMeadow.Debug("OnlineManager Created");
        }

        public static void Reset()
        {
            subscriptions = new();
            feeds = new();
            recentEntities = new();
            waitingEvents = new(4);
            QueuedStates = new Queue<QueuedState>();
            tickOffset = tickOffsetDefault;

            WorldSession.map = new();
            RoomSession.map = new();
            OnlinePhysicalObject.map = new();

            lobby = null;
            mePlayer = new OnlinePlayer(mePlayer.id) { isMe = true };
            players = new List<OnlinePlayer>() { mePlayer };
        }

        public override void Update()
        {
            base.Update();

            NetTick();

            while (QueuedStates.Count > 0 && QueuedStates.Peek().timeToSend)
            {
                ProcessState(QueuedStates.Dequeue().state);
            }
        }

        // from a force-load situation
        public static void ForceLoadUpdate()
        {
            if (UnityEngine.Time.realtimeSinceStartup > lastDt + 1f / instance.framesPerSecond)
            {
#if !LOCAL_P2P
             SteamAPI.RunCallbacks();
#endif
             NetTick();

            }
        }

        public static void NetTick()
        {
            // Incoming messages
            NetIO.Update();

            if (lobby != null)
            {
                mePlayer.tick++;
                ProcessSelfEvents();
                
                // Prepare outgoing messages
                foreach (var subscription in subscriptions)
                {
                    subscription.Update(mePlayer.tick);
                }
                
                foreach (var feed in feeds)
                {
                    feed.Update(mePlayer.tick);
                }
                
                // Outgoing messages
                foreach (var player in players)
                {
                    SendData(player);
                }
            }

            lastDt = UnityEngine.Time.realtimeSinceStartup;
        }

        public static void SendData(OnlinePlayer toPlayer)
        {
            if (toPlayer.isMe)
                return;

            if (toPlayer.needsAck || toPlayer.OutgoingEvents.Any() || toPlayer.OutgoingStates.Any())
            {
                NetIO.SendSessionData(toPlayer);
            }
        }

        public static void ProcessSelfEvents()
        {
            // Stuff mePlayer set to itself, events from the distributed lease system
            while (mePlayer.OutgoingEvents.Count > 0)
            {
                try
                {
                    mePlayer.OutgoingEvents.Dequeue().Process();
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                }
            }
        }

        public static void ProcessIncomingEvent(OnlineEvent onlineEvent)
        {
            OnlinePlayer fromPlayer = onlineEvent.from;
            fromPlayer.needsAck = true;
            if (NetIO.IsNewer(onlineEvent.eventId, fromPlayer.lastEventFromRemote))
            {
                RainMeadow.Debug($"New event {onlineEvent} from {fromPlayer}, processing...");
                fromPlayer.lastEventFromRemote = onlineEvent.eventId;
                if (onlineEvent.CanBeProcessed())
                {
                    try
                    {
                        onlineEvent.Process();
                    }
                    catch (Exception e)
                    {
                        RainMeadow.Error(e);
                    }
                    MaybeProcessWaitingEvents();
                }
                else if (!onlineEvent.ShouldBeDiscarded())
                {
                    waitingEvents.Add(onlineEvent);
                }
            }
            //else { RainMeadow.Debug($"Stale event {onlineEvent} from {fromPlayer}"); }
        }

        public static void MaybeProcessWaitingEvents()
        {
            if (waitingEvents.Count > 0)
            {
                waitingEvents.RemoveWhere(ev => ev.ShouldBeDiscarded());
                while (waitingEvents.FirstOrDefault(ev => ev.CanBeProcessed()) is OnlineEvent ev)
                {
                    try
                    {
                        ev.Process();
                    }
                    catch (Exception e)
                    {
                        RainMeadow.Error(e);
                    }
                    waitingEvents.Remove(ev);
                }
            }
        }

        public static void resetOffset()
        {
            QueuedStates.Clear();
            tickOffset = tickOffsetDefault;
        }
        public static void ProcessIncomingState(OnlineState state)
        {
            OnlinePlayer fromPlayer = state.from;
            deltaTick = fromPlayer.tick - fromPlayer.lastAckdTick;

            if (tickOffset < 0) //doing this would require a time machine
            {
                ProcessState(state);
                resetOffset();
            }
            if (lastStateTimestamp + tickOffset < mePlayer.tick) //this should've already been updated
            {
                ProcessState(state);
            }
            else
                QueuedStates.Enqueue(new QueuedState(state, tickOffset));
            
            tickOffset += deltaTick - lastDeltaTick;
            lastDeltaTick = deltaTick;
            lastStateTimestamp = mePlayer.tick;
            
            if (tickOffset > 20) //we're falling behind
            {
                ProcessState(state);
                resetOffset();
            }
        }

        public static void ProcessState(OnlineState state)
        {
            try
            {
                if (state is OnlineResource.ResourceState resourceState && resourceState.resource != null && (resourceState.resource.isAvailable || resourceState.resource.isWaitingForState))
                {
                    resourceState.resource.ReadState(resourceState);
                }
                if (state is EntityFeedState entityInResourceState && entityInResourceState.inResource != null && entityInResourceState.inResource.isAvailable)
                {
                    entityInResourceState.entityState.entityId.FindEntity()?.ReadState(entityInResourceState.entityState, entityInResourceState.inResource);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
            }
        }

        public static void AddSubscription(OnlineResource onlineResource, OnlinePlayer player)
        {
            subscriptions.Add(new ResourceSubscription(onlineResource, player));
        }

        public static void RemoveSubscription(OnlineResource onlineResource, OnlinePlayer player)
        {
            subscriptions.RemoveAll(s => s.resource == onlineResource && s.player == player);
        }

        public static void RemoveSubscriptions(OnlineResource onlineResource)
        {
            subscriptions.RemoveAll(s => s.resource == onlineResource);
        }

        public static void AddFeed(OnlineResource resource, OnlineEntity oe)
        {
            feeds.Add(new EntityFeed(resource, oe));
        }

        public static void RemoveFeed(OnlineResource resource, OnlineEntity oe)
        {
            feeds.RemoveAll(f => f.resource == resource && f.entity == oe);
        }

        public static void RemoveFeeds(OnlineResource resource)
        {
            feeds.RemoveAll(f => f.resource == resource);
        }

        // this smells
        public static OnlineResource ResourceFromIdentifier(string rid)
        {
            if (lobby != null)
            {
                if (rid == ".") return lobby;
                if (rid.Length == 2 && lobby.worldSessions.TryGetValue(rid, out var r)) return r;
                if (rid.Length > 2 && lobby.worldSessions.TryGetValue(rid.Substring(0, 2), out var r2) && r2.roomSessions.TryGetValue(rid.Substring(2), out var room)) return room;
            }
            RainMeadow.Error("resource not found : " + rid);
            return null;
        }
    }
}
