using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
            if(IsDelta) SerializeNullable(ref customSerializable);
            else Serialize(ref customSerializable);
        }


        public void Serialize<T>(ref List<T> listOfSerializables) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                writer.Write((byte)listOfSerializables.Count);
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

        public void Serialize<T>(ref T[] arrayOfSerializables) where T : ICustomSerializable, new()
        {
            if (IsWriting)
            {
                writer.Write((byte)arrayOfSerializables.Length);
                for (int i = 0; i < arrayOfSerializables.Length; i++)
                {
                    arrayOfSerializables[i].CustomSerialize(this);
                }
            }
            if (IsReading)
            {
                var count = reader.ReadByte();
                arrayOfSerializables = new T[count];
                for (int i = 0; i < count; i++)
                {
                    T item = new();
                    item.CustomSerialize(this);
                    arrayOfSerializables[i] = item;
                }
            }
        }

        // tempting to try and cache this, would need a query icomparable
        internal static MethodInfo GetSerializationMethod(Type fieldType, bool nullable, bool polymorphic)
        {
            var arguments = new { nullable, polymorphic }; // one hell of a drug
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
                    { nullable: false, polymorphic: false } => "SerializeStaticStates",
                    { nullable: false, polymorphic: true } => "SerializePolyStates",
                    { nullable: true, polymorphic: false } => "SerializeNullableStaticStates",
                    { nullable: true, polymorphic: true } => "SerializeNullablePolyStates"
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
                    { nullable: false } => "Serialize",
                    { nullable: true } => "SerializeNullable"
                } && m.IsGenericMethod && m.GetGenericMethodDefinition().GetGenericArguments().Any(ga => ga.GetGenericParameterConstraints().Any(t => t == typeof(Serializer.ICustomSerializable)))
                && m.GetParameters().Any(p => p.ParameterType.IsByRef && (p.ParameterType.GetElementType().IsGenericType && p.ParameterType.GetElementType().GetGenericTypeDefinition() == typeof(List<>)) != fieldType.IsArray && p.ParameterType.GetElementType().IsArray == fieldType.IsArray)
                ).MakeGenericMethod(new Type[] { fieldType.IsArray ? fieldType.GetElementType() : fieldType.GetGenericArguments()[0] }) ;
            }
            if (typeof(OnlineResource).IsAssignableFrom(fieldType))
            {
                return typeof(Serializer).GetMethod(nameof(Serializer.SerializeResourceByReference)).MakeGenericMethod(fieldType);
            }
            if (typeof(OnlineEntity).IsAssignableFrom(fieldType))
            {
                return typeof(Serializer).GetMethod(nameof(Serializer.SerializEntityById));
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
            if ((fieldType.BaseType?.IsGenericType ?? false) && typeof(ExtEnum<>).IsAssignableFrom(fieldType.BaseType.GetGenericTypeDefinition()))
            {
                return typeof(Serializer).GetMethods().Single(m =>
                m.Name == "SerializeExtEnum" && m.IsGenericMethod).MakeGenericMethod(fieldType);
            }
            
            if (!fieldType.IsValueType && fieldType != typeof(string))
            {
                RainMeadow.Error($"{fieldType} not handled by SerializerCallMethod");
            }
            return typeof(Serializer).GetMethod(nullable ? "SerializeNullable" : "Serialize", new[] { fieldType.MakeByRefType() });
        }
    }
}