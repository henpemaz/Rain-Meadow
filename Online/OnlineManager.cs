using MonoMod;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // Static/singleton class for online features and callbacks
    // is a mainloopprocess so update bound to game update? worth it? idk
    public class OnlineManager : MainLoopProcess {

        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow_" + RainMeadow.MeadowVersionStr;
        public static string NAME_KEY = "name";
        public static string MODE_KEY = "mode";
        public static OnlineManager instance;
        public static Serializer serializer = new Serializer(16000);

        public static Lobby lobby;
        public static List<ResourceSubscription> subscriptions;
        public static List<EntityFeed> feeds;
        public static Dictionary<OnlineEntity.EntityId, OnlineEntity> recentEntities;
        public static HashSet<OnlineEvent> waitingEvents;
        public static float lastDt;

        public OnlineManager(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.OnlineManager)
        {
            instance = this;
            framesPerSecond = 20; // alternatively, run as fast as we can for the receiving stuff, but send on a lower tickrate?
            Reset();

            RainMeadow.Debug("OnlineManager Created");
        }

        public static void Reset()
        {
            lobby = null;
            subscriptions = new();
            feeds = new();
            recentEntities = new();
            waitingEvents = new(4);

            WorldSession.map = new();
            RoomSession.map = new();
            OnlinePhysicalObject.map = new();

            PlayersManager.Reset();
        }

        public override void Update()
        {
            base.Update();

            // Incoming messages
            NetIO.Update();

            if (lobby != null)
            {
                PlayersManager.mePlayer.tick++;
                ProcessSelfEvents();

                // Prepare outgoing messages
                foreach (var subscription in subscriptions)
                {
                    subscription.Update(PlayersManager.mePlayer.tick);
                }

                foreach (var feed in feeds)
                {
                    feed.Update(PlayersManager.mePlayer.tick);
                }

                // Outgoing messages
                foreach (var player in PlayersManager.players)
                {
                    if (player.isMe)
                        continue;
                    
                    SendData(player);
                }
            }

            lastDt = UnityEngine.Time.realtimeSinceStartup;
        }

        public static void SendData(OnlinePlayer toPlayer)
        {
            if (toPlayer.needsAck || toPlayer.OutgoingEvents.Any() || toPlayer.OutgoingStates.Any())
            {
                serializer.SendData(toPlayer);
            }
        }

        // from a force-load situation
        public static void TickEvents()
        {
            SteamAPI.RunCallbacks();
            // Incoming messages

            NetIO.Update();

            if (lobby != null)
            {
                if(UnityEngine.Time.realtimeSinceStartup > lastDt + 1f/instance.framesPerSecond)
                {
                    PlayersManager.mePlayer.tick++;

                    // Local messages
                    ProcessSelfEvents();

                    // no state

                    // Outgoing messages
                    foreach (var player in PlayersManager.players)
                    {
                        SendData(player);
                    }
                    lastDt = UnityEngine.Time.realtimeSinceStartup;
                }
            }
        }

        public static void ProcessSelfEvents()
        {
            // Stuff mePlayer set to itself, events from the distributed lease system
            while (PlayersManager.mePlayer.OutgoingEvents.Count > 0)
            {
                try
                {
                    PlayersManager.mePlayer.OutgoingEvents.Dequeue().Process();
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                }
            }
        }

        public static bool IsNewer(ulong eventId, ulong lastIncomingEvent)
        {
            var delta = eventId - lastIncomingEvent;
            return delta != 0 && delta < ulong.MaxValue / 2;
        }

        public static bool IsNewerOrEqual(ulong eventId, ulong lastIncomingEvent)
        {
            var delta = eventId - lastIncomingEvent;
            return delta < ulong.MaxValue / 2;
        }

        public static void ProcessIncomingEvent(OnlineEvent onlineEvent)
        {
            OnlinePlayer fromPlayer = onlineEvent.from;
            fromPlayer.needsAck = true;
            if (IsNewer(onlineEvent.eventId, fromPlayer.lastEventFromRemote))
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
            if(waitingEvents.Count > 0)
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
            OnlinePlayer fromPlayer = state.from;
            try
            {
                if (state is OnlineResource.ResourceState resourceState && resourceState.resource != null && resourceState.resource.isActive)
                {
                    resourceState.resource.ReadState(resourceState);
                }
                if (state is EntityInResourceState entityInResourceState)
                {
                    entityInResourceState.entityState.onlineEntity.ReadState(entityInResourceState.entityState, entityInResourceState.inResource);
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
            if (rid == ".") return lobby;
            if (rid.Length == 2 && lobby.worldSessions.TryGetValue(rid, out var r)) return r;
            if (rid.Length > 2 && lobby.worldSessions.TryGetValue(rid.Substring(0, 2), out var r2) && r2.roomSessions.TryGetValue(rid.Substring(2), out var room)) return room;

            RainMeadow.Error("resource not found : " + rid);
            return null;
        }
    }
}
