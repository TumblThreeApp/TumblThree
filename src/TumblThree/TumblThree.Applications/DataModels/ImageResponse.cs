using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels
{
    [DataContract(Name = "imageResponse")]
    public class ImageResponse
    {
        [DataMember(Name = "imageResponse")]
        public IList<Image> Images { get; set; }
    }

    [DataContract]
    public class Image
    {
        [DataMember(Name = "mediaKey")]
        public string MediaKey { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "colors")]
        public Colors Colors { get; set; }

        [DataMember(Name = "hasOriginalDimensions", IsRequired = false, EmitDefaultValue = false)]
        public bool HasOriginalDimensions { get; set; }
    }

    [DataContract]
    public class Colors
    {
        [DataMember(Name = "c0")]
        public string C0 { get; set; }

        [DataMember(Name = "c1")]
        public string C1 { get; set; }
    }
}
