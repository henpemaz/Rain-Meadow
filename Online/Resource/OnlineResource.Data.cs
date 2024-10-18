using RainMeadow.Generics;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        private Dictionary<Type, ResourceData> resourceData = new();

        public T AddData<T>(T toAdd) where T : ResourceData
        {
            if (resourceData.TryGetValue(toAdd.GetType(), out var temp)) return (T)temp;
            resourceData.Add(toAdd.GetType(), toAdd);
            return toAdd;
        }

        public bool TryGetData<T>(out T d) where T : ResourceData
        {
            ResourceData temp;
            if (resourceData.TryGetValue(typeof(T), out temp))
            {
                d = (T)temp;
                return true;
            }
            d = null;
            return false;
        }

        public bool TryGetData(Type T, out ResourceData d)
        {
            return resourceData.TryGetValue(T, out d);
        }

        public T GetData<T>() where T : ResourceData
        {
            return (T)resourceData[typeof(T)];
        }

        public ResourceData GetData(Type T)
        {
            return resourceData[T];
        }

        /// <summary>
        /// Conditional data for a resource.
        /// Must have ctor(OnlineResource)
        /// </summary>
        public abstract class ResourceData
        {
            public ResourceData() { }

            public abstract ResourceDataState MakeState(OnlineResource resource);

            [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
            public abstract class ResourceDataState : OnlineState, IIdentifiable<byte>
            {
                public byte ID => (byte)handler.stateType.index;

                public ResourceDataState() { }

                public abstract void ReadTo(ResourceData data, OnlineResource resource);
                public abstract Type GetDataType();
                public ResourceData MakeData(OnlineResource resource) => (ResourceData)Activator.CreateInstance(GetDataType());
            }
        }
    }
}
