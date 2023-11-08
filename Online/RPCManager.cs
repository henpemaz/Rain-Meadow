using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RainMeadow
{
    // what sort of options are needed here?
    // "if target owner only"
    // ""
    public class RPCMethodAttribute : Attribute
    {

    }

    // todo
    // support nullable parameters
    // support enumext (serializer finder pick right method)
    // support polymorphism of entity target
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
        }

        public static void SetupRPCs()
        {
            index = 1; // zero is an easy to catch mistake
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                RegisterRPCs(type);
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
        static MethodInfo serializeEntityById = typeof(Serializer).GetMethod(nameof(Serializer.SerializEntityById));

        public static void RegisterRPCs(Type targetType)
        {
            var methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance| BindingFlags.Static | BindingFlags.DeclaredOnly).Where(m => m.GetCustomAttribute<RPCMethodAttribute>() != null);
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
                    if(args[i].ParameterType == typeof(RPCEvent))
                    {
                        argsEventIndex = i;
                        var tempArgs = args.ToList();
                        tempArgs.RemoveAt(i);
                        args = tempArgs.ToArray();
                    }
                }
                RainMeadow.Debug(args.Aggregate("", (e, a) => e + " - " + a));

                // make serialize method
                // uses rpcEvent, serializer

                ParameterExpression targetVar = Expression.Variable(targetType, "target");
                var expressions = new List<Expression>();
                var vars = new List<ParameterExpression>()
                {
                    targetVar, argsVar,
                };

                if (targetType != null && !isStatic)
                {
                    expressions.Add(Expression.Assign(targetVar, Expression.Convert(Expression.Field(rpceventParam, rpcEventTargetAccessor), targetType)));
                    // todo
                    // serialize the target type
                    // for Resource or Entity we know what to do
                    if (typeof(OnlineResource).IsAssignableFrom(targetType))
                    {
                        expressions.Add(Expression.Call(serializerParam, serializeResourceByRef, targetVar));
                    }
                    else if( typeof(OnlineEntity).IsAssignableFrom(targetType))
                    {
                        expressions.Add(Expression.Call(serializerParam, serializeEntityById, targetVar));
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
                    expressions.Add(Expression.IfThen(Expression.Property(serializerParam, serializerIsReadingProp),
                        Expression.Assign(argsVar, Expression.NewArrayBounds(typeof(object), Expression.Constant(args.Length)))
                      ));
                    expressions.Add(Expression.Assign(Expression.Field(rpceventParam, rpcEventArgsAccessor), argsVar));

                    for (int i = 0; i < args.Length; i++)
                    {
                        // T arg = (T)args[i];
                        // serializer.Serialize(ref arg);
                        // args[i] = (object)arg;

                        var argType = args[i].ParameterType;
                        ParameterExpression argVar = Expression.Variable(argType);
                        vars.Add(argVar);
                        expressions.Add(Expression.Assign(argVar, Expression.Convert(Expression.ArrayAccess(argsVar, Expression.Constant(i)), argType)));
                        var serializerMethod = Serializer.GetSerializationMethod(argType, false, false);
                        if(serializerMethod == null)
                        {
                            throw new NotSupportedException($"can't serialize parameter {args[i].ParameterType} on type {targetType}");
                        }
                        expressions.Add(Expression.Call(serializerParam, serializerMethod, argVar));
                        expressions.Add(Expression.Assign(Expression.ArrayAccess(argsVar, Expression.Constant(i)), Expression.Convert(argVar, typeof(object))));
                    }
                }
                else
                {
                    expressions.Add(Expression.IfThen(Expression.Property(serializerParam, serializerIsReadingProp),
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
                    isStatic = isStatic
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
            RainMeadow.Debug($"Processing RPC: {handler.method}");
            if (handler.eventArgIndex > -1)
            {
                var newArgs = args.ToList();
                newArgs.Insert(handler.eventArgIndex, this);
                args = newArgs.ToArray();
            }

            // todo "must be owner" support and similar for simplicity

            var result = handler.method.Invoke(target, args);

            if (result is GenericResult res) from.QueueEvent(res);
            else from.QueueEvent(new GenericResult.Ok(this));
        }

        public event Action<GenericResult> OnResolve;

        internal OnlineEvent Then(Action<GenericResult> onResolve)
        {
            this.OnResolve += onResolve;
            return this;
        }

        public void Resolve(GenericResult genericResult)
        {
            this.OnResolve?.Invoke(genericResult);
        }

        internal bool IsIdentical(Delegate del, params object[] args)
        {
            return handler.method == del.Method && this.target == del.Target && this.args.SequenceEqual(args);
        }
    }
}