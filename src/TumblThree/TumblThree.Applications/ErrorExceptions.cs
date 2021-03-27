using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TumblThree.Applications
{

    [Serializable]
    public class ClipboardContentException : Exception
    {
        public ClipboardContentException(Exception innerException) : base(null, innerException) { }
    }

    [Serializable]
    public class QueuelistLoadException : Exception
    {
        public QueuelistLoadException(Exception innerException) : base(null, innerException) { }
    }

    [Serializable]
    public class QueuelistSaveException : Exception
    {
        public QueuelistSaveException(Exception innerException) : base(null, innerException) { }
    }

    [Serializable]
    public class TumblrPrivacyConsentException : Exception
    {
        public TumblrPrivacyConsentException(Exception innerException) : base(null, innerException) { }
    }

    [Serializable]
    public class DiskFullException : Exception
    {
        public DiskFullException(Exception innerException) : base(null, innerException) { }
    }

    [Serializable]
    public class UISettingsException : Exception
    {
        public UISettingsException(Exception innerException) : base(null, innerException) { }
    }
}
