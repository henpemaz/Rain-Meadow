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
    /// <summary>
    /// runDeferred: If true, the RPC is processed after states are processed (at the end of the network-frame)
    /// customIndex: The unique index of the RPC. If 0, will determine an unused index automatically.
    /// useErrorCorrection: If true, unrecognized RPCs and RPCs with erroneous data will be safely ignored. However, RPC size is limited to 255 bytes.
    /// 
    /// MOD AUTHORS: Please (PLEASE!) specify a unique customIndex that is > 1000 and use error correction!!
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RPCMethodAttribute : Attribute
    {
        public bool runDeferred; // run after state is processed, at end of network-frame
        public ushort customIndex = 0; //used to specify the index, to prevent clients from having different indices, or 0 if determined by RegisterRPCs()
        public bool optimizedRPC = false; //if false, comes with limited RPC size, an extra byte of data, and slightly slower processing
                                          //by default it is false, because skipping error correction should only be used if someone knows what he's doing
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
            public bool runDeferred;
            public bool optimizedRPC;
        }

        public static void SetupRPCs()
        {
            nextIndex = 1; // zero is an easy to catch mistake

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().ToList())
            {
                bool isMain = assembly == Assembly.GetExecutingAssembly();
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
                            if (isMain) throw e; 
                            RainMeadow.Error(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (isMain) throw e;
                    RainMeadow.Error(e);
                }
            }
        }

        static ushort nextIndex;
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
                ushort customIndex = method.GetCustomAttribute<RPCMethodAttribute>().customIndex;
                ushort index = customIndex > 0 ? customIndex : nextIndex;

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

                RPCDefinition entry = new()
                {
                    index = index,
                    method = method,
                    serialize = serialize,
                    eventArgIndex = argsEventIndex,
                    isStatic = isStatic,
                    runDeferred = method.GetCustomAttribute<RPCMethodAttribute>().runDeferred,
                    optimizedRPC = method.GetCustomAttribute<RPCMethodAttribute>().optimizedRPC,
                    summary = $"{targetType.Name}{method.Name}"
                };

                defsByIndex[index] = entry;
                defsByMethod[method] = entry;

                if (customIndex == 0)
                    nextIndex++;
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
                if (!handler.optimizedRPC) { WriteWithErrorCorrection(serializer); return; }
            }
            if (serializer.IsReading)
            {
                ushort idx = serializer.reader.ReadUInt16();
                if (RPCManager.defsByIndex.TryGetValue(idx, out var rpcHandler) && rpcHandler.optimizedRPC)
                    handler = rpcHandler;
                else { ReadWithErrorCorrection(serializer, idx); return; }
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

        /// <summary>
        /// Adds a byte specifying the length of the RPC data sent,
        /// so that the data can be ignored it is erroneous or if the RPC is unrecognized.
        /// </summary>
        private void WriteWithErrorCorrection(Serializer serializer)
        {
            try
            {
                //skip one byte ahead; serialize RPC args
                long headerPos = serializer.Position;
                byte tempArgLength = 0;
                serializer.Serialize(ref tempArgLength);
                long argsStartPos = serializer.Position;

                handler.serialize(this, serializer); //actually serialize the RPC

                long argsEndPos = serializer.Position;
                if (argsEndPos - argsStartPos > 255)
                {
                    RainMeadow.Error(new OverflowException($"RPC args of {argsEndPos - argsStartPos} exceeds 255 byte limit!"));

                    //return to the start of the args. Hopefully the erroneous data will be over-written, and a client will ignore it for having a 0 bytes!
                    serializer.stream.Position = argsStartPos;

                    throw new OverflowException($"RPC args of {argsEndPos - argsStartPos} exceeds 255 byte limit!");
                }

                //move back to the start; write the length byte
                serializer.stream.Position = headerPos;
                byte argsLength = (byte)(argsEndPos - argsStartPos);
                serializer.Serialize(ref argsLength);

                //finally, move back to the end
                serializer.stream.Position = argsEndPos;

                //TODO: change to RainMeadow.Trace
                RainMeadow.Debug($"Sending RPC of index {handler.index} with length {argsLength}");
            }
            catch (Exception)
            {
                RainMeadow.Error($"Error writing RPC {this}");
                throw;
            }
        }
        //idx is passed in because it has already been read
        private void ReadWithErrorCorrection(Serializer serializer, ushort idx)
        {
            try
            {
                byte argsLength = serializer.reader.ReadByte();
                if (argsLength == 0)
                {
                    RainMeadow.Error("Received RPC of length 0!" + (RPCManager.defsByIndex.TryGetValue(idx, out var rpc) ? rpc.method : ""));
                    return; //don't serialize empty RPCs! That's pointless!
                }

                //TODO: change to RainMeadow.Trace
                RainMeadow.Debug($"Receiving RPC of index {idx} with length {argsLength}");

                if (RPCManager.defsByIndex.TryGetValue(idx, out var rpcHandler))
                    handler = rpcHandler;
                else
                {
                    RainMeadow.Debug($"Received RPC with an unregistered index: {idx}");
                    serializer.reader.ReadBytes(argsLength); //just skip through the bytes; who cares what their values are
                    aborted = true; //so OnlineManager doesn't try to process it
                    return;
                }

                //now actually serialize the RPC
                long startPos = serializer.Position;
                try
                {
                    handler.serialize(this, serializer);
                }
                catch (Exception)
                {
                    RainMeadow.Error($"Error serializing RPC {this}");
                    //since the serialization failed, try to skip over the RPC's data, so the rest of the packet can still be serialized.
                    serializer.stream.Position = startPos + argsLength;
                    throw;
                }
            }
            catch (Exception)
            {
                RainMeadow.Error($"Error reading RPC {this}");
                throw;
            }
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
