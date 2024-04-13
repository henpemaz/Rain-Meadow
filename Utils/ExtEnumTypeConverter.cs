using System;
using System.ComponentModel;

namespace RainMeadow
{
    public class ExtEnumTypeConverter<T> : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            return value != null && ExtEnumBase.TryParse(typeof(T), (string)value, true, out var val) ? val : null;
        }


        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            return (value == null) ? null : value.ToString();
        }
    }
}
