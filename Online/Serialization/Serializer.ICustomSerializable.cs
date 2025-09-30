using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Unity.Mathematics;

namespace RainMeadow
{
    public partial class Serializer
    {
        public interface ICustomSerializable
        {
            void CustomSerialize(Serializer serializer);
        }

        public void Serialize<T>(ref T customSerializable) where T : ICustomSerializable, new()
        {
            if (IsReading) customSerializable = new();
            customSerializable.CustomSerialize(this);
        }

        public void SerializeNullable<T>(ref T customSerializable) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                writer.Write(customSerializable != null);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                if (customSerializable != null)
                {
                    customSerializable.CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                if (reader.ReadBoolean())
                {
                    customSerializable = new();
                    customSerializable.CustomSerialize(this);
                }
            }
        }

        public void SerializeNullableDelta<T>(ref T customSerializable) where T : ICustomSerializable, new()
        {
            if (IsDelta) SerializeNullable(ref customSerializable);
            else Serialize(ref customSerializable);
        }

        public void SerializeByte<T>(ref List<T> listOfSerializables) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                if (listOfSerializables.Count > 255) throw new OverflowException("too many elements");
                writer.Write((byte)listOfSerializables.Count);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                for (int i = 0; i < listOfSerializables.Count; i++)
                {
                    listOfSerializables[i].CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                listOfSerializables = new(count);
                for (int i = 0; i < count; i++)
                {
                    T item = new();
                    item.CustomSerialize(this);
                    listOfSerializables.Add(item);
                }
            }
        }

        public void SerializeByte<T>(ref T[] arrayOfSerializables) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                if (arrayOfSerializables.Length > 255) throw new OverflowException("too many elements");
                writer.Write((byte)arrayOfSerializables.Length);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                for (int i = 0; i < arrayOfSerializables.Length; i++)
                {
                    arrayOfSerializables[i].CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                byte count = reader.ReadByte();
                arrayOfSerializables = new T[count];
                for (int i = 0; i < count; i++)
                {
                    T item = new();
                    item.CustomSerialize(this);
                    arrayOfSerializables[i] = item;
                }
            }
        }

        public void SerializeShort<T>(ref List<T> listOfSerializables) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                if (listOfSerializables.Count > ushort.MaxValue) throw new OverflowException("too many elements");
                writer.Write((ushort)listOfSerializables.Count);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                for (int i = 0; i < listOfSerializables.Count; i++)
                {
                    listOfSerializables[i].CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                ushort count = reader.ReadUInt16();
                listOfSerializables = new(count);
                for (int i = 0; i < count; i++)
                {
                    T item = new();
                    item.CustomSerialize(this);
                    listOfSerializables.Add(item);
                }
            }
        }

        public void SerializeShort<T>(ref T[] arrayOfSerializables) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                if (arrayOfSerializables.Length > ushort.MaxValue) throw new OverflowException("too many elements");
                writer.Write((ushort)arrayOfSerializables.Length);
#if TRACING
                if (IsWriting) RainMeadow.Trace(1);
#endif
                for (int i = 0; i < arrayOfSerializables.Length; i++)
                {
                    arrayOfSerializables[i].CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                ushort count = reader.ReadUInt16();
                arrayOfSerializables = new T[count];
                for (int i = 0; i < count; i++)
                {
                    T item = new();
                    item.CustomSerialize(this);
                    arrayOfSerializables[i] = item;
                }
            }
        }

        internal static Dictionary<Serializer.TypeInfo, MethodInfo> serializerMethods = new();

        // tempting to try and cache this, would need a query icomparable
        // liz: say no more!
        // hen: yeah sure nice except it was untested and didn't work :(
        internal static MethodInfo GetSerializationMethod(Type fieldType, bool nullable, bool polymorphic, bool longList)
        {
            var key = new Serializer.TypeInfo(fieldType, nullable, polymorphic, longList);
            MethodInfo cached = null;
            if (serializerMethods.TryGetValue(key, out cached))
            {
                RainMeadow.Debug($"Using cached method for {key}");
                return cached;
            }
            RainMeadow.Debug($"Adding cached method for {key}");
            var method = MakeSerializationMethod(fieldType, nullable, polymorphic, longList);
            serializerMethods.Add(key, method);
            return method;
        }

        internal static MethodInfo MakeSerializationMethod(Type fieldType, bool nullable, bool polymorphic, bool longList)
        {
            var arguments = new { nullable, polymorphic, longList }; // one hell of a drug
            if (typeof(OnlineState).IsAssignableFrom(fieldType))
            {
                return typeof(Serializer).GetMethods().Single(m =>
                m.Name == arguments switch
                {
                    { nullable: false, polymorphic: false } => "SerializeStaticState",
                    { nullable: false, polymorphic: true } => "SerializePolyState",
                    { nullable: true, polymorphic: false } => "SerializeNullableStaticState",
                    { nullable: true, polymorphic: true } => "SerializeNullablePolyState"
                } && m.IsGenericMethod).MakeGenericMethod(new Type[] { fieldType });
            }
            if (typeof(OnlineState[]).IsAssignableFrom(fieldType) || fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>) && typeof(OnlineState).IsAssignableFrom(fieldType.GetGenericArguments()[0]))
            {
                return typeof(Serializer).GetMethods().Single(m =>
                m.Name == arguments switch
                {
                    { nullable: false, polymorphic: false, longList: false } => "SerializeStaticStatesByte",
                    { nullable: false, polymorphic: true, longList: false } => "SerializePolyStatesByte",
                    { nullable: true, polymorphic: false, longList: false } => "SerializeNullableStaticStatesByte",
                    { nullable: true, polymorphic: true, longList: false } => "SerializeNullablePolyStatesByte",
                    { nullable: false, polymorphic: false, longList: true } => "SerializeStaticStatesShort",
                    { nullable: false, polymorphic: true, longList: true } => "SerializePolyStatesShort",
                    { nullable: true, polymorphic: false, longList: true } => "SerializeNullableStaticStatesShort",
                    { nullable: true, polymorphic: true, longList: true } => "SerializeNullablePolyStatesShort"

                } && m.IsGenericMethod && (m.GetParameters()[0].ParameterType.GetElementType().IsArray == fieldType.IsArray)).MakeGenericMethod(new Type[] { fieldType.IsArray ? fieldType.GetElementType() : fieldType.GetGenericArguments()[0] });
            }

            if (typeof(Serializer.ICustomSerializable).IsAssignableFrom(fieldType))
            {
                return typeof(Serializer).GetMethods().Single(m =>
                m.Name == arguments switch
                {
                    { nullable: false } => "Serialize",
                    { nullable: true } => "SerializeNullable"
                } && m.IsGenericMethod && m.GetGenericMethodDefinition().GetGenericArguments().Any(ga => ga.GetGenericParameterConstraints().Any(t => t == typeof(Serializer.ICustomSerializable)))
                && m.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.ParameterType.GetElementType().IsGenericType || p.ParameterType.GetElementType().GetGenericTypeDefinition() != typeof(List<>)) && !p.ParameterType.GetElementType().IsArray)
                ).MakeGenericMethod(new Type[] { fieldType });
            }
            if (typeof(Serializer.ICustomSerializable[]).IsAssignableFrom(fieldType) || fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>) && typeof(Serializer.ICustomSerializable).IsAssignableFrom(fieldType.GetGenericArguments()[0]))
            {
                return typeof(Serializer).GetMethods().Single(m =>
                m.Name == arguments switch
                {
                    { nullable: false, longList: false } => "SerializeByte",
                    { nullable: true, longList: false } => "SerializeNullableByte",
                    { nullable: false, longList: true } => "SerializeShort",
                    { nullable: true, longList: true } => "SerializeNullableShort"
                } && m.IsGenericMethod && m.GetGenericMethodDefinition().GetGenericArguments().Any(ga => ga.GetGenericParameterConstraints().Any(t => t == typeof(Serializer.ICustomSerializable)))
                && m.GetParameters().Any(p => p.ParameterType.IsByRef && (p.ParameterType.GetElementType().IsGenericType && p.ParameterType.GetElementType().GetGenericTypeDefinition() == typeof(List<>)) != fieldType.IsArray && p.ParameterType.GetElementType().IsArray == fieldType.IsArray)
                ).MakeGenericMethod(new Type[] { fieldType.IsArray ? fieldType.GetElementType() : fieldType.GetGenericArguments()[0] });
            }
            if (typeof(OnlineResource).IsAssignableFrom(fieldType))
            {
                return typeof(Serializer).GetMethod(nameof(Serializer.SerializeResourceByReference)).MakeGenericMethod(fieldType);
            }
            if (typeof(OnlineEntity).IsAssignableFrom(fieldType))
            {
                return typeof(Serializer).GetMethod(nullable ? nameof(Serializer.SerializeNullableEntityById) : nameof(Serializer.SerializeEntityById)).MakeGenericMethod(fieldType);
            }
            if (typeof(OnlinePlayer).IsAssignableFrom(fieldType))
            {
                return typeof(Serializer).GetMethod(nameof(Serializer.SerializePlayerInLobby));
            }
            if (typeof(OnlineEvent).IsAssignableFrom(fieldType))
            {
                return typeof(Serializer).GetMethods().Single(m =>
                m.Name == "SerializeEvent" && m.IsGenericMethod).MakeGenericMethod(fieldType);
            }
            if ((fieldType.BaseType?.IsGenericType ?? false) && typeof(ExtEnum<>).IsAssignableFrom(fieldType.BaseType.GetGenericTypeDefinition())) // todo array/list of this will be a headache
            {
                return typeof(Serializer).GetMethods().Single(m =>
                m.Name == (arguments.nullable ? "SerializeNullableExtEnum" : "SerializeExtEnum") && m.IsGenericMethod).MakeGenericMethod(fieldType);
            }

            if (!(fieldType.IsValueType || (fieldType.IsArray && fieldType.GetElementType().IsValueType)) && fieldType != typeof(string))
            {
                RainMeadow.Debug($"{fieldType} not handled by SerializerCallMethod");
            }

            if (Nullable.GetUnderlyingType(fieldType) is Type t)
            {
                var retMethod = typeof(Serializer).GetMethod("SerializeNullable", new[] { fieldType.MakeByRefType() }); ;
                if (retMethod is not null) return retMethod;

                // T internalvalue = default(T);
                // if (serializer.isWriting)
                // {
                //     serializer.writer.Write(value.HasValue);
                //     if (value.HasValue)
                //     {
                //          internalvalue = value.Value;
                //          goto callSerialize;
                //     }
                //     
                // }

                // if (serializer.isReading && serializer.reader.ReadBoolean())
                // {
                //     goto callSerialize;
                // }
                // return;

                // callSerialize:
                // 
                //     Serialize(ref internalvalue);
                //     value = in
                //     return;

                // IL implementation:
                var writer = typeof(Serializer).GetField(nameof(Serializer.writer));
                var reader = typeof(Serializer).GetField(nameof(Serializer.reader));
                var hasValue = fieldType.GetProperty("HasValue").GetGetMethod();
                var value = fieldType.GetProperty("Value").GetGetMethod();
                var nullableCtor = fieldType.GetConstructor([t]);
                var serializeFunc = GetSerializationMethod(t, false, true, true);
                if (serializeFunc == null)
                {
                    throw new InvalidOperationException($"No matching serialization method found for type {t.FullName}");
                }

                var dynMethod = new DynamicMethod("SerializeNullable" + t.Name, null, [typeof(Serializer), fieldType.MakeByRefType()]);

                var il = dynMethod.GetILGenerator();
                var internalValue = il.DeclareLocal(t, true);
                var afterWrite = il.DefineLabel();
                var afterRead = il.DefineLabel();
                var callSerialize = il.DefineLabel();

                // internalValue = default;
                il.Emit(OpCodes.Ldloca, internalValue);
                il.Emit(OpCodes.Initobj, t);
                il.Emit(OpCodes.Stloc, internalValue);

                // if (serializer.IsWriting)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(Serializer).GetProperty(nameof(Serializer.IsWriting)).GetGetMethod());
                il.Emit(OpCodes.Brfalse_S, afterWrite);

                // serializer.writer.Write(value.HasValue);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, writer);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, hasValue);
                il.Emit(OpCodes.Callvirt, typeof(BinaryWriter).GetMethod(nameof(BinaryWriter.Write), new[] { typeof(bool) }));

                // if (value.HasValue) goto afterWrite;
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, hasValue);
                il.Emit(OpCodes.Brfalse_S, afterWrite);

                // internalvalue = value.Value;
                // goto callSerialize;
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, value);
                il.Emit(OpCodes.Stloc, internalValue);
                il.Emit(OpCodes.Br, callSerialize);

                il.MarkLabel(afterWrite);

                // if (serializer.IsReading)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(Serializer).GetProperty(nameof(Serializer.IsReading)).GetGetMethod());
                il.Emit(OpCodes.Brfalse_S, afterRead);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, reader);
                il.Emit(OpCodes.Callvirt, typeof(BinaryReader).GetMethod(nameof(BinaryReader.ReadBoolean)));
                il.Emit(OpCodes.Brfalse_S, afterRead);
                il.Emit(OpCodes.Br, callSerialize);

                il.MarkLabel(afterRead);
                il.Emit(OpCodes.Ret);

                // callSerialize:
                il.MarkLabel(callSerialize);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloca, internalValue);
                il.Emit(OpCodes.Call, serializeFunc);

                il.Emit(OpCodes.Ldarga, 1);
                il.Emit(OpCodes.Ldloc, internalValue);
                il.Emit(OpCodes.Newobj, nullableCtor);
                il.Emit(OpCodes.Stobj, fieldType);
                il.Emit(OpCodes.Ret);
                return dynMethod;
            }

            return typeof(Serializer).GetMethod(nullable ? "SerializeNullable" : "Serialize", new[] { fieldType.MakeByRefType() });
        }
    }
}