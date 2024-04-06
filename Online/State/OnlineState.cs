using RainMeadow.GameModes;
using RainMeadow.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using static RainMeadow.Lobby;
using static RainMeadow.RoomSession;
using static RainMeadow.WorldSession;

namespace RainMeadow
{
    public abstract class OnlineState : Generics.IPrimaryDelta<OnlineState>
    {
        public StateHandler handler;
        public bool[] valueFlags;

        public bool isDelta;
        public bool IsEmptyDelta => isDelta && !this.valueFlags.Any(x => x);

        protected OnlineState()
        {
            handler = handlersByType[GetType()];
            valueFlags = new bool[handler.ngroups];
        }

        // Warning: not a deep clone
        public OnlineState Clone()
        {
            var e = (OnlineState)MemberwiseClone();
            e.valueFlags = new bool[handler.ngroups];
            return e;
        }

        // todo register these automatically I'm tired of typing new ones in here
        // todo figure out how to handle indexes for modded stuff (so doesn't depend on load-order and so forth)
        public class StateType : ExtEnum<StateType>
        {
            public StateType(string value, bool register) : base(value, register) { }
            public StateType(string value) : base(value, false) { }
            public StateType(string value, Type type) : base(value, true) { OnlineState.RegisterState(this, type); }

            public static readonly StateType Unknown = new("Unknown", true); // sending zeroes over should error out

            public static readonly StateType LobbyState = new("LobbyState", typeof(LobbyState));
            public static readonly StateType WorldState = new("WorldState", typeof(WorldState));
            public static readonly StateType RoomState = new("RoomState", typeof(RoomState));

            public static readonly StateType EntityFeedState = new("EntityFeedState", typeof(EntityFeedState));

            public static readonly StateType PhysicalObjectEntityState = new("PhysicalObjectEntityState", typeof(PhysicalObjectEntityState));
            public static readonly StateType OnlineConsumableState = new("OnlineConsumableState", typeof(OnlineConsumableState));
            public static readonly StateType OnlineSeedCobState = new("OnlineSeedCobState", typeof(OnlineSeedCobState));
            public static readonly StateType OnlineBubbleGrassState = new("OnlineBubbleGrassState", typeof(OnlineBubbleGrassState));
            public static readonly StateType OnlineSporePlantState = new("OnlineSporePlantState", typeof(OnlineSporePlantState));
            public static readonly StateType PlayerStateState = new("PlayerStateState", typeof(PlayerStateState));
            public static readonly StateType AbstractCreatureState = new("AbstractCreatureState", typeof(AbstractCreatureState));
            public static readonly StateType RealizedFlyState = new("RealizedFlyState", typeof(RealizedFlyState));
            public static readonly StateType RealizedPhysicalObjectState = new("RealizedPhysicalObjectState", typeof(RealizedPhysicalObjectState));
            public static readonly StateType RealizedDangleFruitState = new("RealizedDangleFruitState", typeof(RealizedDangleFruitState));
            public static readonly StateType RealizedSeedCobState = new("RealizedSeedCobState", typeof(RealizedSeedCobState));
            public static readonly StateType RealizedSlimeMoldState = new("RealizedSlimeMoldState", typeof(RealizedSlimeMoldState));
            public static readonly StateType RealizedVultureGrubState = new("RealizedVultureGrubState", typeof(RealizedVultureGrubState));
            public static readonly StateType RealizedCreatureState = new("RealizedCreatureState", typeof(RealizedCreatureState));
            public static readonly StateType RealizedPlayerState = new("RealizedPlayerState", typeof(RealizedPlayerState));
            public static readonly StateType RealizedOverseerState = new("RealizedOverseerState", typeof(RealizedOverseerState));
            public static readonly StateType RealizedWeaponState = new("RealizedWeaponState", typeof(RealizedWeaponState));
            public static readonly StateType RealizedSporePlantState = new("RealizedSporePlantState", typeof(RealizedSporePlantState));
            public static readonly StateType RealizedSpearState = new("RealizedSpearState", typeof(RealizedSpearState));
            public static readonly StateType CreatureStateState = new("CreatureStateState", typeof(CreatureStateState));
            public static readonly StateType CreatureHealthStateState = new("CreatureHealthStateState", typeof(CreatureHealthStateState));

            public static readonly StateType RainCycleDataState = new("RainCycleDataState", typeof(RainCycleData));

            public static readonly StateType MeadowPersonaSettingsState = new("MeadowPersonaSettings.State", typeof(MeadowAvatarSettings.State));
            public static readonly StateType SlugcatAvatarSettingsState = new("SlugcatAvatarSettings.State", typeof(StoryClientSettings.State));
            public static readonly StateType ArenaCustomization = new("ArenaAvatarCustomization.State", typeof(ArenaClientSettings.State));

            //public static readonly StateType GamemodeDataState = new("GamemodeDataState", typeof(GamemodeDataState));
            public static readonly StateType MeadowCreatureDataState = new("MeadowCreatureDataState", typeof(MeadowCreatureData.State));
            public static readonly StateType MeadowLobbyState = new("MeadowLobbyState", typeof(MeadowLobbyData.State));


            public static readonly StateType MeadowWorldState = new("MeadowWorldState", typeof(MeadowWorldData.State));
            public static readonly StateType MeadowRoomState = new("MeadowRoomState", typeof(MeadowRoomData.State));

            public static readonly StateType StoryLobbyDataState = new("StoryLobbyDataState", typeof(StoryLobbyData.State));
            public static readonly StateType ArenaLobbyDataState = new("ArenaLobbyDataState", typeof(ArenaLobbyData.State));


            public static readonly StateType OnlinePhysicalObjectDefinition = new("OnlinePhysicalObjectDefinition", typeof(OnlinePhysicalObjectDefinition));
            public static readonly StateType OnlineConsumableDefinition = new("OnlineConsumableDefinition", typeof(OnlineConsumableDefinition));
            public static readonly StateType OnlinePebblesPearlDefinition = new("OnlinePebblesPearlDefinition", typeof(OnlinePebblesPearlDefinition));
            public static readonly StateType OnlineSeedCobDefinition = new("OnlineSeedCobDefinition", typeof(OnlineSeedCobDefinition));
            public static readonly StateType OnlineBubbleGrassDefinition = new("OnlineBubbleGrassDefinition", typeof(OnlineBubbleGrassDefinition));
            public static readonly StateType OnlineSporePlantDefinition = new("OnlineSporePlantDefinition", typeof(OnlineSporePlantDefinition));
            public static readonly StateType OnlineCreatureDefinition = new("OnlineCreatureDefinition", typeof(OnlineCreatureDefinition));

            public static readonly StateType MeadowAvatarSettingsDefinition = new ("MeadowAvatarSettings.Definition", typeof(MeadowAvatarSettings.Definition));
            public static readonly StateType SlugcatAvatarSettingsDefinition = new ("SlugcatAvatarSettings.Definition", typeof(StoryClientSettings.Definition));
            public static readonly StateType ArenaAvatarSettingsDefinition = new("ArenaAvatarSettings.Definition", typeof(ArenaClientSettings.Definition));

            public static readonly StateType DeflateState = new("DeflateState", typeof(DeflateState)); // used in serializer for wrapping large states
        }

        public static OnlineState ParsePolymorph(Serializer serializer)
        {
            return handlersByEnum[new StateType(StateType.values.GetEntry(serializer.reader.ReadByte()))].factory();
        }

        public void WritePolymorph(Serializer serializer)
        {
            serializer.writer.Write((byte)handler.stateType.index);
        }

        private static Dictionary<StateType, StateHandler> handlersByEnum = new Dictionary<StateType, StateHandler>();
        private static Dictionary<Type, StateHandler> handlersByType = new Dictionary<Type, StateHandler>();
        
        public static void RegisterState(StateType stateType, Type type)
        {
            if (!handlersByEnum.ContainsKey(stateType)) { handlersByEnum[stateType] = handlersByType[type] = new StateHandler(stateType, type); }
        }
        internal static void InitializeBuiltinTypes()
        {
            _ = StateType.Unknown; // runs static init
        }

        public virtual void CustomSerialize(Serializer serializer)
        {
            try
            {
                long wasPos = serializer.Position;
                handler.serialize(this, serializer);
                RainMeadow.Trace($"{this} (delta?:{isDelta}) took {serializer.Position - wasPos}");
            }
            catch (Exception e)
            {
                RainMeadow.Error($"Error serializing {this.GetType()}");
                RainMeadow.Error(e);
                throw;
            }
        }

        public virtual OnlineState Delta(OnlineState baseline)
        {
            if (baseline == null) throw new ArgumentNullException();
            if (baseline.isDelta) throw new InvalidProgrammerException("baseline is delta");
            if (isDelta) throw new InvalidProgrammerException("self is delta");
            try
            {
                var result = handler.delta(this, baseline);
                if (!result.isDelta) throw new InvalidProgrammerException("did not produce a delta");
                return result;
            }
            catch (Exception e)
            {
                RainMeadow.Error($"Error Delta {this.GetType()}");
                RainMeadow.Error(e);
                throw;
            }
        }

        public virtual OnlineState ApplyDelta(OnlineState incoming)
        {
            if (incoming == null) throw new ArgumentNullException();
            if (!incoming.isDelta) throw new InvalidProgrammerException("incoming not delta");
            if (isDelta) throw new InvalidProgrammerException("self is delta");
            try
            {
                var result = handler.applydelta(this, incoming);
                if (result.isDelta) throw new InvalidProgrammerException("produced a delta");
                return result;
            }
            catch (Exception e)
            {
                RainMeadow.Error($"Error ApplyDelta {this.GetType()}");
                RainMeadow.Error(e);
                throw;
            }
        }

        public class OnlineFieldAttribute : Attribute
        {
            public string group; // things on the same group are gruped in deltas, saving bytes
            public bool nullable; // field can be null
            public bool polymorphic; // type of field needs to be serialized/parsed
            public bool always; // field is always sent, to be used as key

            public OnlineFieldAttribute(string group = "default", bool nullable = false, bool polymorphic = false, bool always = false)
            {
                this.group = group;
                this.nullable = nullable;
                this.polymorphic = polymorphic;
                this.always = always;
            }

            public virtual Expression SerializerCallMethod(FieldInfo f, Expression serializerRef, Expression fieldRef)
            {
                return Expression.Call(serializerRef, Serializer.GetSerializationMethod(f.FieldType, nullable, polymorphic), fieldRef);
            }

            public virtual Expression ComparisonMethod(FieldInfo f, MemberExpression currentField, MemberExpression baselineField)
            {
                return Expression.Equal(currentField, baselineField);
            }
        }

        public class OnlineFieldHalf : OnlineFieldAttribute
        {

            public OnlineFieldHalf(string group = "default", bool nullable = false, bool polymorphic = false, bool always = false) : base(group, nullable, polymorphic, always) { }
            public override Expression SerializerCallMethod(FieldInfo f, Expression serializerRef, Expression fieldRef)
            {
                return Expression.Call(serializerRef, typeof(Serializer).GetMethods().First(m => m.Name == nameof(Serializer.SerializeHalf) && m.GetParameters()[0].ParameterType == typeof(float).MakeByRefType()), fieldRef);
            }
        }
        public class OnlineFieldColorRgbAttribute : OnlineFieldAttribute
        {
            public OnlineFieldColorRgbAttribute(string group = "default", bool nullable = false, bool polymorphic = false, bool always = false) : base(group, nullable, polymorphic, always) { }
            public override Expression SerializerCallMethod(FieldInfo f, Expression serializerRef, Expression fieldRef)
            {
                return Expression.Call(serializerRef, typeof(Serializer).GetMethods().First(m => m.Name == nameof(Serializer.SerializeRGB) && m.GetParameters()[0].ParameterType == typeof(Color).MakeByRefType()), fieldRef);
            }
        }

        public class DeltaSupportAttribute : Attribute { public StateHandler.DeltaSupport level; }

        public class StateHandler
        {
            public OnlineState.StateType stateType;
            public Type type;
            public DeltaSupport deltaSupport;
            public Func<OnlineState> factory;
            public Action<OnlineState, Serializer> serialize;
            public Func<OnlineState, OnlineState, OnlineState> delta;
            public Func<OnlineState, OnlineState, OnlineState> applydelta;
            public int ngroups;

            public enum DeltaSupport
            {
                None,
                FollowsContainer,
                NullableDelta,
                Full
            }

            // Welcome to Expression Trees hell
            public StateHandler(OnlineState.StateType stateType, Type type)
            {
                RainMeadow.Debug($"Registering " + type.FullName);
                try
                {
                    if (!type.IsValueType && !type.IsClass) throw new InvalidProgrammerException("not class or struct");
                    this.stateType = stateType;
                    this.type = type;
                    this.deltaSupport = type.GetCustomAttribute<DeltaSupportAttribute>()?.level ?? DeltaSupport.None;

                    if (deltaSupport == DeltaSupport.None) RainMeadow.Error("No delta support for type: " + type.Name);

                    BindingFlags anyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                    var tresults = new List<FieldInfo>();
                    var t = type; while (t != null) { tresults.AddRange(t.GetFields(anyInstance | BindingFlags.DeclaredOnly).Where(f => f.GetCustomAttribute<OnlineFieldAttribute>() != null)); t = t.BaseType; }
                    var fields = tresults.ToArray();
                    RainMeadow.Debug($"found {fields.Length} fields");
                    if (fields.Length > 0) RainMeadow.Debug(fields.Select(f => $"{f.FieldType.Name} {f.Name}").Aggregate((a, b) => a + "\n" + b));
                    else throw new InvalidProgrammerException($"Type {type} has no online fields");
                    var keys = fields.Where(o => o.GetCustomAttribute<OnlineFieldAttribute>().always).ToList();
                    Dictionary<string, List<FieldInfo>> deltaGroups = fields.Where(o => !o.GetCustomAttribute<OnlineFieldAttribute>().always).GroupBy(o => o.GetCustomAttribute<OnlineFieldAttribute>().group).ToDictionary(g => g.Key, g => g.ToList());
                    ngroups = deltaGroups.Count;
                    RainMeadow.Debug($"found {ngroups} groups");

                    var valueFlagsAcessor = typeof(OnlineState).GetField("valueFlags", anyInstance);
                    var isDeltaAcessor = typeof(OnlineState).GetField("isDelta", anyInstance);
                    var baselineAcessor = typeof(RootDeltaState).GetField("baseline", anyInstance);
                    var tickAcessor = typeof(RootDeltaState).GetField("tick", anyInstance);
                    var serializerIsDeltaAcessor = typeof(Serializer).GetField("IsDelta", anyInstance);
                    var serializeBoolRef = typeof(Serializer).GetMethod("Serialize", new[] { typeof(bool).MakeByRefType() });
                    var serializeUintRef = typeof(Serializer).GetMethod("Serialize", new[] { typeof(uint).MakeByRefType() });

                    var expressions = new List<Expression>();

                    // make factory

                    factory = Expression.Lambda<Func<OnlineState>>(Expression.New(type.GetConstructor(new Type[] { }))).Compile();

                    // make serialize func

                    ParameterExpression self = Expression.Parameter(typeof(OnlineState), "self");
                    ParameterExpression selfConverted = Expression.Variable(type, "selfConverted");
                    ParameterExpression serializer = Expression.Parameter(typeof(Serializer), "serializer");

                    MemberExpression serializerIsDelta = Expression.Field(serializer, serializerIsDeltaAcessor);
                    MemberExpression selfIsDelta = Expression.Field(self, isDeltaAcessor);

                    expressions = new List<Expression>();
                    expressions.Add(Expression.Assign(selfConverted, Expression.Convert(self, type)));

                    switch (deltaSupport)
                    {
                        case DeltaSupport.None:
                            // serializer.Serialize(ref field);
                            expressions.Add(Expression.Block(fields.Select(
                                f => f.GetCustomAttribute<OnlineFieldAttribute>().SerializerCallMethod(f, serializer, Expression.Field(selfConverted, f))
                            ).Where(e => e != null)));
                            break;
                        case DeltaSupport.FollowsContainer:
                            // Main delta-aware serialization logic here

                            // for each group
                            //      if (serializer.IsDelta) serializer.Serialize(ref hasGroupValue[n]);
                            //      if (!serializer.IsDelta || hasGroupValue[n])
                            //      {
                            //          serializer.Serialize(ref fieldInGroup);
                            //      }

                            expressions.Add(Expression.Assign(selfIsDelta, serializerIsDelta));

                            // always send
                            if (keys.Count > 0)
                            {
                                expressions.Add(Expression.Block(keys.Select(
                                        f => f.GetCustomAttribute<OnlineFieldAttribute>().SerializerCallMethod(f, serializer, Expression.Field(selfConverted, f))
                                    ).Where(e => e != null)));
                            }

                            for (int i = 0; i < ngroups; i++)
                            {
                                if (deltaGroups[deltaGroups.Keys.ToList()[i]].Count == 0) continue;

                                expressions.Add(Expression.IfThen(serializerIsDelta,
                                    Expression.Call(serializer, serializeBoolRef, new[] {
                                Expression.ArrayAccess(Expression.Field(selfConverted, valueFlagsAcessor), Expression.Constant(i)) })));
                                var deltagroupname = deltaGroups.Keys.ToList()[i];
                                expressions.Add(Expression.IfThen(Expression.OrElse(Expression.Not(serializerIsDelta),
                                        Expression.ArrayAccess(Expression.Field(selfConverted, valueFlagsAcessor), Expression.Constant(i))),
#if TRACING
                                    Expression.Block(Expression.Invoke(Expression.Constant((Serializer s) => { if (s.IsWriting) RainMeadow.Trace("sending " + deltagroupname); }), serializer),
#endif                                   
                                    Expression.Block(deltaGroups[deltaGroups.Keys.ToList()[i]].Select(
                                        f =>
#if TRACING
                                            Expression.Block(Expression.Invoke(Expression.Constant((Serializer s) => { if (s.IsWriting) RainMeadow.Trace(f.Name); }), serializer),
#endif
                                            f.GetCustomAttribute<OnlineFieldAttribute>().SerializerCallMethod(f, serializer, Expression.Field(selfConverted, f))
#if TRACING
                                        )
#endif
                                    ).Where(e => e != null))
#if TRACING
                                    )
#endif
                                    ));

                            }
                            break;
                        case DeltaSupport.NullableDelta:
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
                            expressions.Add(Expression.IfThen(serializerIsDelta, Expression.Block(
                                    Expression.Call(serializer, serializeBoolRef, new[] { selfIsDelta }),
                                    Expression.Assign(serializerIsDelta, selfIsDelta
                                ))));
                            goto case DeltaSupport.FollowsContainer;
                        case DeltaSupport.Full:
                            // serializer.Serialize(ref IsDelta);
                            // if (!serializer.IsDelta && IsDelta) { serializer.Serialize(ref Baseline); }
                            // serializer.IsDelta = IsDelta; // Serializer wraps this call and restores the previous value later
                            // ...etc
                            expressions.Add(Expression.Call(serializer, serializeBoolRef, new[] { selfIsDelta }));
                            expressions.Add(Expression.IfThen(Expression.AndAlso(Expression.Not(serializerIsDelta),
                                                                     selfIsDelta),
                                        Expression.Call(serializer, serializeUintRef, new[] { Expression.Field(selfConverted, baselineAcessor) })));
                            expressions.Add(Expression.Assign(serializerIsDelta, selfIsDelta));
                            goto case DeltaSupport.FollowsContainer;
                    }

                    serialize = Expression.Lambda<Action<OnlineState, Serializer>>(Expression.Block(new[] { selfConverted }, expressions), self, serializer).Compile();

                    // if supports delta
                    if (deltaSupport != DeltaSupport.None)
                    {
                        // make delta func

                        ParameterExpression baseline = Expression.Parameter(typeof(OnlineState), "baseline");
                        ParameterExpression baselineConverted = Expression.Variable(type, "baselineConverted");
                        ParameterExpression output = Expression.Variable(type, "output");
                        MethodInfo cloneRef = typeof(OnlineState).GetMethod("Clone", anyInstance);

                        // if (baseline == null) throw new InvalidProgrammerException("baseline is null");
                        // **if (baseline.IsDelta) throw new InvalidProgrammerException("baseline is delta");
                        // var output = DeepCopy();
                        // **output.IsDelta = true;
                        // **output.baseline = baseline.tick;
                        // 
                        // output.fieldWithDelta = this.fieldWithDelta?.Delta(baseline.fieldWithDelta);
                        // 
                        // output.hasGroupValue = field != baseline.field || field2 != baseline.field2;

                        expressions = new List<Expression>();
                        // todo arg checking
                        expressions.Add(Expression.Assign(selfConverted, Expression.Convert(self, type)));
                        expressions.Add(Expression.Assign(baselineConverted, Expression.Convert(baseline, type)));
                        expressions.Add(Expression.Assign(output, Expression.Convert(Expression.Call(selfConverted, cloneRef), type)));

                        expressions.Add(Expression.Assign(Expression.Field(output, isDeltaAcessor), Expression.Constant(true)));
                        if (deltaSupport == DeltaSupport.Full)
                            expressions.Add(Expression.Assign(Expression.Field(output, baselineAcessor), Expression.Field(baselineConverted, tickAcessor)));

                        foreach (var f in fields)
                        {
                            // fields have already been copied, this is for sub-deltas

                            if (f.FieldType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Generics.IDelta<>)))
                            {
                                // IPrimaryDelta:   o.f = this.f ? (b.f ? this.f.delta(b.f) : this.f) : null // can this be simplified?
                                //                        b.f != null ? f?.delta(b.f) : f;
                                // IDelta:          o.f = this.f?.delta(b.f)
                                expressions.Add(Expression.Assign(Expression.Field(output, f),
                                            Expression.Condition(Expression.Equal(Expression.Field(selfConverted, f), Expression.Constant(null, f.FieldType)),
                                                Expression.Constant(null, f.FieldType),
                                                (f.FieldType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Generics.IPrimaryDelta<>))) ?
                                                    Expression.Condition(Expression.Equal(Expression.Field(baselineConverted, f), Expression.Constant(null, f.FieldType)),
                                                    Expression.Field(selfConverted, f),
                                                    Expression.Convert(Expression.Call(Expression.Field(selfConverted, f), f.FieldType.GetMethod("Delta"), Expression.Field(baselineConverted, f)), f.FieldType))
                                                : Expression.Convert(Expression.Call(Expression.Field(selfConverted, f), f.FieldType.GetMethod("Delta"), Expression.Field(baselineConverted, f)), f.FieldType)
                                                )
                                           ));
                            }
                        }

                        // set flags for sent/omitted fields
                        for (int i = 0; i < ngroups; i++)
                        {
                            if (deltaGroups[deltaGroups.Keys.ToList()[i]].Count == 0) continue;
                            // valueFlags[i] = self.f != baseline.f || self.f2 != baseline.f2 || ...
                            expressions.Add(Expression.Assign(Expression.ArrayAccess(Expression.Field(output, valueFlagsAcessor), Expression.Constant(i)),
                                OrAny(deltaGroups[deltaGroups.Keys.ToList()[i]].Select(
                                        f => Expression.Not(
                                            (f.FieldType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IPrimaryDelta<>))) ?
                                                // (this.f && b.f) ? delta.f.isempty : (this.f == b.f)
                                                Expression.Condition(Expression.AndAlso(
                                                    Expression.NotEqual(Expression.Field(selfConverted, f), Expression.Constant(null, f.FieldType)),
                                                    Expression.NotEqual(Expression.Field(baselineConverted, f), Expression.Constant(null, f.FieldType))
                                                    ),
                                                    Expression.Call(Expression.Field(output, f), f.FieldType.GetProperty("IsEmptyDelta").GetMethod),
                                                    Expression.Equal(Expression.Field(selfConverted, f), Expression.Field(baselineConverted, f))
                                                )
                                            : (f.FieldType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDelta<>))) ?
                                                Expression.Equal(Expression.Field(output, f), Expression.Constant(null, f.FieldType))
                                            : f.GetCustomAttribute<OnlineFieldAttribute>().ComparisonMethod(f, Expression.Field(selfConverted, f), Expression.Field(baselineConverted, f))
                                            )
                                    ).Where(e => e != null).ToArray())
                                ));
#if TRACING
                            expressions.Add(Expression.Block(
                                deltaGroups[deltaGroups.Keys.ToList()[i]].Select(
                                        f => Expression.Invoke(Expression.Constant((string n, bool v) =>
                                        {
                                            RainMeadow.Trace(n + " has changed?: " + v);
                                        }), Expression.Constant(f.Name), Expression.Not(
                                            (f.FieldType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IPrimaryDelta<>))) ?
                                                // (this.f && b.f) ? delta.f.isempty : (this.f == b.f)
                                                Expression.Condition(Expression.AndAlso(
                                                    Expression.NotEqual(Expression.Field(selfConverted, f), Expression.Constant(null, f.FieldType)),
                                                    Expression.NotEqual(Expression.Field(baselineConverted, f), Expression.Constant(null, f.FieldType))
                                                    ),
                                                    Expression.Call(Expression.Field(output, f), f.FieldType.GetProperty("IsEmptyDelta").GetMethod),
                                                    Expression.Equal(Expression.Field(selfConverted, f), Expression.Field(baselineConverted, f))
                                                )
                                            : (f.FieldType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDelta<>))) ?
                                                Expression.Equal(Expression.Field(output, f), Expression.Constant(null, f.FieldType))
                                            : f.GetCustomAttribute<OnlineFieldAttribute>().ComparisonMethod(f, Expression.Field(selfConverted, f), Expression.Field(baselineConverted, f))
                                            )
                                    )
                                )));
                            var deltagroupname = deltaGroups.Keys.ToList()[i];
                            expressions.Add(Expression.Invoke(Expression.Constant((bool v) =>
                            {
                                RainMeadow.Trace(deltagroupname + " has value?: " + v);
                            }), Expression.ArrayAccess(Expression.Field(output, valueFlagsAcessor), Expression.Constant(i))));
#endif
                        }

                        expressions.Add(output); // return

                        delta = Expression.Lambda<Func<OnlineState, OnlineState, OnlineState>>(Expression.Block(new[] { selfConverted, baselineConverted, output }, expressions), self, baseline).Compile();

                        // make applydelta func

                        ParameterExpression incoming = Expression.Parameter(typeof(OnlineState));
                        ParameterExpression incomingConverted = Expression.Variable(type);
                        // self is baseline
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
                        expressions.Add(Expression.Assign(output, Expression.Convert(Expression.Call(selfConverted, cloneRef), type)));

                        if (deltaSupport != DeltaSupport.FollowsContainer && deltaSupport != DeltaSupport.NullableDelta)
                        {
                            expressions.Add(Expression.Assign(Expression.Field(output, tickAcessor), Expression.Field(incomingConverted, tickAcessor)));
                        }

                        for (int i = 0; i < ngroups; i++)
                        {
                            if (deltaGroups[deltaGroups.Keys.ToList()[i]].Count == 0) continue;

                            expressions.Add(Expression.IfThen(Expression.ArrayAccess(Expression.Field(incomingConverted, valueFlagsAcessor), Expression.Constant(i)),
                                    Expression.Block(deltaGroups[deltaGroups.Keys.ToList()[i]].Select(

                                        // regular:         o.f = i.f;
                                        // IPrimaryDelta:   o.f = f ? (i.f ? f.applydelta(i.f) : null) : i.f
                                        // IDelta:          o.f = f ? f.applydelta(i.f) : i.f

                                        f => Expression.Assign(Expression.Field(output, f),
                                            (f.FieldType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Generics.IDelta<>))) ?
                                            Expression.Condition(Expression.Equal(Expression.Field(selfConverted, f), Expression.Constant(null, f.FieldType)),
                                                Expression.Field(incomingConverted, f),
                                                (f.FieldType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(Generics.IPrimaryDelta<>))) ?
                                                    Expression.Condition(Expression.Equal(Expression.Field(incomingConverted, f), Expression.Constant(null, f.FieldType)),
                                                    Expression.Constant(null, f.FieldType),
                                                    Expression.Convert(Expression.Call(Expression.Field(selfConverted, f), f.FieldType.GetMethod("ApplyDelta"), Expression.Field(incomingConverted, f)), f.FieldType))
                                                : Expression.Convert(Expression.Call(Expression.Field(selfConverted, f), f.FieldType.GetMethod("ApplyDelta"), Expression.Field(incomingConverted, f)), f.FieldType)
                                                )
                                            : Expression.Field(incomingConverted, f) // regular
                                        )
                                    ))));
                        }

                        expressions.Add(output); // return

                        RainMeadow.Debug(Expression.Block(new[] { selfConverted, incomingConverted, output }, expressions).ToString());
                        RainMeadow.Debug(Expression.Lambda<Func<OnlineState, OnlineState, OnlineState>>(Expression.Block(new[] { selfConverted, incomingConverted, output }, expressions), self, incoming).ToString());
                        applydelta = Expression.Lambda<Func<OnlineState, OnlineState, OnlineState>>(Expression.Block(new[] { selfConverted, incomingConverted, output }, expressions), self, incoming).Compile();
                    }
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                    throw;
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