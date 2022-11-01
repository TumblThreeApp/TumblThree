using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class FileEntry
    {
        private string _originalLink;
        private string _filename;

        [DataMember(Name = "L")]
        public string Link { get; set; }

        [DataMember(Name = "O", EmitDefaultValue = false)]
        public string OriginalLinkSer
        {
            get
            {
                return _originalLink;
            }
            set
            {
                _originalLink = value;
            }
        }

        public string OriginalLink
        {
            get
            {
                return string.IsNullOrEmpty(_originalLink) ? null : _originalLink;
            }
            set
            {
                _originalLink = (string.IsNullOrEmpty(value) || value == Link) ? null : value;
            }
        }

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
