using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public partial class Serializer
    {
        public struct TypeInfo : IEqualityComparer<TypeInfo>
        {
            public Type fieldType;
            public bool nullable;
            public bool polymorphic;
            public bool longList;
            public TypeInfo(Type fieldType, bool nullable, bool polymorphic, bool longList)
            {
                this.fieldType = fieldType;
                this.nullable = nullable;
                this.polymorphic = polymorphic;
                this.longList = longList;
            }
            public readonly override string ToString() => $"{this.fieldType.FullName}{this.nullable}{this.polymorphic}{this.longList}";
            public readonly bool Equals(TypeInfo b1, TypeInfo b2)
            {
                return b1.fieldType.FullName == b2.fieldType.FullName && b1.nullable == b2.nullable && b1.polymorphic == b2.polymorphic && b1.longList == b2.longList;
            }
            public readonly int GetHashCode(TypeInfo obj) => obj.ToString().GetHashCode();
        }
    }
}
