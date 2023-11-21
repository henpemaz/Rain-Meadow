﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RainMeadow
{
    // what sort of options are needed here?
    // "if target owner only"
    // "if resource active"
    [AttributeUsage(AttributeTargets.Method)]
    public class RPCMethodAttribute : Attribute
    {

    }

    public static class RPCManager
    {
        public static Dictionary<ushort, RPCDefinition> defsByIndex = new Dictionary<ushort, RPCDefinition>();
        public static Dictionary<MethodInfo, RPCDefinition> defsByMethod = new Dictionary<MethodInfo, RPCDefinition>();


        public class RPCDefinition
        {
            public ushort index;
            public MethodInfo method;
            public Action<RPCEvent, Serializer> serialize;
            public int eventArgIndex;
            public bool isStatic;
            public string summary;
        }

        public static void SetupRPCs()
        {
            index = 1; // zero is an easy to catch mistake
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                RegisterRPCs(type);
            }
        }

        private static ushort index;
        private static ParameterExpression rpceventParam = Expression.Parameter(typeof(RPCEvent), "rpcEvent");
        private static ParameterExpression serializerParam = Expression.Parameter(typeof(Serializer), "serializer");
        private static ParameterExpression argsVar = Expression.Variable(typeof(object[]), "args");
        private static FieldInfo rpcEventTargetAccessor = typeof(RPCEvent).GetField(nameof(RPCEvent.target));
        private static FieldInfo rpcEventArgsAccessor = typeof(RPCEvent).GetField(nameof(RPCEvent.args));
        private static PropertyInfo serializerIsReadingProp = typeof(Serializer).GetProperty(nameof(Serializer.IsReading));
        private static MethodInfo serializeResourceByRef = typeof(Serializer).GetMethod(nameof(Serializer.SerializeResourceByReference));
        private static MethodInfo serializeEntityById = typeof(Serializer).GetMethod(nameof(Serializer.SerializEntityById));

        public static void RegisterRPCs(Type targetType)
        {
            var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(m => m.GetCustomAttribute<RPCMethodAttribute>() != null);
            if (!methods.Any()) return;

            foreach (var method in methods)
            {
                var isStatic = method.IsStatic;
                // get args
                RainMeadow.Debug($"New RPC: {targetType}-{method.Name}");
                var args = method.GetParameters();

                var argsEventIndex = -1;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ParameterType == typeof(RPCEvent))
                    {
                        argsEventIndex = i;
                        var tempArgs = args.ToList();
                        tempArgs.RemoveAt(i);
                        args = tempArgs.ToArray();
                    }
                }
                RainMeadow.Debug(args.Aggregate("", (e, a) => e + " - " + a));

                // make serialize method(rpcEvent, serializer)
                ParameterExpression targetVar = Expression.Variable(targetType, "target");
                ParameterExpression isReading = Expression.Variable(typeof(bool), "isReading");
                var expressions = new List<Expression>();
                var vars = new List<ParameterExpression>()
                {
                    targetVar, argsVar, isReading
                };
                expressions.Add(Expression.Assign(isReading, Expression.Property(serializerParam, serializerIsReadingProp))); // minimize invokes :pensive:

                if (!isStatic)
                {
                    expressions.Add(Expression.Assign(targetVar, Expression.Convert(Expression.Field(rpceventParam, rpcEventTargetAccessor), targetType)));

                    // serialize the target type
                    // for Resource or Entity we know what to do
                    // dunno how to make this extensible, it's probably enough for now
                    if (typeof(OnlineResource).IsAssignableFrom(targetType))
                    {
                        expressions.Add(Expression.Call(serializerParam, serializeResourceByRef.MakeGenericMethod(targetType), targetVar));
                    }
                    else if (typeof(OnlineEntity).IsAssignableFrom(targetType))
                    {
                        expressions.Add(Expression.Call(serializerParam, serializeEntityById, targetVar));
                    }
                    else if (typeof(RainMeadow).IsAssignableFrom(targetType)) // lambdas aren't static lol
                    {
                        expressions.Add(Expression.Assign(targetVar, Expression.Constant(RainMeadow.instance)));
                    }
                    else
                    {
                        throw new NotSupportedException($"can't register non-static RPC on type {targetType}, poke Henpemaz about it");
                    }

                    expressions.Add(Expression.Assign(Expression.Field(rpceventParam, rpcEventTargetAccessor), targetVar));
                }

                if (args.Length > 0)
                {
                    // args = event.args;
                    // if(serializer.IsReading) args = new[x];
                    // event.args = args;
                    expressions.Add(Expression.Assign(argsVar, Expression.Field(rpceventParam, rpcEventArgsAccessor)));
                    expressions.Add(Expression.IfThen(isReading,
                        Expression.Assign(argsVar, Expression.NewArrayBounds(typeof(object), Expression.Constant(args.Length)))
                      ));
                    expressions.Add(Expression.Assign(Expression.Field(rpceventParam, rpcEventArgsAccessor), argsVar));

                    for (int i = 0; i < args.Length; i++)
                    {
                        // T arg
                        // if(isWritting) arg = (T)args[i];
                        // serializer.Serialize(ref arg);
                        // if(isReading) args[i] = (object)arg;

                        var argType = args[i].ParameterType;
                        ParameterExpression argVar = Expression.Variable(argType);
                        vars.Add(argVar);
                        expressions.Add(Expression.IfThen(Expression.Not(isReading), Expression.Assign(argVar, Expression.Convert(Expression.ArrayAccess(argsVar, Expression.Constant(i)), argType))));
                        var serializerMethod = Serializer.GetSerializationMethod(argType, !argType.IsValueType || Nullable.GetUnderlyingType(argType) != null, true);
                        if (serializerMethod == null)
                        {
                            throw new NotSupportedException($"can't serialize parameter {args[i].ParameterType} on type {method} for {targetType}");
                        }
                        expressions.Add(Expression.Call(serializerParam, serializerMethod, argVar));
                        expressions.Add(Expression.IfThen(isReading, Expression.Assign(Expression.ArrayAccess(argsVar, Expression.Constant(i)), Expression.Convert(argVar, typeof(object)))));
                    }
                }
                else
                {
                    // if(serializer.IsReading) args = new[0];
                    expressions.Add(Expression.IfThen(isReading,
                        Expression.Assign(Expression.Field(rpceventParam, rpcEventArgsAccessor), Expression.NewArrayBounds(typeof(object), Expression.Constant(0)))
                      ));
                }

                var serialize = Expression.Lambda<Action<RPCEvent, Serializer>>(Expression.Block(vars, expressions), rpceventParam, serializerParam).Compile();

                RPCDefinition entry = new()
                {
                    index = index,
                    method = method,
                    serialize = serialize,
                    eventArgIndex = argsEventIndex,
                    isStatic = isStatic,
                    summary = $"{targetType.Name}{method.Name}"
                };

                defsByIndex[index] = entry;
                defsByMethod[method] = entry;
                index++;
            }
        }

        public static RPCEvent BuildRPC(Delegate del, object[] args)
        {
            RainMeadow.Debug($"Sending RPC: {del.Method}");
            return new RPCEvent(del, args);
        }
    }

    public class RPCEvent : OnlineEvent, ResolvableEvent
    {
        public RPCManager.RPCDefinition handler;
        public object target;
        public object[] args;

        //private RPCArguments arguments;

        public RPCEvent() { }

        public RPCEvent(Delegate del, object[] args)
        {
            this.handler = RPCManager.defsByMethod[del.Method];
            this.target = del.Target;
            this.args = args;
        }

        public override EventTypeId eventType => EventTypeId.RPCEvent;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);

            if (serializer.IsWriting)
            {
                serializer.writer.Write(handler.index);
            }
            if (serializer.IsReading)
            {
                handler = RPCManager.defsByIndex[serializer.reader.ReadUInt16()];
            }

            handler.serialize(this, serializer);
        }

        public override void Process()
        {
            RainMeadow.Debug($"Processing RPC: {handler.summary}");
            if (handler.eventArgIndex > -1)
            {
                var newArgs = args.ToList();
                newArgs.Insert(handler.eventArgIndex, this);
                args = newArgs.ToArray();
            }

            var nout = from.OutgoingEvents.Count;
            var result = handler.method.Invoke(target, args);
            if (from.OutgoingEvents.Count != nout && from.OutgoingEvents.Any(e => e is GenericResult gr && gr.referencedEvent == this)) return;

            if (result is GenericResult res) from.QueueEvent(res);
            else from.QueueEvent(new GenericResult.Ok(this));
        }

        public event Action<GenericResult> OnResolve;

        public RPCEvent Then(Action<GenericResult> onResolve)
        {
            this.OnResolve += onResolve;
            return this;
        }

        public RPCEvent NotBefore(TickReference notbefore)
        {
            this.dependsOnTick = notbefore;
            return this;
        }

        public void Resolve(GenericResult genericResult)
        {
            this.OnResolve?.Invoke(genericResult);
        }

        public bool IsIdentical(Delegate del, params object[] args)
        {
            return handler.method == del.Method && this.target == del.Target && this.args.SequenceEqual(args);
        }
    }
}