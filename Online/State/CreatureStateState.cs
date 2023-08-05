using RainMeadow.Generics;

namespace RainMeadow
{
    public class CreatureStateState : OnlineState, IDelta<CreatureStateState>
    {
        // main part of AbstractCreatureState
        public bool alive;
        public byte meatLeft;

        protected bool hasStateValue;

        public virtual CreatureStateState EmptyDelta() => new();
        public CreatureStateState() { }
        public CreatureStateState(OnlineCreature onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            alive = abstractCreature.state.alive;
            meatLeft = (byte)abstractCreature.state.meatLeft;
        }

        public override StateType stateType => StateType.CreatureStateState;

        public bool IsEmptyDelta { get; set; }

        public override void CustomSerialize(Serializer serializer)
        {
            if (serializer.IsDelta) serializer.Serialize(ref hasStateValue);
            if (!serializer.IsDelta || hasStateValue)
            {
                serializer.Serialize(ref alive);
                serializer.Serialize(ref meatLeft);
            }
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            var val = 1l;
            if (inDeltaContext) val += 1;
            if (!inDeltaContext || hasStateValue)
            {
                val += 2;
            }
            return val;
        }

        public virtual CreatureStateState Delta(CreatureStateState _other)
        {
            if (_other == null) throw new InvalidProgrammerException("null");
            var delta = EmptyDelta();
            delta.alive = alive;
            delta.meatLeft = meatLeft;
            delta.hasStateValue = alive != _other.alive || meatLeft != _other.meatLeft;
            delta.IsEmptyDelta = !delta.hasStateValue;
            return delta;
        }

        public virtual CreatureStateState ApplyDelta(CreatureStateState _other)
        {
            if (_other == null) throw new InvalidProgrammerException("null");
            var result = EmptyDelta();
            if(_other.hasStateValue)
            {
                result.alive = _other.alive;
                result.meatLeft = _other.meatLeft;
            }
            else
            {
                result.alive = alive;
                result.meatLeft = meatLeft;
            }
            return result;
        }
    }
}