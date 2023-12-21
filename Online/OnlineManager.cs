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
        public static float lastUpdate;
        public static OnlinePlayer mePlayer;
        public static List<OnlinePlayer> players;
        public static Lobby lobby;
        public static LobbyInfo currentlyJoiningLobby;

        public OnlineManager(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.OnlineManager)
        {
            instance = this;
            framesPerSecond = 20; // alternatively, run as fast as we can for the receiving stuff, but send on a lower tickrate?

            MatchmakingManager.InitLobbyManager();
            Reset();
            MatchmakingManager.instance.OnLobbyJoined += OnlineManager_OnLobbyJoined;
            RainMeadow.Debug("OnlineManager Created");
        }
        
        private void OnlineManager_OnLobbyJoined(bool ok, string error)
        {
            RainMeadow.Debug(ok);
            currentlyJoiningLobby = default;
            if (ok)
            {
                manager.RequestMainProcessSwitch(lobby.gameMode.MenuProcessId());
            }
            else
            {
                MatchmakingManager.instance.LeaveLobby();
            }
        }

        public static void Reset()
        {
            subscriptions = new();
            feeds = new();
            recentEntities = new();
            waitingEvents = new(4);

            WorldSession.map = new();
            RoomSession.map = new();
            OnlinePhysicalObject.map = new();

            lobby = null;
            mePlayer = new OnlinePlayer(mePlayer.id) { isMe = true };
            players = new List<OnlinePlayer>() { mePlayer };
        }

        public override void RawUpdate(float dt)
        {
            myTimeStacker += dt * (float)framesPerSecond;
            NetIO.Update(); // incoming data

            if (myTimeStacker >= 1f)
            {
                myTimeStacker -= 1f;
                if (myTimeStacker >= 1f)
                {
                    myTimeStacker = 0f;
                }
                Update(); // outgoing data
            }
            lastUpdate = UnityEngine.Time.realtimeSinceStartup;
        }

        // from a force-load situation
        public static void ForceLoadUpdate()
        {
#if !LOCAL_P2P
            SteamAPI.RunCallbacks();
#endif
            NetIO.Update();

            if (UnityEngine.Time.realtimeSinceStartup > lastDt + 1f / instance.framesPerSecond)
            {
                instance.Update();
                lastDt = UnityEngine.Time.realtimeSinceStartup;
            }
        }

        public override void Update()
        {
            if (lobby != null)
            {
                foreach (OnlinePlayer player in players)
                {
                    player.Updade();
                }

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

        public static void ProcessIncomingState(OnlineState state)
        {
            try
            {
                if (state is OnlineResource.ResourceState resourceState && resourceState.resource != null && (resourceState.resource.isAvailable || resourceState.resource.isWaitingForState))
                {
                    //RainMeadow.Debug($"Processing {resourceState} for {resourceState.resource}");
                    resourceState.resource.ReadState(resourceState);
                }
                else if (state is EntityFeedState entityFeedState && entityFeedState.inResource != null && entityFeedState.inResource.isAvailable)
                {
                    var ent = entityFeedState.entityState.entityId.FindEntity();
                    if(ent != null)
                    {
                        //RainMeadow.Debug($"Processing {entityFeedState} for {ent}");
                        ent.ReadState(entityFeedState);
                    }
                    else
                    {
                        RainMeadow.Error($"Entity {entityFeedState.entityState.entityId} not found for incoming state from {entityFeedState.entityState.from} in {entityFeedState.inResource}");
                    }
                }
                else
                {
                    RainMeadow.Error($"Unexpected incoming state: {state}");
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
                if (rid.StartsWith("arena")) // "arena" + room.name
                {
                    var ws = lobby.worldSessions.First().Value;

                    if (rid == "arena") return ws;

                    RainMeadow.Debug(string.Join(" | ", ws.roomSessions.Keys));

                    return ws.roomSessions[rid.Replace("arena", "")];
                }
            }
            RainMeadow.Error("resource not found : " + rid);
            return null;
        }
    }
}
