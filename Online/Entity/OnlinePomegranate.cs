using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RainMeadow.OnlineConsumable;
using static RainMeadow.OnlineEntity;
using static RainMeadow.OnlineSeedCob;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;

namespace RainMeadow
{
    public class OnlinePomegranate : OnlineConsumable
    {
        public OnlinePomegranate(OnlineConsumableDefinition entityDefinition, OnlineResource inResource, OnlineConsumableState initialState) : base(entityDefinition, inResource, initialState) { }

        public OnlinePomegranate(AbstractConsumable ac, EntityId id, OnlinePlayer owner, bool isTransferable) : base(ac, id, owner, isTransferable) { }

        public Pomegranate.AbstractPomegranate AbstractPomegranate => apo as Pomegranate.AbstractPomegranate;

        public override EntityDefinition MakeDefinition(OnlineResource inResource)
        {
            return new OnlinePomegranateDefinition(this, inResource);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new OnlinePomegranateState(this, inResource, tick);
        }

        protected override AbstractPhysicalObject ApoFromDef(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource, AbstractPhysicalObjectState initialState)
        {
            var consumableDef = (OnlinePomegranateDefinition)newObjectEvent;
            var apo = base.ApoFromDef(newObjectEvent, inResource, initialState);
            RoomSettings roomsetting = new RoomSettings(apo.Room.name, apo.world.region, false, false, OnlineManager.lobby.gameMode.LoadWorldIn(apo.world.game), apo.world.game);

            var asc = new Pomegranate.AbstractPomegranate(
                apo.world, apo.realizedObject, apo.pos, apo.ID, 
                consumableDef.originRoom, consumableDef.placedObjectIndex, 
                roomsetting.placedObjects[consumableDef.placedObjectIndex].data as PlacedObject.ConsumableObjectData,
                consumableDef.originallySmashed, consumableDef.originallyDisconnected, consumableDef.originallyStabbed)
            { isConsumed = (initialState as OnlineConsumableState).isConsumed };
            return asc;
        }
    }

    public class OnlinePomegranateDefinition : OnlineConsumableDefinition
    {
        [OnlineField]
        public bool originallySmashed;
        [OnlineField]
        public bool originallyDisconnected;
        [OnlineField]
        public bool originallyStabbed;
        public OnlinePomegranateDefinition() { }
        public OnlinePomegranateDefinition(OnlinePomegranate onlinePomegranate, OnlineResource inResource) : base(onlinePomegranate, inResource)
        {
            originallySmashed = onlinePomegranate.AbstractPomegranate.smashed;
            originallyDisconnected = onlinePomegranate.AbstractPomegranate.disconnected;
            originallyStabbed = onlinePomegranate.AbstractPomegranate.spearmasterStabbed;
        }
        public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
        {
            return new OnlinePomegranate(this, inResource, (OnlinePomegranateState)initialState);
        }
    }

    public class OnlinePomegranateState : OnlineConsumableState
    {
        [OnlineField]
        bool smashed;
        [OnlineField]
        bool disconnected;
        [OnlineField]
        bool spearmasterStabbed;
        public OnlinePomegranateState() { }
        public OnlinePomegranateState(OnlinePomegranate onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            smashed = onlineEntity.AbstractPomegranate.smashed;
            disconnected = onlineEntity.AbstractPomegranate.disconnected;
            spearmasterStabbed = onlineEntity.AbstractPomegranate.spearmasterStabbed;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var onlinePomegranate = onlineEntity as OnlinePomegranate;
            var pomegranate = onlinePomegranate.AbstractPomegranate;
            if (onlinePomegranate.apo.realizedObject is Pomegranate realized)
            {
                if (smashed && !pomegranate.smashed)
                {
                    realized.EnterSmashedMode();
                }
                if (disconnected && !pomegranate.disconnected)
                {
                    realized.Disconnect();
                }
                if (spearmasterStabbed && !pomegranate.spearmasterStabbed)
                {
                    realized.stabbedFade = 1f;
                }
            }
            else
            {
                pomegranate.smashed = smashed;
                pomegranate.disconnected = disconnected;
                pomegranate.spearmasterStabbed = spearmasterStabbed;
            }
        }
    }
}
