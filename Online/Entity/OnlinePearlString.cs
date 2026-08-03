

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public class OnlinePearlString : OnlineEntity
    {
        public class OnlinePearlStringDefinition : EntityDefinition
        {
            public enum PearlStringType : int
            {
                OutpostPearlString = 0,
                HangingPearlString
            };

            [OnlineField]
            public int stringtype;

            [OnlineField]
            public ushort pearlStringIndex;

            public OnlinePearlStringDefinition() { }

            public OnlinePearlStringDefinition(OnlinePearlString onlinePearlString, OnlineResource inResource) : base(onlinePearlString, inResource)
            {
                if (inResource is not RoomSession roomSession) throw new InvalidProgrammerException("add to roomsesions only");
                if (onlinePearlString.pearlString.room is Room room && room.abstractRoom == roomSession.absroom)
                {
                    switch (onlinePearlString.pearlString)
                    {
                        case ScavengerOutpost.PearlString outpostPearlString:
                            stringtype = (int)PearlStringType.OutpostPearlString;
                            pearlStringIndex = (ushort)room.updateList.OfType<ScavengerOutpost.PearlString>().ToList().IndexOf(outpostPearlString);
                            break;
                        case MoreSlugcats.HangingPearlString hangingPearlString:
                            stringtype = (int)PearlStringType.HangingPearlString;
                            pearlStringIndex = (ushort)room.updateList.OfType<MoreSlugcats.HangingPearlString>().ToList().IndexOf(hangingPearlString);
                            break;
                        default:
                            throw new InvalidProgrammerException("implemente this");
                    }
                }
                else
                {
                    throw new InvalidProgrammerException("Pearlstring not in room");
                }
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlinePearlString(this, inResource, (PearlStringState)initialState);
            }
        }

        public override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            remotedefinition = remotedefinition ?? new OnlinePearlStringDefinition(this, onlineResource);
            return remotedefinition;
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new PearlStringState(this, inResource, tick);
        }

        public static void InitializePearlString(UpdatableAndDeletable pearlString)
        {
            RainMeadow.DebugMe();
            if (pearlString.room?.abstractRoom?.GetResource() is not RoomSession rs) return;
            if (!rs.isActive || !rs.isOwner) return;
            if (pearlStringMap.TryGetValue(pearlString, out _)) return;
            var entityID = new EntityId(OnlineManager.mePlayer.inLobbyId, EntityId.IdType.uad, pearlString.room.world.game.GetNewID().number);
            var oe = new OnlinePearlString(pearlString, entityID, OnlineManager.mePlayer, true);
            RainMeadow.Debug(oe);
            oe.EnterResource(rs);
        }


        OnlinePearlStringDefinition? remotedefinition = null;

        public static ConditionalWeakTable<UpdatableAndDeletable, OnlinePearlString> pearlStringMap = new();
        public bool initializedPearls = false;
        public OnlinePearlString(OnlinePearlStringDefinition entityDefinition, OnlineResource inResource, PearlStringState initialState) : base(entityDefinition, inResource, initialState)
        {
            remotedefinition = entityDefinition;
            OnlineManager.RunDeferred(() => FindPearlString(inResource));
        }

        public void FindPearlString(OnlineResource inResource)
        {
            if (pearlString is not null) return;
            if (remotedefinition is null) throw new InvalidProgrammerException("null remote definition");
            if (inResource is not RoomSession roomSession) throw new InvalidProgrammerException("must be added to roomSession");
            if (roomSession.absroom.realizedRoom is not Room room) return;
            switch ((OnlinePearlStringDefinition.PearlStringType)remotedefinition.stringtype)
            {
                case OnlinePearlStringDefinition.PearlStringType.OutpostPearlString:
                    pearlString = room.updateList.OfType<ScavengerOutpost.PearlString>().ElementAtOrDefault(remotedefinition.pearlStringIndex);
                    break;

                case OnlinePearlStringDefinition.PearlStringType.HangingPearlString:
                    pearlString = room.updateList.OfType<MoreSlugcats.HangingPearlString>().ElementAtOrDefault(remotedefinition.pearlStringIndex);
                    break;
                default:
                    throw new InvalidProgrammerException("implement this");
            }

            

            if (pearlString is not null)
            {
                pearlStringMap.Add(pearlString, this);
            } 
            else
            {
                RainMeadow.Debug($"Couldn't find pearl string {remotedefinition.pearlStringIndex}");
            }

            // read the last state again
            if (lastStates.TryGetValue(inResource, out var lastState))
            {
                lastState.ReadTo(this);
            }
            
        }

        public UpdatableAndDeletable? pearlString;
        public OnlinePearlString(UpdatableAndDeletable hangingPearlString, EntityId id, OnlinePlayer owner, bool isTransferable) : base(id, owner, isTransferable)
        {
            initializedPearls = true; // don't override pearls if we registered it.
            this.pearlString = hangingPearlString;
            pearlStringMap.Add(pearlString, this);
        }
    }
}