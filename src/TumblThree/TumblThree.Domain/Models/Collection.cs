using System.Runtime.Serialization;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class Collection
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string DownloadLocation { get; set; }

        public Collection Clone()
        {
            return new Collection()
            {
                Id = Id,
                Name = Name,
                DownloadLocation = DownloadLocation
            };
        }
    }
}
