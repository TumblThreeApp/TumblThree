using System.Runtime.Serialization;
using System.Waf.Foundation;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class Collection : Model
    {
        private int id;
        private string name;
        private string downloadLocation;

        [DataMember]
        public int Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        [DataMember]
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        [DataMember]
        public string DownloadLocation
        {
            get => downloadLocation;
            set => SetProperty(ref downloadLocation, value);
        }

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
