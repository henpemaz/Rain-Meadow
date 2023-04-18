using Steamworks;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    // Static/singleton class for online features and callbacks
    // is a mainloopprocess so update bound to game update? worth it? idk
    public class OnlineManager : MainLoopProcess {

        public static string CLIENT_KEY = "client";
        public static string CLIENT_VAL = "Meadow_" + RainMeadow.MeadowVersionStr;
        public static string NAME_KEY = "name";
        public static OnlineManager instance;
        public static Serializer serializer = new Serializer(16000);

        public static Lobby lobby;
        public static List<Subscription> subscriptions = new();
        public static List<EntityFeed> feeds = new();
        public static Dictionary<OnlineEntity.EntityId, OnlineEntity> recentEntities = new();

        public OnlineManager(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.OnlineManager)
        {
            instance = this;
            PlayersManager.me = SteamUser.GetSteamID();
            PlayersManager.mePlayer = new OnlinePlayer(PlayersManager.me) { isMe = true, name = SteamFriends.GetPersonaName() };

            framesPerSecond = 5; // alternatively, run as fast as we can for the receiving stuff, but send on a lower tickrate?
            PlayersManager.
                        players = new List<OnlinePlayer>() { PlayersManager.mePlayer };

            RainMeadow.Debug("OnlineManager Created");
        }

        public override void Update()
        {
            base.Update();
            if(lobby != null)
            {
                PlayersManager.mePlayer.tick++;
                ProcessSelfEvents();
                
                // Incoming messages
                serializer.ReceiveData();

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
                    serializer.SendData(player);
                }
            }
        }

        public static void ProcessSelfEvents()
        {
            // Stuff mePlayer set to itself, events from the distributed lease system
            while (PlayersManager.mePlayer.OutgoingEvents.Count > 0)
            {
                PlayersManager.mePlayer.OutgoingEvents.Dequeue().Process();
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
                try
                {
                    onlineEvent.Process();
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                }
            }
        }

        public static void ProcessIncomingState(OnlineState state)
        {
            OnlinePlayer fromPlayer = state.from;
            try
            {
                if (state is OnlineResource.ResourceState resourceState)
                {
                    resourceState.resource.ReadState(resourceState, fromPlayer.tick);
                }
                if (state is OnlineEntity.EntityState entityState)
                {
                    entityState.onlineEntity.ReadState(entityState, fromPlayer.tick);
                }
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
            }
        }

        public static void AddSubscription(OnlineResource onlineResource, OnlinePlayer player)
        {
            subscriptions.Add(new Subscription(onlineResource, player));
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
