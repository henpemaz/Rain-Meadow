using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RainMeadow
{
    public abstract class OnlineState
    {
        [FieldIgnore]
        private StateHandler handler;
        [FieldIgnore]
        private bool[] valueFlags;

        public bool IsEmptyDelta => !this.valueFlags.Any();

        public StateType stateType => handler.stateType;

        protected OnlineState()
        {
            handler = handlersByType[GetType()];
            valueFlags = new bool[handler.ngroups];
        }

        public class StateType : ExtEnum<StateType>
        {
            internal static StateType CreatureStateState;

            public StateType(string value, bool register = false) : base(value, register) { }

            public static explicit operator StateType(byte v)
            {
                return new(values.GetEntry(v), false);
            }
        }

        public enum DeltaSupport
        {
            None,
            FollowsContainer,
            NullableDelta,
            Full
        }

        private static Dictionary<StateType, StateHandler> handlersByEnum = new Dictionary<StateType, StateHandler>();
        private static Dictionary<Type, StateHandler> handlersByType = new Dictionary<Type, StateHandler>();
        
        public static void RegisterState(StateType stateType, Type type, DeltaSupport deltaSupport)
        {
            if (!handlersByEnum.TryGetValue(stateType, out var handler)) { handlersByEnum[stateType] = handlersByType[type] = handler = new StateHandler(stateType, type, deltaSupport); }
        }

        internal void CustomSerialize(Serializer serializer)
        {
            handler.serialize(this, serializer);
        }

        internal OnlineState DeepCopy()
        {
            return handler.deepcopy(this);
        }

        public OnlineState Delta(OnlineState baseline)
        {
            return handler.delta(this, baseline);
        }

        public OnlineState ApplyDelta(OnlineState incoming)
        {
            return handler.applydelta(this, incoming);
        }

        static Serializer mock = new Serializer(4096);
        internal long EstimatedSize(Serializer serializer)
        {
            mock.BeginWrite(OnlineManager.mePlayer);
            CustomSerialize(mock);
            return mock.Position;
        }

        internal static OnlineState NewFromType(StateType stateType)
        {
            return handlersByEnum[stateType].factory();
        }

        public class OnlineFieldAttribute : Attribute
        {
            public string group;

            public OnlineFieldAttribute(string group = "default")
            {
                this.group = group;
            }

            public virtual MethodInfo SerializerMethod()
            {
                return null;
            }

            public virtual Expression ComparisonMethod(MemberExpression currentField, MemberExpression baselineField)
            {
                return null;
            }

            internal uint? EstimatedSize(FieldInfo f)
            {
                return null;
            }
        }

        public class FieldIgnoreAttribute : Attribute { }
        public class FieldNoCopyAttribute : Attribute { }

        private class StateHandler
        {
            public OnlineState.StateType stateType;
            private Type type;
            private OnlineState.DeltaSupport deltaSupport;
            public Func<OnlineState> factory;
            public Action<OnlineState, Serializer> serialize;
            public Func<OnlineState, OnlineState> deepcopy;
            public Func<OnlineState, OnlineState, OnlineState> delta;
            public Func<OnlineState, OnlineState, OnlineState> applydelta;
            public int ngroups;

            // Welcome to Expression Trees hell
            public StateHandler(OnlineState.StateType stateType, Type type, OnlineState.DeltaSupport deltaSupport)
            {
                if (!type.IsValueType && !type.IsClass) throw new InvalidProgrammerException("not class or struct");
                this.stateType = stateType;
                this.type = type;
                this.deltaSupport = deltaSupport;

                BindingFlags anyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var fields = type.GetFields((BindingFlags)(-1)).Where(f => f.GetCustomAttribute<FieldIgnoreAttribute>() == null).OrderBy(f => f.Name).ToArray();
                RainMeadow.Debug($"found {fields.Length} fields");
                Dictionary<string, List<FieldInfo>> deltaGroups = fields.GroupBy(o => o.GetCustomAttribute<OnlineFieldAttribute>()?.group ?? "default").ToDictionary(g => g.Key, g => g.ToList());
                ngroups = deltaGroups.Count;
                RainMeadow.Debug($"found {ngroups} groups");
                var valueFlagsAcessor = typeof(OnlineState).GetField("valueFlags", anyInstance);
                var isDeltaAcessor = typeof(RootDeltaState).GetField("IsDelta", anyInstance);
                var baselineAcessor = typeof(RootDeltaState).GetField("Baseline", anyInstance);
                var tickAcessor = typeof(RootDeltaState).GetField("tick", anyInstance);
                var serializerIsDeltaAcessor = typeof(Serializer).GetField("IsDelta", anyInstance);
                var serializeBoolRef = typeof(Serializer).GetMethod("Serialize", new[] { typeof(bool).MakeByRefType() });
                var serializeUintRef = typeof(Serializer).GetMethod("Serialize", new[] { typeof(uint).MakeByRefType() });

                var expressions = new List<Expression>();

                // make factory

                factory = Expression.Lambda<Func<OnlineState>>(Expression.New(type.GetConstructor(new Type[] { }))).Compile();

                // make serialize func

                ParameterExpression self = Expression.Parameter(typeof(OnlineState));
                ParameterExpression selfConverted = Expression.Variable(type);
                ParameterExpression serializer = Expression.Parameter(typeof(Serializer));

                expressions = new List<Expression>();
                expressions.Add(Expression.Assign(selfConverted, Expression.Convert(self, type)));

                switch (deltaSupport)
                {
                    case OnlineState.DeltaSupport.None:
                        // serializer.Serialize(ref field);
                        expressions.Add(Expression.Block(fields.Select(
                            f => Expression.Call(serializer,
                                f.GetCustomAttribute<OnlineFieldAttribute>()?.SerializerMethod() ?? typeof(Serializer).GetMethod("Serialize", new[] { f.FieldType.MakeByRefType() }),
                                Expression.Field(selfConverted, f))
                        )));
                        break;
                    case OnlineState.DeltaSupport.FollowsContainer:
                        // if (serializer.IsDelta) serializer.Serialize(ref hasGroupValue[n]);
                        // if (!serializer.IsDelta || hasGroupValue)
                        // {
                        //     serializer.Serialize(ref fieldInGroup);
                        // }
                        for (int i = 0; i < ngroups; i++)
                        {
                            if (deltaGroups[deltaGroups.Keys.ToList()[i]].Count == 0) continue;
                            expressions.Add(Expression.IfThen(Expression.Field(serializer, serializerIsDeltaAcessor),
                                Expression.Call(serializer, serializeBoolRef, new[] {
                                Expression.ArrayAccess(Expression.Field(selfConverted, valueFlagsAcessor), Expression.Constant(i)) })));
                            expressions.Add(Expression.IfThen(Expression.OrElse(Expression.Not(Expression.Field(serializer, serializerIsDeltaAcessor)),
                                    Expression.ArrayAccess(Expression.Field(selfConverted, valueFlagsAcessor), Expression.Constant(i))),
                                Expression.Block(deltaGroups[deltaGroups.Keys.ToList()[i]].Select(
                                    f => Expression.Call(serializer,
                                        f.GetCustomAttribute<OnlineFieldAttribute>()?.SerializerMethod() ?? typeof(Serializer).GetMethod("Serialize", new[] { f.FieldType.MakeByRefType() }),
                                        Expression.Field(selfConverted, f))
                                ))));
                        }
                        break;
                    case OnlineState.DeltaSupport.NullableDelta:
                        // if (serializer.IsDelta) // In a non-delta context, can only be non-delta
                        // {
                        //     serializer.Serialize(ref IsDelta);
                        //     serializer.IsDelta = IsDelta;
                        // }
                        // if (IsDelta) serializer.Serialize(ref hasGroupValue);
                        // if (!IsDelta || hasGroupValue)
                        // {
                        //     serializer.Serialize(ref fieldInGroup);
                        // }
                        expressions.Add(Expression.IfThen(Expression.Field(serializer, serializerIsDeltaAcessor), Expression.Block(
                                Expression.Call(serializer, serializeBoolRef, new[] { Expression.Field(self, isDeltaAcessor) }),
                                Expression.Assign(Expression.Field(serializer, serializerIsDeltaAcessor), Expression.Field(self, isDeltaAcessor)
                            ))));
                        goto case OnlineState.DeltaSupport.FollowsContainer;
                    case OnlineState.DeltaSupport.Full:
                        // serializer.Serialize(ref IsDelta);
                        // if (!serializer.IsDelta && IsDelta) { serializer.Serialize(ref Baseline); }
                        // serializer.IsDelta = IsDelta; // Serializer wraps this call and restores the previous value later
                        // ...etc
                        expressions.Add(Expression.Call(serializer, serializeBoolRef, new[] { Expression.Field(self, isDeltaAcessor) }));
                        expressions.Add(Expression.IfThen(Expression.AndAlso(Expression.Not(Expression.Field(serializer, serializerIsDeltaAcessor)),
                                                                 Expression.Field(self, isDeltaAcessor)),
                                    Expression.Call(serializer, serializeUintRef, new[] { Expression.Field(self, baselineAcessor) })));
                        expressions.Add(Expression.Assign(Expression.Field(serializer, serializerIsDeltaAcessor), Expression.Field(self, isDeltaAcessor)));
                        goto case OnlineState.DeltaSupport.FollowsContainer;
                }

                serialize = Expression.Lambda<Action<OnlineState, Serializer>>(Expression.Block(new[] { selfConverted }, expressions), self, serializer).Compile();

                //   make deepcopy func

                var output = Expression.Variable(typeof(OnlineState));
                var memberwiseCloneRef = typeof(object).GetMethod("MemberwiseClone", anyInstance);
                var deepCopyRef = typeof(OnlineState).GetMethod("DeepCopy", anyInstance);

                expressions = new List<Expression>();
                expressions.Add(Expression.Assign(selfConverted, Expression.Convert(self, type)));
                expressions.Add(Expression.Assign(output, Expression.Convert(Expression.Call(selfConverted, memberwiseCloneRef), type)));
                foreach (var f in fields)
                {
                    if (typeof(OnlineState).IsAssignableFrom(f.FieldType) && f.GetCustomAttribute<FieldNoCopyAttribute>() == null)
                    {
                        expressions.Add(Expression.Assign(Expression.Field(output, f), Expression.Call(output, deepCopyRef)));
                    }
                }
                expressions.Add(output); // return

                deepcopy = Expression.Lambda<Func<OnlineState, OnlineState>>(Expression.Block(new[] { selfConverted, output }, expressions), self).Compile();

                // if supports delta
                if (deltaSupport != OnlineState.DeltaSupport.None)
                {
                    // make delta func

                    ParameterExpression baseline = Expression.Parameter(typeof(OnlineState));
                    ParameterExpression baselineConverted = Expression.Variable(type);

                    // if (baseline == null) throw new InvalidProgrammerException("baseline is null");
                    // **if (baseline.IsDelta) throw new InvalidProgrammerException("baseline is delta");
                    // var output = DeepCopy();
                    // **output.IsDelta = true;
                    // **output.baseline = baseline.tick;
                    // 
                    // output.hasGroupValue = field != baseline.field || field2 != baseline.field2;

                    expressions = new List<Expression>();
                    // todo arg checking
                    expressions.Add(Expression.Assign(selfConverted, Expression.Convert(self, type)));
                    expressions.Add(Expression.Assign(baselineConverted, Expression.Convert(baseline, type)));
                    expressions.Add(Expression.Assign(output, Expression.Convert(Expression.Call(self, deepCopyRef), type)));

                    if (deltaSupport != OnlineState.DeltaSupport.FollowsContainer)
                    {
                        expressions.Add(Expression.Assign(Expression.Field(output, isDeltaAcessor), Expression.Constant(true)));
                        expressions.Add(Expression.Assign(Expression.Field(output, baselineAcessor), Expression.Field(baselineConverted, tickAcessor)));
                    }

                    // todo check if field is deltaable, fieldwise delta
                    for (int i = 0; i < ngroups; i++)
                    {
                        if (deltaGroups[deltaGroups.Keys.ToList()[i]].Count == 0) continue;
                        expressions.Add(Expression.Assign(Expression.ArrayAccess(Expression.Field(selfConverted, valueFlagsAcessor), Expression.Constant(i)),
                            OrAny(deltaGroups[deltaGroups.Keys.ToList()[i]].Select(
                                    f => f.GetCustomAttribute<OnlineFieldAttribute>()?.ComparisonMethod(Expression.Field(selfConverted, f), Expression.Field(baselineConverted, f))
                                        ?? Expression.Equal(Expression.Field(selfConverted, f), Expression.Field(baselineConverted, f))
                                ).ToArray())
                            ));
                    }

                    expressions.Add(output); // return

                    delta = Expression.Lambda<Func<OnlineState, OnlineState, OnlineState>>(Expression.Block(new[] { selfConverted, baselineConverted, output }, expressions), self, baseline).Compile();

                    // make applydelta func

                    ParameterExpression incoming = Expression.Parameter(typeof(OnlineState));
                    ParameterExpression incomingConverted = Expression.Variable(type);

                    // if (incoming == null) throw new InvalidProgrammerException("incoming is null");
                    // **if (!incoming.IsDelta) throw new InvalidProgrammerException("incoming not delta");
                    // var result = DeepClone();
                    // **result.tick = incoming.tick;
                    // if incoming.hasGroupValue
                    //      result.field1 = incoming.field1;
                    // return result;

                    expressions = new List<Expression>();
                    // todo arg checking
                    expressions.Add(Expression.Assign(selfConverted, Expression.Convert(self, type)));
                    expressions.Add(Expression.Assign(incomingConverted, Expression.Convert(incoming, type)));
                    expressions.Add(Expression.Assign(output, Expression.Convert(Expression.Call(self, deepCopyRef), type)));

                    if (deltaSupport != OnlineState.DeltaSupport.FollowsContainer)
                    {
                        expressions.Add(Expression.Assign(Expression.Field(output, tickAcessor), Expression.Field(incomingConverted, tickAcessor)));
                    }

                    for (int i = 0; i < ngroups; i++)
                    {
                        if (deltaGroups[deltaGroups.Keys.ToList()[i]].Count == 0) continue;

                        // todo check if field is deltaable, fieldwise delta
                        expressions.Add(Expression.IfThen(Expression.ArrayAccess(Expression.Field(incomingConverted, valueFlagsAcessor), Expression.Constant(i)),
                                Expression.Block(deltaGroups[deltaGroups.Keys.ToList()[i]].Select(
                                    f => Expression.Assign(Expression.Field(selfConverted, f), Expression.Field(incomingConverted, f))
                                ))));
                    }

                    expressions.Add(output); // return

                    applydelta = Expression.Lambda<Func<OnlineState, OnlineState, OnlineState>>(Expression.Block(new[] { selfConverted, incomingConverted, output }, expressions), self, incoming).Compile();
                }
            }

            // c# son why must you disappoint me so often
            private Expression OrAny(params Expression[] args)
            {
                if (args.Length == 1) return args[0];
                if (args.Length == 2) return Expression.OrElse(args[0], args[1]);
                return Expression.OrElse(args[0], OrAny(args.Skip(1).ToArray()));
            }
        }
    }
}