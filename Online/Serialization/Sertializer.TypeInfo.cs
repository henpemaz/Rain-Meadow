using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RainMeadow
{
    public partial class Serializer
    {
        public class TypeInfo : IEqualityComparer<TypeInfo>
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
            public override string ToString() => $"{this.fieldType.FullName}{this.nullable}{this.polymorphic}{this.longList}";
            public bool Equals(TypeInfo? b1, TypeInfo? b2)
            {
                if (ReferenceEquals(b1, b2))
                    return true;
                if (b2 is null || b1 is null)
                    return false;
                return b1.fieldType.FullName == b2.fieldType.FullName && b1.nullable == b2.nullable && b1.polymorphic == b2.polymorphic && b1.longList == b2.longList;
            }
            public int GetHashCode(TypeInfo obj) => obj.ToString().GetHashCode();
        }
    }
}
