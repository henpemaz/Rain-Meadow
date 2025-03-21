namespace RainMeadow
{
    public partial class RoomSession
    {
        [RPCMethod]
        public void AbstractRoomFirstTimeRealized()
        {
            absroom.firstTimeRealized = false;
        }


        [RPCMethod] 
        public void CreaturePutItemOnGround(OnlineEntity.EntityId item, OnlineEntity.EntityId creature) {
            if (item?.FindEntity() is OnlinePhysicalObject onlineitem && 
                creature?.FindEntity() is OnlineCreature onlineCreature) {
                    if (this.absroom.realizedRoom is not null && 
                        onlineitem.apo.realizedObject is not null && 
                        onlineCreature.creature.realizedCreature is not null) {
                        this.absroom.realizedRoom.socialEventRecognizer.CreaturePutItemOnGround(
                            onlineitem.apo.realizedObject, onlineCreature.creature.realizedCreature);
                    }
            }
        }
    }
}
