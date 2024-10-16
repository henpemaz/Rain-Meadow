using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public Queue<OnlineStateMessage> OutgoingStates = new(16);

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

        [Conditional("TRACING")]
        public void TraceOutgoingState(OnlineStateMessage stateMessage)
        {
            if (RainMeadow.tracing)
            {
                switch (stateMessage.state)
                {
                    case EntityFeedState entityFeedState:
                        RainMeadow.Trace($"{entityFeedState}:{entityFeedState.entityState.ID} for {this}");
                        break;
                    case OnlineResource.ResourceState resourceState:
                        RainMeadow.Trace($"{resourceState}:{resourceState.resource.Id()} for {this}");
                        break;
                    default:
                        RainMeadow.Trace($"{stateMessage.state} for {this}");
                        break;
                }
            }
        }

        public OnlineStateMessage QueueStateMessage(OnlineStateMessage stateMessage)
        {
            TraceOutgoingState(stateMessage);
            OutgoingStates.Enqueue(stateMessage);
            return stateMessage;
        }

        public OnlineEvent GetRecentEvent(ushort id)
        {
            return recentlyAckedEvents.FirstOrDefault(e => e.eventId == id) ?? abortedEvents.FirstOrDefault(e => e.eventId == id);
        }

        internal void NewTick(uint newTick)
        {
            tick = newTick;
            recentTicks.Enqueue(tick);
            var windowstart = tick - 15;
            while (NetIO.IsNewer(windowstart, recentTicks.Peek())) recentTicks.Dequeue();
            recentTicksToAckBitpack = recentTicks.Select(t => (int)(uint)(tick - t)).Aggregate((ushort)0, (s, e) => (ushort)(s | (ushort)(1 << e)));
            needsAck = true;
            RainMeadow.Trace(this + " - " + tick);
            RainMeadow.Trace(Convert.ToString(recentTicksToAckBitpack, 2).PadLeft(16, '0'));
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
            var timeSinceLastTick = (int)Math.Floor(Math.Max(1, (UnityEngine.Time.realtimeSinceStartup - OnlineManager.lastReceive) * 1000));
            ping = (int)(OnlineManager.mePlayer.tick - tickAck) * OnlineManager.instance.milisecondsPerFrame + timeSinceLastTick;

            if (NetIO.IsNewerOrEqual(tickAck, latestTickAck) && (recentTickAcks & 1) == 1)
            {
                //RainMeadow.Debug(tickAck);
                //RainMeadow.Debug(Convert.ToString(recentTickAcks, 2));
                this.latestTickAck = tickAck;
                this.oldestTickToConsider = tickAck - 64;
                recentlyAckdTicks.RemoveWhere(t => NetIO.IsNewer(oldestTickToConsider, t)); // keep a bigger window from previous acks
                for (int i = 0; i < 16; i++)
                {
                    if ((recentTickAcks & (1 << i)) != 0)
                    {
                        recentlyAckdTicks.Add(tickAck - (uint)i);
                    }
                }
                while (!recentlyAckdTicks.Contains(oldestTickToConsider)) oldestTickToConsider++;
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

        internal RPCEvent InvokeRPC(Delegate del, params object[] args)
        {
            return (RPCEvent)this.QueueEvent(RPCManager.BuildRPC(del, args));
        }

        internal RPCEvent InvokeOnceRPC(Delegate del, params object[] args)
        {
            foreach (var e in OutgoingEvents)
                if (e is RPCEvent rpc && rpc.IsIdentical(del, args))
                    return rpc;

            if (isMe)
            {
                del.Method.Invoke(del.Target, args);
                return null;
            }

            return (RPCEvent)this.QueueEvent(RPCManager.BuildRPC(del, args));
        }

        internal void Update()
        {
            // Update snapshot cycle for debug overlay and reset for snapshot frame
            var nextSnapshotIndex = (bytesSnapIndex + 1) % 40;
            bytesIn[nextSnapshotIndex] = 0;
            bytesOut[nextSnapshotIndex] = 0;
            bytesSnapIndex = nextSnapshotIndex;
        }
        internal TickReference MakeTickReference()
        {
            return new TickReference(this);
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
