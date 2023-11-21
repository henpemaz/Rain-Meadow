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

        public ushort nextOutgoingEvent = 1; // outgoing, event id
        public ushort lastEventFromRemote; // incoming, the last event I've received from them, I'll write it back on headers as an ack
        public ushort lastAckFromRemote; // incoming, the last event they've ack'd to me, used imediately on receive
        public uint tick; // incoming, the latest tick I've received from them, I'll write it back on headers as an ack
        public Queue<uint> recentTicks = new(16); // incoming ticks
        public ushort recentTicksToAckBitpack; // outgoing, bitpack of recent ticks relative to tick, used for ack
        public uint latestTickAck; // incoming, the last tick they've ack'd to me
        public HashSet<uint> recentlyAckdTicks = new(); // incoming, recent ticks they've acked (from bitpack)
        public uint oldestTickToConsider; // incoming, from acked ticks the oldest to use for deltas

        public bool needsAck;

        public bool isMe;
        public bool hasLeft;


        // For Debug Overlay
        public int ping; // rtt
        public bool eventsWritten;
        public bool statesWritten;
        public bool eventsRead;
        public bool statesRead;
        public int bytesSnapIndex; // used to loop through the array and overwrite old data
        public readonly int[] bytesIn = new int[40];
        public readonly int[] bytesOut = new int[40];

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

        public void NewTick(uint newTick)
        {
            tick = newTick;
            if (recentTicks.Count >= 16) recentTicks.Dequeue();
            recentTicks.Enqueue(tick);
            recentTicksToAckBitpack = recentTicks.Select(t => (int)(tick - t)).Aggregate((ushort)0, (s, e) => (ushort)(s | (ushort)(1 << e)));
            needsAck = true;
            //RainMeadow.Debug(tick);
            //RainMeadow.Debug(Convert.ToString(recentTicksToAckBitpack, 2));
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

        public void TickAckFromRemote(uint tickAck, ushort recentTickAcks)
        {
            var timeSinceLastTick = (int)Math.Floor(Math.Max(1, (UnityEngine.Time.realtimeSinceStartup - OnlineManager.lastUpdate) * 1000));
            ping = (int)(OnlineManager.mePlayer.tick - tickAck) * 50 + timeSinceLastTick;

            if (NetIO.IsNewerOrEqual(tickAck, latestTickAck))
            {
                //RainMeadow.Debug(tickAck);
                //RainMeadow.Debug(Convert.ToString(recentTickAcks, 2));
                this.latestTickAck = tickAck;
                this.oldestTickToConsider = tickAck;
                recentlyAckdTicks = new();
                for (int i = 0; i < 16; i++)
                {
                    if ((recentTickAcks & (1 << i)) != 0)
                    {
                        recentlyAckdTicks.Add(tickAck - (uint)i);
                        oldestTickToConsider = tickAck - (uint)i;
                    }
                }
            }
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

        public RPCEvent InvokeRPC(Delegate del, params object[] args)
        {
            return (RPCEvent)this.QueueEvent(RPCManager.BuildRPC(del, args));
        }

        public void Updade()
        {
            // Update snapshot cycle for debug overlay and reset for snapshot frame
            var nextSnapshotIndex = (bytesSnapIndex + 1) % 40;
            bytesIn[nextSnapshotIndex] = 0;
            bytesOut[nextSnapshotIndex] = 0;
            bytesSnapIndex = nextSnapshotIndex;
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
