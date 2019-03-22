using System.Runtime.Serialization;

namespace TumblThree.Applications.Properties
{
    [DataContract]
    public sealed class ManagerSettings : IExtensibleDataObject
    {
        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }
    }
}
