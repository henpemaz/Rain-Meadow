using System;

namespace RainMeadow
{
    public abstract class OnlineState
    {
        public OnlinePlayer fromPlayer; // not serialized, message source
        public ulong ts; // not serialized, latest from player when read

        protected OnlineState() { }

        protected OnlineState(ulong ts)
        {
            this.ts = ts;
        }

        public abstract StateType stateType { get; } // serialized externally

        public virtual long EstimatedSize => 1;

        public enum StateType : byte
        {
            Unknown = 0,
            LobbyState,
            WorldState,
            RoomState,
            PhysicalObjectEntityState,
            CreatureEntityState,
            RealizedObjectState,
            RealizedCreatureState,
            RealizedPlayerState,
        }

        internal static OnlineState NewFromType(StateType stateType)
        {
            OnlineState s = null;
            switch (stateType)
            {
                case StateType.Unknown:
                    break;
                case StateType.LobbyState:
                    s = new Lobby.LobbyState();
                    break;
                case StateType.WorldState:
                    s = new WorldSession.WorldState();
                    break;
                case StateType.RoomState:
                    s = new RoomSession.RoomState();
                    break;
                case StateType.PhysicalObjectEntityState:
                    s = new OnlineEntity.PhysicalObjectEntityState();
                    break;
                case StateType.CreatureEntityState:
                    s = new OnlineEntity.CreatureEntityState();
                    break;
                case StateType.RealizedObjectState:
                    s = new OnlineEntity.RealizedObjectState();
                    break;
                case StateType.RealizedCreatureState:
                    s = new OnlineEntity.RealizedCreatureState();
                    break;
                case StateType.RealizedPlayerState:
                    s = new OnlineEntity.RealizedPlayerState();
                    break;
                default:
                    break;
            }
            if (s is null) throw new InvalidOperationException("invalid state type");
            return s;
        }

        public virtual void CustomSerialize(Serializer serializer)
        {
            // no op
        }

        internal virtual OnlineState Delta(OnlineState lastAcknoledgedState)
        {
            return this;
        }

        internal virtual OnlineState ApplyDelta(OnlineState newState)
        {
            return newState;
        }
    }
}