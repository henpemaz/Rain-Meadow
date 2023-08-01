using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public partial class OnlinePlayer : IEquatable<OnlinePlayer>
    {
        public MeadowPlayerId id; // big id for matchmaking
        public ushort inLobbyId; // small id in lobby serialization

        public Queue<OnlineEvent> OutgoingEvents = new(8);
        public List<OnlineEvent> recentlyAckedEvents = new(4);
        public List<OnlineEvent> abortedEvents = new(8);
        public Queue<OnlineState> OutgoingStates = new(16);

        private ushort nextOutgoingEvent = 1;
        public ushort lastEventFromRemote; // the last event I've received from them, I'll write it back on headers as an ack
        public ushort lastAckFromRemote; // the last event they've ack'd to me, used imediately on receive
        public uint tick; // the last tick I've received from them, I'll write it back on headers as an ack
        public uint lastAckdTick; // the last tick they've ack'd to me
        public bool needsAck;
        public int ping; // rtt

        public bool isMe;
        public bool hasLeft;


        public OnlinePlayer(MeadowPlayerId id)
        {
            this.id = id;
        }

        public OnlineEvent QueueEvent(OnlineEvent e)
        {
            e.eventId = this.nextOutgoingEvent;
            e.to = this;
            e.from = OnlineManager.mePlayer;
            RainMeadow.Debug($"{e} for {this}");
            nextOutgoingEvent++;
            OutgoingEvents.Enqueue(e);
            return e;
        }

        public OnlineEvent GetRecentEvent(ushort id)
        {
            return recentlyAckedEvents.FirstOrDefault(e => e.eventId == id) ?? abortedEvents.FirstOrDefault(e => e.eventId == id);
        }

        public void EventAckFromRemote(ushort lastAck)
        {
            this.recentlyAckedEvents.Clear();
            this.lastAckFromRemote = lastAck;
            while (OutgoingEvents.Count > 0 && NetIO.IsNewerOrEqual(lastAck, OutgoingEvents.Peek().eventId))
            {
                var e = OutgoingEvents.Dequeue();
                RainMeadow.Debug($"{this} ackd {e}");
                recentlyAckedEvents.Add(e);
            }
        }

        public void TickAckFromRemote(uint lastTick)
        {
            DebugOverlay.playersRead.addPlayer(this);

            var timeSinceLastTick = (int)Math.Floor(Math.Max(1, (UnityEngine.Time.realtimeSinceStartup - OnlineManager.lastUpdate) * 1000));
            ping = (int)(OnlineManager.mePlayer.tick - lastTick) * 50 + timeSinceLastTick;

            this.lastAckdTick = lastTick;
        }

        public bool HasUnacknoledgedEvents()
        {
            return OutgoingEvents.Count > 0;
        }

        public void AbortUnacknoledgedEvents()
        {
            if (OutgoingEvents.Count > 0)
            {
                RainMeadow.Debug($"Aborting events for player {this}");
                var toBeAborted = new Queue<OnlineEvent>(OutgoingEvents); // newly added events are not aborted on purpose
                OutgoingEvents.Clear();
                while (toBeAborted.Count > 0)
                {
                    var e = toBeAborted.Dequeue();
                    RainMeadow.Debug($"Aborting: {e}");
                    e.Abort();
                    abortedEvents.Add(e);
                }
            }
        }

        public override string ToString()
        {
            return $"{inLobbyId}:{id}";
        }

        // IEqu
        public override bool Equals(object obj) => this.Equals(obj as OnlinePlayer);
        public bool Equals(OnlinePlayer other)
        {
            return other != null && id == other.id;
        }
        public override int GetHashCode() => id.GetHashCode();

        public static bool operator ==(OnlinePlayer lhs, OnlinePlayer rhs)
        {
            return lhs is null ? rhs is null : lhs.Equals(rhs);
        }
        public static bool operator !=(OnlinePlayer lhs, OnlinePlayer rhs) => !(lhs == rhs);
    }
}
