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
        public static void SetupRPCs()
        {
            var methods = typeof(OnlineResource).GetMethods().Where(m => m.GetCustomAttribute<RPCMethodAttribute>() != null);
            int index = 1;
            foreach(var method in methods)
            {
                var targetType = typeof(OnlineResource); //todo
                // get args
                var args = method.GetParameters();
                RainMeadow.Debug(args.Aggregate("", (e, a) => e + " - "+ a));

                // make serialize method

                ParameterExpression targetParam = Expression.Parameter(typeof(OnlineResource), "target");
                ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");
                ParameterExpression serializerParam = Expression.Parameter(typeof(Serializer), "serializer");

                var expressions = new List<Expression>();
                var vars = new List<ParameterExpression>();

                for (int i = 0; i < args.Length; i++)
                {
                    // T arg = (T)args[i];
                    // serializer.Serialize(ref arg);
                    // args[i] = (object)arg;

                    var argType = args[i].ParameterType;
                    ParameterExpression argVar = Expression.Variable(argType);
                    vars.Add(argVar);
                    expressions.Add(Expression.Assign(argVar, Expression.Convert(Expression.ArrayIndex(argsParam, Expression.Constant(i)), argType)));
                    expressions.Add(Expression.Call(serializerParam, Serializer.GetSerializationMethod(argType, false, false)));
                    expressions.Add(Expression.Assign(Expression.ArrayIndex(argsParam, Expression.Constant(i)), Expression.Convert(argVar, typeof(object))));
                }

                var asdf = Expression.Lambda<Action<OnlineResource, object[], Serializer>>(Expression.Block(vars, expressions), targetParam, argsParam, serializerParam).Compile();


            }
        }

        internal static OnlineEvent BuildRPC(Delegate del, params object[] args)
        {
            OnlineResource resource = (OnlineResource)del.Target;

            //var rpcArgs = new RPCArguments(args);


            return new RPCEvent(del);
        }

        private abstract class RPCArguments
        {
            public abstract void CustomSerialize(Serializer serializer);
        }

        private class RPCEvent : OnlineEvent
        {
            private RPCArguments arguments;

            public RPCEvent(Delegate del, params object[] args)
            {

            }

            public override EventTypeId eventType => throw new NotImplementedException();

            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                // new()?
                arguments.CustomSerialize(serializer);
            }

            public override void Process()
            {
                throw new NotImplementedException();
            }
        }
    }
}