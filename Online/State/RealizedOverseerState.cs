using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    // TODO: The overseer type isnt synced. (or atleast the guide, havent tested others).
    //          we could technically do this here if we didnt mind resending the same information every packet lmao.
    // TODO: When zipping, the body and the mycelium gets stretched along the zip.
    //          Should be fixable by ensuring DrawPosOfSegment returns what we want, 
    //          and throwing in a well timed reset on the graphics module
    // TODO: Death is completely broken, need to make Die or HitSomethingWithoutStopping an event.
    //          this leads to an overseer being fake killed on remote.
    public class RealizedOverseerState : RealizedCreatureState
    {
        [OnlineField]
        private Vector2 rootPos;
        [OnlineField]
        private IntVector2 rootTile;
        [OnlineField]
        private IntVector2 hoverTile;
        [OnlineField]
        private Vector2 lookAt;
        [OnlineField]
        private byte mode;
        [OnlineField]
        private float extended;
        [OnlineField(nullable = true)]
        private OnlineEntity.EntityId? conversationPartner;

        public RealizedOverseerState() { }
        public RealizedOverseerState(OnlineCreature entity) : base(entity)
        {
            Overseer o = entity.apo.realizedObject as Overseer;

            rootPos = o.rootPos;
            rootTile = o.rootTile;
            hoverTile = o.hoverTile;
            mode = (byte)o.mode.index;
            lookAt = o.AI.lookAt;
            extended = o.extended;

            if (o.conversationPartner != null)
            {
                if (!OnlinePhysicalObject.map.TryGetValue(o.conversationPartner.abstractPhysicalObject, out var conversationPartner)) throw new System.InvalidOperationException("Conversation partner doesnt exist in online space!");
                this.conversationPartner = conversationPartner.id;
            }
            else
            {
                this.conversationPartner = null;
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var overseer = ((OnlineCreature)onlineEntity).apo.realizedObject as Overseer;
            if (overseer == null) return;

            overseer.rootPos = rootPos;
            overseer.rootTile = rootTile;
            overseer.hoverTile = hoverTile;
            overseer.mode = new Overseer.Mode(Overseer.Mode.values.GetEntry(mode));
            overseer.AI.lookAt = lookAt;
            overseer.extended = extended;

            if (conversationPartner != null)
            {
                overseer.conversationPartner = (conversationPartner.FindEntity() as OnlineCreature).apo.realizedObject as Overseer;
            }
            else
            {
                conversationPartner = null;
            }
        }
    }
}
