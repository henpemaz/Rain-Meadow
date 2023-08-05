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
        private Vector2 rootPos;
        private IntVector2 rootTile;
        private IntVector2 hoverTile;
        private Vector2 lookAt;
        private byte mode;
        private float extended;
        private OnlineEntity.EntityId? conversationPartner;

        bool hasOverseerValue;

        public override RealizedPhysicalObjectState EmptyDelta() => new RealizedOverseerState();
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

        public override StateType stateType => StateType.RealizedOverseerState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            if (IsDelta) serializer.Serialize(ref hasOverseerValue);
            if (!IsDelta || hasOverseerValue)
            {
                serializer.Serialize(ref rootPos);
                serializer.Serialize(ref rootTile);
                serializer.Serialize(ref hoverTile);
                serializer.Serialize(ref mode);
                serializer.Serialize(ref lookAt);
                serializer.SerializeHalf(ref extended);
                serializer.SerializeNullable(ref conversationPartner);
            }
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            var val = base.EstimatedSize(inDeltaContext);
            if (IsDelta) val += 1;
            if (!IsDelta || hasOverseerValue)
            {
                val += 28;
                if (conversationPartner != null) val += 6;
            }
            return val;
        }

        public override RealizedPhysicalObjectState Delta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedOverseerState)_other;
            var delta = (RealizedOverseerState)base.Delta(_other);
            delta.rootPos = rootPos;
            delta.rootTile = rootTile;
            delta.hoverTile = hoverTile;
            delta.mode = mode;
            delta.lookAt = lookAt;
            delta.extended = extended;
            delta.conversationPartner = conversationPartner;
            delta.hasOverseerValue = !rootPos.CloseEnough(other.rootPos, 1f)
                || rootTile != other.rootTile
                || hoverTile != other.hoverTile
                || mode != other.mode
                || !lookAt.CloseEnough(other.lookAt, 1f)
                || extended != other.extended
                || conversationPartner != other.conversationPartner;
            delta.IsEmptyDelta &= !delta.hasOverseerValue;
            return delta;
        }

        public override RealizedPhysicalObjectState ApplyDelta(RealizedPhysicalObjectState _other)
        {
            var other = (RealizedOverseerState)_other;
            var result = (RealizedOverseerState)base.ApplyDelta(_other);
            if (other.hasOverseerValue)
            {
                result.rootPos = other.rootPos;
                result.rootTile = other.rootTile;
                result.hoverTile = other.hoverTile;
                result.mode = other.mode;
                result.lookAt = other.lookAt;
                result.extended = other.extended;
                result.conversationPartner = other.conversationPartner;
            }
            else
            {
                result.rootPos = rootPos;
                result.rootTile = rootTile;
                result.hoverTile = hoverTile;
                result.mode = mode;
                result.lookAt = lookAt;
                result.extended = extended;
                result.conversationPartner = conversationPartner;
            }
            return result;
        }
    }
}
