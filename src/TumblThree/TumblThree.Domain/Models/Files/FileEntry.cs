using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class FileEntry
    {
        private string _filename;

        [DataMember(Name = "L")]
        public string Link { get; set; }

        [DataMember(Name = "F", EmitDefaultValue = false)]
        public string FilenameSer
        {
            get
            {
                return _filename;
            }
            set
            {
                _filename = value;
            }
        }

        public string Filename
        {
            get
            {
                return _filename == null ? Link : _filename;
            }
            set
            {
                _filename = (value == Link) ? null : value;
            }
        }
    }
}
