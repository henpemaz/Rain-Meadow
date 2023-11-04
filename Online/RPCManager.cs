using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RainMeadow
{
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
        }

        public static void SetupRPCs()
        {
            // assembly.GetTypes().Where(t => typeof(OnlineResource).IsAssignableFrom(t));
            // assembly.GetTypes().Where(t => typeof(OnlineEntity).IsAssignableFrom(t));

            ushort index = 0;
            foreach(var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly=> assembly.GetTypes()))
            {
                index++;
                ProcessClass(type, index);
            }
        }

        public static void ProcessClass(Type targetType, ushort index)
        {
            var methods = typeof(OnlineResource).GetMethods().Where(m => m.GetCustomAttribute<RPCMethodAttribute>() != null);
            if (!methods.Any()) return;

            ParameterExpression rpceventParam = Expression.Parameter(typeof(RPCEvent), "rpcEvent");
            ParameterExpression serializerParam = Expression.Parameter(typeof(Serializer), "serializer");

            ParameterExpression targetVar = Expression.Variable(typeof(object), "target");
            ParameterExpression argsVar = Expression.Variable(typeof(object[]), "args");

            var rpcEventTargetAccessor = typeof(RPCEvent).GetField("target");
            var rpcEventArgsAccessor = typeof(RPCEvent).GetField("args");
            var serializerIsReadingProp = typeof(Serializer).GetProperty("IsReading");

            foreach (var method in methods)
            {


                var isStatic = method.IsStatic;
                // get args
                RainMeadow.Debug($"{targetType}-{method.Name}");
                var args = method.GetParameters();
                RainMeadow.Debug(args.Aggregate("", (e, a) => e + " - " + a));


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

                var expressions = new List<Expression>();
                var vars = new List<ParameterExpression>()
                {
                    targetVar, argsVar,
                };

                if (targetType != null && !isStatic)
                {
                    expressions.Add(Expression.Assign(targetVar, Expression.Field(rpceventParam, rpcEventTargetAccessor)));
                    // todo
                    // serialize the target type
                    // for Re

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
                        expressions.Add(Expression.Assign(argVar, Expression.Convert(Expression.ArrayIndex(argsVar, Expression.Constant(i)), argType)));
                        expressions.Add(Expression.Call(serializerParam, Serializer.GetSerializationMethod(argType, false, false)));
                        expressions.Add(Expression.Assign(Expression.ArrayIndex(argsVar, Expression.Constant(i)), Expression.Convert(argVar, typeof(object))));
                    }
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
            }
        }

        internal static RPCEvent BuildRPC(Delegate del, object[] args)
        {
            return new RPCEvent(del, args);
        }
    }

    public class RPCEvent : OnlineEvent, ResolvableEvent
    {
        private RPCManager.RPCDefinition handler;
        private object target;
        private object[] args;

        //private RPCArguments arguments;

        public RPCEvent() { }

        public RPCEvent(Delegate del, object[] args)
        {
            this.handler = RPCManager.defsByMethod[del.Method];
            this.target = del.Target;
            this.args = args;
        }

        public override EventTypeId eventType => throw new NotImplementedException();

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
    }
}