using System;

namespace RainMeadow
{
    public abstract class OnlineState : IPrimaryDelta<OnlineState>
    {
        public OnlinePlayer from; // not serialized, message source
        public ulong tick; // not serialized, latest from player when read

        protected OnlineState() { }

        protected OnlineState(ulong ts)
        {
            this.tick = ts;
        }

        public abstract StateType stateType { get; } // serialized externally

        public virtual long EstimatedSize => 1;

        public enum StateType : byte
        {
            Unknown = 0,
            LobbyState,
            WorldState,
            RoomState,
            EntityInResourceState,
            PhysicalObjectEntityState,
            AbstractCreatureState,
            RealizedPhysicalObjectState,
            RealizedCreatureState,
            RealizedPlayerState,
            RealizedWeaponState,
            RealizedSpearState,
            CreatureStateState,
            CreatureHealthState,
        }

        public static OnlineState NewFromType(StateType stateType)
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
                case StateType.EntityInResourceState:
                    s = new EntityInResourceState();
                    break;
                case StateType.PhysicalObjectEntityState:
                    s = new PhysicalObjectEntityState();
                    break;
                case StateType.AbstractCreatureState:
                    s = new AbstractCreatureState();
                    break;
                case StateType.RealizedPhysicalObjectState:
                    s = new RealizedPhysicalObjectState();
                    break;
                case StateType.RealizedCreatureState:
                    s = new RealizedCreatureState();
                    break;
                case StateType.RealizedPlayerState:
                    s = new RealizedPlayerState();
                    break;
                case StateType.RealizedWeaponState:
                    s = new RealizedWeaponState();
                    break;
                case StateType.RealizedSpearState:
                    s = new RealizedSpearState();
                    break;
                case StateType.CreatureStateState:
                    s = new CreatureStateState();
                    break;
                case StateType.CreatureHealthState:
                    s = new CreatureHealthStateState();
                    break;
                default:
                    break;
            }
            if (s is null) throw new InvalidOperationException("invalid state type");
            return s;
        }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref _isDelta);
            serializer.IsDelta = IsDelta;
        }

        public bool IsDelta { get => _isDelta; set => _isDelta = value; }
        bool _isDelta;


        public virtual OnlineState Delta(OnlineState lastAcknoledgedState)
        {
            return this;
        }

        public virtual OnlineState ApplyDelta(OnlineState newState)
        {
            return newState;
        }
    }
}