using System.ComponentModel;
using TumblThree.Domain.Attributes;
using TumblThree.Domain.Converter;
using TumblThree.Domain.Properties;

namespace TumblThree.Domain.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum AspectRatioType
    {
        [LocalizedDescription("AspectRatio_All", typeof(Resources))]
        All,
        [LocalizedDescription("AspectRatio_Landscape", typeof(Resources))]
        Landscape,
        [LocalizedDescription("AspectRatio_LandscapePortrait", typeof(Resources))]
        LandscapePortrait,
        [LocalizedDescription("AspectRatio_LandscapeSquare", typeof(Resources))]
        LandscapeSquare,
        [LocalizedDescription("AspectRatio_Portrait", typeof(Resources))]
        Portrait,
        [LocalizedDescription("AspectRatio_PortraitSquare", typeof(Resources))]
        PortraitSquare,
        [LocalizedDescription("AspectRatio_Square", typeof(Resources))]
        Square
    }
}
