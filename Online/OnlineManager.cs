using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;

namespace RainMeadow
{
    // Static/singleton class for online features and callbacks
    // is a mainloopprocess so update bound to game update? worth it? idk
    public class OnlineManager : MainLoopProcess
    {
        public static OnlineManager instance;
        public static Serializer serializer = new Serializer(32000);
        public static List<ResourceSubscription> subscriptions;
        public static List<EntityFeed> feeds;
        public static Dictionary<OnlineEntity.EntityId, OnlineEntity> recentEntities;
        public static HashSet<OnlineEvent> waitingEvents;
        public static float lastSend;
        public static float lastReceive;
        public static OnlinePlayer mePlayer;
        public static List<OnlinePlayer> players;
        public static Lobby lobby;

        public static LobbyInfo currentlyJoiningLobby;

        public OnlineManager(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.OnlineManager)
        {
            instance = this;
            framesPerSecond = 20; // alternatively, run as fast as we can for the receiving stuff, but send on a lower tickrate?

            MatchmakingManager.InitLobbyManager();
            LeaveLobby();
            MatchmakingManager.instance.OnLobbyJoined += OnlineManager_OnLobbyJoined;
            RainMeadow.Debug("OnlineManager Created");
        }


        private void OnlineManager_OnLobbyJoined(bool ok, string error)
        {
            RainMeadow.Debug(ok);
            currentlyJoiningLobby = default;
            if (ok)
            {
                // manager.RequestMainProcessSwitch(lobby.gameMode.MenuProcessId());
            }
            else
            {
                OnlineManager.LeaveLobby();
            }
        }

        public static void LeaveLobby()
        {
            MatchmakingManager.instance.LeaveLobby();
            lobby = null;

            subscriptions = new();
            feeds = new();
            recentEntities = new();
            waitingEvents = new(4);

            WorldSession.map = new();
            RoomSession.map = new();
            OnlinePhysicalObject.map = new();

            RainMeadowModManager.Reset();

            mePlayer = new OnlinePlayer(mePlayer.id) { isMe = true };
            players = new List<OnlinePlayer>() { mePlayer };
        }

        public override void RawUpdate(float dt)
        {
            myTimeStacker += dt * (float)framesPerSecond;
            NetIO.Update(); // incoming data
            lastReceive = UnityEngine.Time.realtimeSinceStartup;

            if (myTimeStacker >= 1f)
            {
                myTimeStacker -= 1f;
                if (myTimeStacker >= 1f)
                {
                    myTimeStacker = 0f;
                }
                Update(); // outgoing data
            }
        }

        // from a force-load situation
        public static void ForceLoadUpdate()
        {
#if !LOCAL_P2P
            SteamAPI.RunCallbacks();
#endif
            NetIO.Update();
            lastReceive = UnityEngine.Time.realtimeSinceStartup;

            if (UnityEngine.Time.realtimeSinceStartup > lastSend + 1f / instance.framesPerSecond)
            {
                instance.Update();
            }
        }

        public override void Update()
        {
            if (lobby != null)
            {
                foreach (OnlinePlayer player in players)
                {
                    player.Update();
                }

                mePlayer.tick++;
                ProcessSelfEvents();

                if (lobby.isActive)
                {
                    lobby.Tick(mePlayer.tick);
                }

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

                lastSend = UnityEngine.Time.realtimeSinceStartup;
                RainMeadow.tracing = false; // tracing captures one tick
            }
        }

        public static void SendData(OnlinePlayer toPlayer)
        {
            if (toPlayer.isMe)
                return;

            if (toPlayer.needsAck || toPlayer.OutgoingEvents.Count > 0 || toPlayer.OutgoingStates.Count > 0)
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
                if (state is OnlineResource.ResourceState resourceState)
                {
                    if (resourceState.resource != null && (resourceState.resource.isAvailable || resourceState.resource.isWaitingForState || resourceState.resource.isPending))
                    {
                        RainMeadow.Trace($"Processing {resourceState} for {resourceState.resource}");
                        resourceState.resource.ReadState(resourceState);
                    }
                    else // resource unloaded or not available
                    {
                        RainMeadow.Trace($"Couldn't process {resourceState} for {resourceState.resource?.ToString() ?? "null"}");
                    }
                }
                else if (state is EntityFeedState entityFeedState)
                {
                    if (entityFeedState.inResource != null && entityFeedState.inResource.isAvailable)
                    {
                        var ent = entityFeedState.entityState.entityId.FindEntity();
                        if (ent != null)
                        {
                            RainMeadow.Trace($"Processing {entityFeedState} for {ent}");
                            ent.ReadState(entityFeedState);
                        }
                        else
                        {
                            RainMeadow.Error($"Entity {entityFeedState.entityState.entityId} not found for incoming state from {entityFeedState.entityState.from} in {entityFeedState.inResource}");
                        }
                    }
                    else // resource unloaded or not available
                    {
                        RainMeadow.Trace($"Couldn't process {entityFeedState} for {entityFeedState.inResource?.ToString() ?? "null"}");
                    }
                }
                else
                {
                    RainMeadow.Error($"Unexpected incoming state: {state}");
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error($"Error reading state {state}");
                if (state is OnlineResource.ResourceState resourceState && resourceState.resource != null && (resourceState.resource.isAvailable || resourceState.resource.isWaitingForState || resourceState.resource.isPending))
                {
                    RainMeadow.Error(resourceState.resource);
                }
                else if (state is EntityFeedState entityFeedState && entityFeedState.inResource != null && entityFeedState.inResource.isAvailable)
                {
                    var ent = entityFeedState.entityState.entityId.FindEntity();
                    RainMeadow.Error(entityFeedState.inResource);
                    RainMeadow.Error(entityFeedState.entityState);
                    RainMeadow.Error(entityFeedState.entityState.entityId);
                    RainMeadow.Error(ent);
                }
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

        internal static void QuitWithError(string v)
        {
            RainMeadow.Error(v);
            if (lobby != null && instance.manager.upcomingProcess != ProcessManager.ProcessID.MainMenu)
            {
                instance.manager.upcomingProcess = null;
                instance.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                instance.manager.ShowDialog(new Menu.DialogNotify(v, "Leaving Lobby", new Vector2(240, 320), instance.manager, () => { }));
                LeaveLobby();
                throw new Exception(v);
            }
        }
    }
}
