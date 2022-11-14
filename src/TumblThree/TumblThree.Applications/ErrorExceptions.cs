using System;
using System.Runtime.Serialization;

namespace TumblThree.Applications
{

    [Serializable]
    public class ClipboardContentException : Exception
    {
        public ClipboardContentException(Exception innerException) : base(innerException?.Message, innerException) { }

        public ClipboardContentException() {  }

        public ClipboardContentException(string message) : base(message)  {  }

        public ClipboardContentException(string message, Exception innerException) : base(message, innerException) { }

        protected ClipboardContentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class QueuelistLoadException : Exception
    {
        public QueuelistLoadException(Exception innerException) : base(innerException?.Message, innerException) { }

        public QueuelistLoadException() { }

        public QueuelistLoadException(string message) : base(message) { }

        public QueuelistLoadException(string message, Exception innerException) : base(message, innerException) { }

        protected QueuelistLoadException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class QueuelistSaveException : Exception
    {
        public QueuelistSaveException(Exception innerException) : base(innerException?.Message, innerException) { }

        public QueuelistSaveException() { }

        public QueuelistSaveException(string message) : base(message) { }

        public QueuelistSaveException(string message, Exception innerException) : base(message, innerException) { }

        protected QueuelistSaveException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class TumblrPrivacyConsentException : Exception
    {
        public TumblrPrivacyConsentException(Exception innerException) : base(innerException?.Message, innerException) { }

        public TumblrPrivacyConsentException() { }

        public TumblrPrivacyConsentException(string message) : base(message) { }

        public TumblrPrivacyConsentException(string message, Exception innerException) : base(message, innerException) { }

        protected TumblrPrivacyConsentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DiskFullException : Exception
    {
        public DiskFullException(Exception innerException) : base(innerException?.Message, innerException) { }

        public DiskFullException() { }

        public DiskFullException(string message) : base(message) { }

        public DiskFullException(string message, Exception innerException) : base(message, innerException) { }

        protected DiskFullException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UISettingsException : Exception
    {
        public UISettingsException(Exception innerException) : base(innerException?.Message, innerException) { }

        public UISettingsException() { }

        public UISettingsException(string message) : base(message) { }

        public UISettingsException(string message, Exception innerException) : base(message, innerException) { }

        protected UISettingsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class LimitExceededWebException : Exception
    {
        public LimitExceededWebException(Exception innerException) : base(innerException?.Message, innerException) { }

        public LimitExceededWebException() { }

        public LimitExceededWebException(string message) : base(message) { }

        public LimitExceededWebException(string message, Exception innerException) : base(message, innerException) { }

        protected LimitExceededWebException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class APIException : Exception
    {
        public APIException(Exception innerException) : base(innerException?.Message, innerException) { }

        public APIException() { }

        public APIException(string message) : base(message) { }

        public APIException(string message, Exception innerException) : base(message, innerException) { }

        protected APIException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
