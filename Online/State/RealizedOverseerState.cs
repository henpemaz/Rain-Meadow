using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    // oh my god overseers are terrible
    // seriously

    // TODO: The overseer type isnt synced. (or atleast the guide, havent tested others).
    //          we could technically do this here if we didnt mind resending the same information every packet lmao.
    // TODO: Conversations dont work, remote conversations dont play the animation.
    // TODO: Remote overseers' mycelium (the lil tentacles) dont get moved along in a zip
    // TODO: Death is completely broken, need to make Die or HitSomethingWithoutStopping an event.
    //          this leads to an overseer being fake killed on remote.
    // TODO: Ask videocult to remove overseers from the game.
    public class RealizedOverseerState : RealizedCreatureState
    {
        
        private Vector2 rootPos;
        private IntVector2 rootTile;
        private IntVector2 hoverTile;
        private Vector2 lookAt;
        private byte mode;
        private float extended;
        private IntVector2 tempHoverTile; // used for zipping calculation. If not synced, zips will be out of sync.
        //private IntVector2 

        // Zipping. 
        //private List<IntVector2> zipPath;
        //private float[] zipProgs;
        //private int zipPathCount;

        // Graphics
        //private Vector2 lookAtAdd; // is random but seems too small to matter
        //private float holoLensUp;


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
            tempHoverTile = o.AI.tempHoverTile;
            //lookAtAdd = o.AI.lookAtAdd;
        }

        public override StateType stateType => StateType.RealizedOverseerState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref rootPos);
            serializer.Serialize(ref rootTile);
            serializer.Serialize(ref hoverTile);
            serializer.Serialize(ref mode);
            serializer.Serialize(ref lookAt);
            serializer.Serialize(ref extended);
            serializer.Serialize(ref tempHoverTile);
            //serializer.Serialize(ref lookAtAdd);

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var overseer = (Overseer)((OnlineCreature)onlineEntity).apo.realizedObject;


            overseer.rootPos = rootPos;
            overseer.rootTile = rootTile;
            overseer.hoverTile = hoverTile;
            overseer.mode = new Overseer.Mode(Overseer.Mode.values.GetEntry(mode));
            overseer.AI.lookAt = lookAt;
            overseer.extended = extended;
            overseer.AI.tempHoverTile = tempHoverTile;
            //overseer.AI.lookAtAdd = lookAtAdd;
        }
    }
}
