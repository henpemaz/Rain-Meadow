using System;

namespace RainMeadow
{
    public abstract class OnlineState : Generics.IDelta<OnlineState>
    {
        public OnlinePlayer from; // not serialized, message source
        public uint tick; // not serialized, latest from player when read

        protected OnlineState() { }

        protected OnlineState(uint ts)
        {
            this.from = OnlineManager.mePlayer;
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
            RealizedOverseerState,
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
                    s = new EntityFeedState();
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
                case StateType.RealizedOverseerState:
                    s = new RealizedOverseerState();
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
            serializer.IsDelta = _isDelta; // Serializer wraps this call and restores the previous value later (override-proof)
            if (_isDelta) { serializer.Serialize(ref DeltaFromTick); }
        }

        public virtual bool SupportsDelta => false;
        public bool IsDelta { get => _isDelta; set => _isDelta = value; }

        private bool _isDelta;
        public uint DeltaFromTick;

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