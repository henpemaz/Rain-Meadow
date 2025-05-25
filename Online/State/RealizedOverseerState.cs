using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    // TODO: The overseer type should be synced in abstractcreaturedef
    //          maybe we could hijack the abscreature customdata?
    // TODO: When zipping, the body and the mycelium gets stretched along the zip.
    //          Should be fixable by ensuring DrawPosOfSegment returns what we want, 
    //          and throwing in a well timed reset on the graphics module
    // TODO: Death is completely broken, need to make Die or HitSomethingWithoutStopping an event.
    //          this leads to an overseer being fake killed on remote.
    public class RealizedOverseerState : RealizedCreatureState
    {
        [OnlineField]
        private int ownerIterator;  // our guide is 1, vanilla goes from 0-5, sandbox uses 10+
        [OnlineField]
        private Vector2 rootPos;
        [OnlineField]
        private IntVector2 rootTile;
        [OnlineField]
        private IntVector2 hoverTile;
        [OnlineFieldHalf]
        private Vector2 lookAt;
        [OnlineField]
        private Overseer.Mode mode;
        [OnlineField]
        private float extended;
        [OnlineField]
        private OnlinePhysicalObject? conversationPartner;

        public RealizedOverseerState() { }
        public RealizedOverseerState(OnlineCreature entity) : base(entity)
        {
            Overseer o = (Overseer)entity.apo.realizedObject;

            ownerIterator = (o.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator;
            rootPos = o.rootPos;
            rootTile = o.rootTile;
            hoverTile = o.hoverTile;
            mode = o.mode;
            lookAt = o.AI.lookAt;
            extended = o.extended;
            conversationPartner = o.conversationPartner?.abstractCreature.GetOnlineObject();
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).realizedCreature is not Overseer overseer) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            (overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator = ownerIterator;
            overseer.rootPos = rootPos;
            overseer.rootTile = rootTile;
            overseer.hoverTile = hoverTile;
            overseer.mode = mode;
            overseer.AI.lookAt = lookAt;
            overseer.extended = extended;
            overseer.conversationPartner = conversationPartner?.apo.realizedObject as Overseer;
        }
    }
}
