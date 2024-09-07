using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    // shouldnt allow object to update if stuck to thing from different person
    // shouldn't allow to be stuck to thing from different owner
    // if needs to stick to thing locally should request it

    [DeltaSupport(level = StateHandler.DeltaSupport.None)] // imutable, only a state for polymorphism
    public abstract class AbstractObjStickRepr : OnlineState
    {
        public static ConditionalWeakTable<AbstractPhysicalObject.AbstractObjectStick, AbstractObjStickRepr> map = new(); // these are generated "once per grasp" and reused with this map
        // this way we can have them be fed through the delta mechanism whith just plain referenceequals comparisons which is peak efficiency

        [OnlineField]
        public OnlineEntity.EntityId B;

        private OnlinePhysicalObject _b;
        public OnlinePhysicalObject b
        {
            get
            {
                if (_b == null) _b = B.FindEntity() as OnlinePhysicalObject;
                return _b;
            }
        }


        public AbstractObjStickRepr() { }

        protected AbstractObjStickRepr(OnlinePhysicalObject b)
        {
            _b = b;
            B = b.id;
            RainMeadow.Debug("new stick! " + this);
        }

        public override string ToString()
        {
            return $"{GetType()} - to {B}";
        }

        public static AbstractObjStickRepr FromStick(AbstractPhysicalObject.AbstractObjectStick stick)
        {
            OnlinePhysicalObject b = null;
            if (!OnlinePhysicalObject.map.TryGetValue(stick.A, out var a) || !OnlinePhysicalObject.map.TryGetValue(stick.B, out b))
            {
                RainMeadow.Error($"skipping stick because creatures not found online: {stick.A} {a} {stick.B} {b}");
                return null;
            }
            switch (stick)
            {
                case AbstractPhysicalObject.AbstractSpearStick s:
                    return new SpearStick(b, s);
                case AbstractPhysicalObject.AbstractSpearAppendageStick s:
                    return new SpearAppendageStick(b, s);
                case AbstractPhysicalObject.ImpaledOnSpearStick s:
                    return new ImpaledOnSpearStick(b, s);
                case Player.AbstractOnBackStick s:
                    return new OnBackStick(b, s);
                case AbstractPhysicalObject.CreatureGripStick s:
                    return new CreatureGripStick(b, s);
                default:
                    throw new NotImplementedException(stick.ToString());
            }
        }

        internal abstract void MakeStick(AbstractPhysicalObject A);

        internal void Release(AbstractPhysicalObject.AbstractObjectStick abstractObjectStick)
        {
            RainMeadow.Debug(this);
            abstractObjectStick.Deactivate();
        }

        internal virtual bool StickEquals(AbstractPhysicalObject.AbstractObjectStick s, OnlinePhysicalObject a, OnlinePhysicalObject b)
        {
            return a.apo == s.A && b.apo == s.B;
        }

        public class SpearStick : AbstractObjStickRepr
        {
            [OnlineField]
            public sbyte chunk;
            [OnlineField]
            public sbyte bodyPart;
            [OnlineFieldHalf]
            public float angle;

            public SpearStick() { }
            public SpearStick(OnlinePhysicalObject b, AbstractPhysicalObject.AbstractSpearStick s) : base(b)
            {
                chunk = (sbyte)s.chunk;
                bodyPart = (sbyte)s.bodyPart;
                angle = s.angle;
            }

            internal override void MakeStick(AbstractPhysicalObject A)
            {
                RainMeadow.Debug(this);
                if (OnlinePhysicalObject.map.TryGetValue(A, out OnlinePhysicalObject a) && a.owner == b.owner && !a.isPending && !b.isPending)
                {
                    var stick = new AbstractPhysicalObject.AbstractSpearStick(a.apo, b.apo, chunk, bodyPart, angle);
                    map.Remove(stick);
                    map.Add(stick, this);
                }
            }
        }

        public class SpearAppendageStick : AbstractObjStickRepr
        {
            [OnlineField]
            public sbyte appendage;
            [OnlineField]
            public sbyte prevSeg;
            [OnlineFieldHalf]
            public float distanceToNext;
            [OnlineFieldHalf]
            public float angle;

            public SpearAppendageStick() { }
            public SpearAppendageStick(OnlinePhysicalObject b, AbstractPhysicalObject.AbstractSpearAppendageStick s) : base(b)
            {
                appendage = (sbyte)s.appendage;
                prevSeg = (sbyte)s.prevSeg;
                distanceToNext = s.distanceToNext;
                angle = s.angle;
            }

            internal override void MakeStick(AbstractPhysicalObject A)
            {
                RainMeadow.Debug(this);
                if (OnlinePhysicalObject.map.TryGetValue(A, out OnlinePhysicalObject a) && a.owner == b.owner && !a.isPending && !b.isPending)
                {
                    var stick = new AbstractPhysicalObject.AbstractSpearAppendageStick(a.apo, b.apo, appendage, prevSeg, distanceToNext, angle);
                    map.Remove(stick);
                    map.Add(stick, this);
                }
            }
        }

        public class ImpaledOnSpearStick : AbstractObjStickRepr
        {
            [OnlineField]
            public sbyte chunk;
            [OnlineField]
            public sbyte onSpearPosition;
            public ImpaledOnSpearStick() { }
            public ImpaledOnSpearStick(OnlinePhysicalObject b, AbstractPhysicalObject.ImpaledOnSpearStick s) : base(b)
            {
                chunk = (sbyte)s.chunk;
                onSpearPosition = (sbyte)s.onSpearPosition;
            }

            internal override void MakeStick(AbstractPhysicalObject A)
            {
                RainMeadow.Debug(this);
                if (OnlinePhysicalObject.map.TryGetValue(A, out OnlinePhysicalObject a) && a.owner == b.owner && !a.isPending && !b.isPending)
                {
                    var stick = new AbstractPhysicalObject.ImpaledOnSpearStick(a.apo, b.apo, chunk, onSpearPosition);
                    map.Remove(stick);
                    map.Add(stick, this);
                }
            }

            internal override bool StickEquals(AbstractPhysicalObject.AbstractObjectStick s, OnlinePhysicalObject a, OnlinePhysicalObject b)
            {
                return base.StickEquals(s, a, b) && s is AbstractPhysicalObject.ImpaledOnSpearStick ioss && ioss.onSpearPosition == onSpearPosition;
            }
        }

        public class OnBackStick : AbstractObjStickRepr
        {
            public OnBackStick() { }
            public OnBackStick(OnlinePhysicalObject b, Player.AbstractOnBackStick s) : base(b)
            {

            }

            internal override void MakeStick(AbstractPhysicalObject A)
            {
                RainMeadow.Debug(this);
                if (OnlinePhysicalObject.map.TryGetValue(A, out OnlinePhysicalObject a) && a.owner == b.owner && !a.isPending && !b.isPending)
                {
                    var stick = new Player.AbstractOnBackStick(a.apo, b.apo);
                    map.Remove(stick);
                    map.Add(stick, this);
                }
            }
        }

        public class CreatureGripStick : AbstractObjStickRepr
        {
            [OnlineField]
            public sbyte grasp;
            [OnlineField]
            public bool carry;
            public CreatureGripStick() { }
            public CreatureGripStick(OnlinePhysicalObject b, AbstractPhysicalObject.CreatureGripStick s) : base(b)
            {
                grasp = (sbyte)s.grasp;
                carry = s.carry;
            }

            internal override void MakeStick(AbstractPhysicalObject A)
            {
                RainMeadow.Debug(this);
                if (OnlinePhysicalObject.map.TryGetValue(A, out OnlinePhysicalObject a) && a.owner == b.owner && !a.isPending && !b.isPending)
                {
                    var stick = new AbstractPhysicalObject.CreatureGripStick((AbstractCreature)a.apo, b.apo, grasp, carry);
                    map.Remove(stick);
                    map.Add(stick, this);
                }
            }

            internal override bool StickEquals(AbstractPhysicalObject.AbstractObjectStick s, OnlinePhysicalObject a, OnlinePhysicalObject b)
            {
                return base.StickEquals(s, a, b) && s is AbstractPhysicalObject.CreatureGripStick cgs && cgs.grasp == grasp;
            }
        }
    }
}