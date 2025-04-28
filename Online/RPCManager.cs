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
    // "only from lobby owner"
    // "only from target owner"

    /// <summary>
    /// RPCs, efficient but require modsync
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RPCMethodAttribute : Attribute
    {
        public bool runDeferred; // run after state is processed, at end of network-frame
    }

    /// <summary>
    /// RPCs for non-modsync-required mods, less efficient
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SoftRPCMethodAttribute : RPCMethodAttribute
    {
        
    }

    public static class RPCManager
    {
        public static Dictionary<ushort, RPCDefinition> defsByIndex = new Dictionary<ushort, RPCDefinition>();
        public static Dictionary<MethodInfo, RPCDefinition> defsByMethod = new Dictionary<MethodInfo, RPCDefinition>();
        public static Dictionary<Guid, Dictionary<ushort, SoftRPCDefinition>> softHandlerClients = new();


        public class RPCDefinition
        {
            public ushort index;
            public MethodInfo method;
            public Action<RPCEvent, Serializer> serialize;
            public int eventArgIndex;
            public bool isStatic;
            public string summary;
            public bool runDeferred;
        }

        public class SoftRPCDefinition : RPCDefinition
        {
            public Guid key;
        }

        public static void SetupRPCs()
        {
            index = 1; // zero is an easy to catch mistake

            // our own RPCs first
            foreach (var type in Assembly.GetExecutingAssembly().GetTypesSafely().ToList())
            {
                try
                {
                    RegisterRPCs(type);
                }
                catch (Exception e)
                {
                    RainMeadow.Error("Error registering RPCs for builtin type: " + type.FullName);
                    throw e;
                }
            }
            // intentionally thrown on failure

            // other RPCs
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().ToList())
            {
                if (assembly == Assembly.GetExecutingAssembly()) continue;
                try
                {
                    foreach (var type in assembly.GetTypesSafely().ToList())
                    {
                        try
                        {
                            RegisterRPCs(type);
                        }
                        catch (Exception e)
                        {
                            RainMeadow.Error(assembly.FullName + ":" + type.FullName);
                            RainMeadow.Error(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                }
            }
            // there's an argument that throwing would be necessary to avoid offset indexes between clients
            // but errors seemed fairly deterministic,mostly interop-related types failing to load, solved by adding interops to high-impact
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
                var isSoft = method.GetCustomAttribute<RPCMethodAttribute>() is SoftRPCMethodAttribute;
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
                        var serializerMethod = Serializer.GetSerializationMethod(argType, !(argType.IsValueType || (argType.IsArray && argType.GetElementType().IsValueType)) || Nullable.GetUnderlyingType(argType) != null, true, true); if (serializerMethod == null)
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

                if (isSoft)
                {
                    var key = targetType.Module.ModuleVersionId;
                    if (!softHandlerClients.ContainsKey(key)) softHandlerClients.Add(key, new Dictionary<ushort, SoftRPCDefinition>());
                    ushort useIndex = (ushort)softHandlerClients[key].Keys.Count;
                    SoftRPCDefinition entry = new()
                    {
                        key = key,
                        index = useIndex,
                        method = method,
                        serialize = serialize,
                        eventArgIndex = argsEventIndex,
                        isStatic = isStatic,
                        runDeferred = method.GetCustomAttribute<RPCMethodAttribute>().runDeferred,
                        summary = $"{targetType.Name}.{method.Name}"
                    };
                    defsByMethod[method] = entry;
                    softHandlerClients[key][useIndex] = entry;
                }
                else
                {
                    RPCDefinition entry = new()
                    {
                        index = index,
                        method = method,
                        serialize = serialize,
                        eventArgIndex = argsEventIndex,
                        isStatic = isStatic,
                        runDeferred = method.GetCustomAttribute<RPCMethodAttribute>().runDeferred,
                        summary = $"{targetType.Name}.{method.Name}"
                    };

                    defsByIndex[index] = entry;
                    defsByMethod[method] = entry;
                    index++;
                }
            }
        }

        internal static RPCEvent BuildRPC(Delegate del, object[] args)
        {
            RainMeadow.Debug($"Sending RPC: {del.Method}");
            var handler = defsByMethod[del.Method];
            if (handler is SoftRPCDefinition) return new SoftRPCEvent(handler, del, args);
            return new RPCEvent(handler, del, args);
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
            return $"{base.ToString()}:{handler.summary}";
        }

        public RPCEvent() { }

        public RPCEvent(RPCManager.RPCDefinition handler, Delegate del, object[] args)
        {
            this.handler = handler;
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
                handler = GetHandler(serializer.reader.ReadUInt16());
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
        protected virtual RPCManager.RPCDefinition GetHandler(ushort index)
        {
            return RPCManager.defsByIndex[index];
        }

        override public bool runDeferred => handler.runDeferred;

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
                RainMeadow.Error($"Error handing RPC {handler.summary} {e}");
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

    public class SoftRPCEvent : RPCEvent
    {
        static RPCManager.SoftRPCDefinition softFallbackHandler = new RPCManager.SoftRPCDefinition() { summary = "NOT FOUND" };

        RPCManager.SoftRPCDefinition softHandler => handler as RPCManager.SoftRPCDefinition;

        public SoftRPCEvent() : base() { }

        public SoftRPCEvent(RPCManager.RPCDefinition handler, Delegate del, object[] args) : base(handler, del, args) { }

        public override EventTypeId eventType => EventTypeId.SoftRPCEvent;

        public override void CustomSerialize(Serializer serializer)
        {
            if (serializer.IsWriting)
            {
                serializer.writer.Write(softHandler.key.ToByteArray());

                long startPos = serializer.Position;
                serializer.writer.Write((ushort)0);

                base.CustomSerialize(serializer);

                var endPos = serializer.Position;
                serializer.stream.Position = startPos;
                serializer.writer.Write((ushort)(endPos - startPos - 4)); // skip lenght and eventId
                serializer.stream.Position = endPos;
            }
            else
            {
                readKey = new Guid(serializer.reader.ReadBytes(16));
                var lenght = serializer.reader.ReadUInt16();
                if (!RPCManager.softHandlerClients.ContainsKey(readKey))
                {
                    handler = softFallbackHandler;
                    eventId = serializer.reader.ReadUInt16(); // still required for skip/ack systems
                    serializer.stream.Seek(lenght, System.IO.SeekOrigin.Current);
                    return;
                }
                base.CustomSerialize(serializer);
            }
        }

        public override void Process()
        {
            if (handler == softFallbackHandler)
            {
                RainMeadow.Debug("Skipping unknown SoftRPC");
                return;
            }
            base.Process();
        }

        private Guid readKey;
        protected override RPCManager.RPCDefinition GetHandler(ushort index)
        {
            return RPCManager.softHandlerClients[readKey][index];
        }
    }
}
