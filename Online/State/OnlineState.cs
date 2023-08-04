using System;

namespace RainMeadow
{
    public abstract class OnlineState
    {
        protected OnlineState() { }

        public abstract StateType stateType { get; } // serialized externally

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

        public abstract void CustomSerialize(Serializer serializer);

        public abstract long EstimatedSize(Serializer serializer);
    }
}