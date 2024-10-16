using RainMeadow.Generics;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract partial class OnlineEntity
    {
        private Dictionary<Type, EntityData> entityData = new();

        internal T AddData<T>(T toAdd) where T : EntityData
        {
            entityData.Add(toAdd.GetType(), toAdd);
            return toAdd;
        }

        internal bool TryGetData<T>(out T d) where T : EntityData
        {
            EntityData temp;
            if (entityData.TryGetValue(typeof(T), out temp))
            {
                d = (T)temp;
                return true;
            }
            d = null;
            return false;
        }

        internal bool TryGetData(Type T, out EntityData d)
        {
            return entityData.TryGetValue(T, out d);
        }

        internal T GetData<T>() where T : EntityData
        {
            return (T)entityData[typeof(T)];
        }

        internal EntityData GetData(Type T)
        {
            return entityData[T];
        }

        /// <summary>
        /// Conditional data for entities.
        /// Must have ctor(OnlineEntity)
        /// </summary>
        public abstract class EntityData
        {
            public EntityData() { }

            public abstract EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource);

            [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
            public abstract class EntityDataState : OnlineState, IIdentifiable<byte>
            {
                public byte ID => (byte)handler.stateType.index;

                public EntityDataState() { }

                public abstract void ReadTo(EntityData data, OnlineEntity onlineEntity);
                public abstract Type GetDataType();
                public EntityData MakeData(OnlineEntity entity) => (EntityData)Activator.CreateInstance(GetDataType());
            }
        }
    }
}
