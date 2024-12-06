using System;
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
            internal bool isStatic;
            public string summary;
        }

        public static void SetupRPCs()
        {
            index = 1; // zero is an easy to catch mistake

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().ToList())
            {
                try
                {
                    foreach (var type in assembly.GetTypes().ToList())
                    {
                        RegisterRPCs(type);
                    }
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                }
            }
        }

        static ushort index;
        static ParameterExpression rpceventParam = Expression.Parameter(typeof(RPCEvent), "rpcEvent");
        static ParameterExpression serializerParam = Expression.Parameter(typeof(Serializer), "serializer");

        static ParameterExpression argsVar = Expression.Variable(typeof(object[]), "args");
        static FieldInfo rpcEventTargetAccessor = typeof(RPCEvent).GetField(nameof(RPCEvent.target));
        static FieldInfo rpcEventArgsAccessor = typeof(RPCEvent).GetField(nameof(RPCEvent.args));
        static PropertyInfo serializerIsReadingProp = typeof(Serializer).GetProperty(nameof(Serializer.IsReading));
        static MethodInfo serializeResourceByRef = typeof(Serializer).GetMethod(nameof(Serializer.SerializeResourceByReference));
        static MethodInfo serializeEntityById = typeof(Serializer).GetMethod(nameof(Serializer.SerializeEntityById));

        public static void RegisterRPCs(Type targetType)
        {
            if (targetType.IsGenericTypeDefinition || targetType.IsInterface) return;
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
                        RainMeadow.Debug("target is OnlineResource");
                        expressions.Add(Expression.Call(serializerParam, serializeResourceByRef.MakeGenericMethod(targetType), targetVar));
                    }
                    else if (typeof(OnlineEntity).IsAssignableFrom(targetType))
                    {
                        RainMeadow.Debug("target is OnlineEntity");
                        expressions.Add(Expression.Call(serializerParam, serializeEntityById.MakeGenericMethod(targetType), targetVar));
                    }
                    else if (typeof(RainMeadow).IsAssignableFrom(targetType)) // lambdas aren't static lol
                    {
                        RainMeadow.Debug("target is RainMeadow");
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
                        var serializerMethod = Serializer.GetSerializationMethod(argType, !argType.IsValueType || Nullable.GetUnderlyingType(argType) != null, true, true);
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

        internal static RPCEvent BuildRPC(Delegate del, object[] args)
        {
            RainMeadow.Debug($"Sending RPC: {del.Method}");
            return new RPCEvent(del, args);
        }
    }

    public class RPCEvent : OnlineEvent, ResolvableEvent
    {
        public static RPCEvent? currentRPCEvent;

        public RPCManager.RPCDefinition handler;
        public object target;
        public object[] args;

        public override string ToString()
        {
            return $"{base.ToString()}:{handler.method.Name}";
        }

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
            try
            {
                handler.serialize(this, serializer);
            }
            catch (Exception)
            {
                RainMeadow.Error($"Error serializing RPC {this}");
                throw;
            }
        }

        public override void Process()
        {
            try
            {
                if (!handler.isStatic && target == null)
                {
                    RainMeadow.Error($"Target of RPC not found for " + handler.summary);
                    from.QueueEvent(new GenericResult.Error(this));
                    return;
                }

                RainMeadow.Debug($"Processing RPC: {handler.summary}");
                var useArgs = args;
                if (handler.eventArgIndex > -1)
                {
                    var newArgs = args.ToList();
                    newArgs.Insert(handler.eventArgIndex, this);
                    useArgs = newArgs.ToArray();
                }

                var nout = from.OutgoingEvents.Count;
                currentRPCEvent = this;
                var result = handler.method.Invoke(target, useArgs);
                currentRPCEvent = null;
                if (from.OutgoingEvents.Count != nout && from.OutgoingEvents.Any(e => e is GenericResult gr && gr.referencedEvent == this)) return;

                if (result is GenericResult res) from.QueueEvent(res);
                else from.QueueEvent(new GenericResult.Ok(this));
            }
            catch (Exception e)
            {
                currentRPCEvent = null;
                RainMeadow.Error($"Error handing RPC {handler.method.Name} {e}");
                from.QueueEvent(new GenericResult.Error(this));
            }
        }

        public event Action<GenericResult> OnResolve;

        internal RPCEvent Then(Action<GenericResult> onResolve)
        {
            this.OnResolve += onResolve;
            return this;
        }

        public void Resolve(GenericResult genericResult)
        {
            try
            {
                this.OnResolve?.Invoke(genericResult);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
            }
        }

        public override void Abort()
        {
            base.Abort();
            this.Resolve(new GenericResult.Error(this));
        }

        public bool IsIdentical(Delegate del, params object[] args)
        {
            return handler.method == del.Method && this.target == del.Target && this.args.SequenceEqual(args);
        }
    }
}
