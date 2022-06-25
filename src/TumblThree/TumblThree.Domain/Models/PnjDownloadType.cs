using System.ComponentModel;
using TumblThree.Domain.Converter;

namespace TumblThree.Domain.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum PnjDownloadType
    {
        png,
        jpg
    }
}
