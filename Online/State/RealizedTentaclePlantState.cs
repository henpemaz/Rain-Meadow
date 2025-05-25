using UnityEngine;

namespace RainMeadow
{
    public class RealizedTentaclePlantState : RealizedCreatureState
    {
        [OnlineField]
        OnlinePhysicalObject? mostInterestingItem;
        [OnlineField]
        Vector2 idlePos;
        [OnlineField]
        Vector2? floatGrabDest;

        public RealizedTentaclePlantState() { }
        public RealizedTentaclePlantState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            TentaclePlant plant = (TentaclePlant)onlineEntity.apo.realizedObject;
            mostInterestingItem = plant.AI.mostInterestingItem?.abstractPhysicalObject.GetOnlineObject();
            idlePos = plant.idlePos;
            floatGrabDest = plant.tentacle.floatGrabDest;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (((OnlineCreature)onlineEntity).apo.realizedObject is not TentaclePlant plant) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            plant.AI.mostInterestingItem = mostInterestingItem?.apo.realizedObject;
            plant.idlePos = idlePos;
            plant.tentacle.floatGrabDest = floatGrabDest;
        }
    }
}

