using UnityEngine;

namespace RainMeadow
{
    // We reuse this for Player.Tongue since all the fields are the same
    // unfortunately they straight up copypasted the code
    // TubeWorm.Tongue and Player.Tongue are two entirely different types
    // Thanks, Andrew -,-
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class TongueState : OnlineState
    {
        [OnlineField]
        public byte mode;
        [OnlineField]
        public Vector2 pos;
        [OnlineFieldHalf(group = "player")]
        public float idealRopeLength; // used by Player.Tongue, not modified by TubeWorm
        [OnlineFieldHalf]
        public float requestedRopeLength;
        [OnlineField(nullable = true)]
        public BodyChunkRef attachedChunk;

        public TongueState() { }
        public TongueState(TubeWorm.Tongue tongue)
        {
            mode = (byte)tongue.mode;
            pos = tongue.pos;
            idealRopeLength = tongue.idealRopeLength;
            requestedRopeLength = tongue.requestedRopeLength;
            attachedChunk = BodyChunkRef.FromBodyChunk(tongue.attachedChunk);
        }

        public TongueState(Player.Tongue tongue)
        {
            mode = (byte)tongue.mode;
            pos = tongue.pos;
            idealRopeLength = tongue.idealRopeLength;
            requestedRopeLength = tongue.requestedRopeLength;
            attachedChunk = BodyChunkRef.FromBodyChunk(tongue.attachedChunk);
        }

        public void ReadTo(TubeWorm.Tongue tongue)
        {
            tongue.mode = new TubeWorm.Tongue.Mode(TubeWorm.Tongue.Mode.values.GetEntry(mode));
            tongue.pos = pos;
            tongue.idealRopeLength = idealRopeLength;
            tongue.requestedRopeLength = requestedRopeLength;
            if (tongue.mode == TubeWorm.Tongue.Mode.AttachedToTerrain)
            {
                tongue.terrainStuckPos = tongue.pos;
            }
            else if (tongue.mode == TubeWorm.Tongue.Mode.AttachedToObject)
            {
                tongue.attachedChunk = attachedChunk.ToBodyChunk();
            }
        }

        public void ReadTo(Player.Tongue tongue)
        {
            tongue.mode = new Player.Tongue.Mode(TubeWorm.Tongue.Mode.values.GetEntry(mode));
            tongue.pos = pos;
            tongue.idealRopeLength = idealRopeLength;
            tongue.requestedRopeLength = requestedRopeLength;
            if (tongue.mode == Player.Tongue.Mode.AttachedToTerrain)
            {
                tongue.terrainStuckPos = tongue.pos;
            }
            else if (tongue.mode == Player.Tongue.Mode.AttachedToObject)
            {
                tongue.attachedChunk = attachedChunk.ToBodyChunk();
            }
        }
    }
}
