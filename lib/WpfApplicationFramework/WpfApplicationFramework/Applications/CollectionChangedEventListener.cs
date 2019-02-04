using System.Collections.Specialized;
using System.Windows;

namespace System.Waf.Applications
{
    internal class CollectionChangedEventListener : IWeakEventListener
    {
        private readonly INotifyCollectionChanged source;
        private readonly NotifyCollectionChangedEventHandler handler;


        public CollectionChangedEventListener(INotifyCollectionChanged source, NotifyCollectionChangedEventHandler handler)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (handler == null) { throw new ArgumentNullException(nameof(handler)); }
            this.source = source;
            this.handler = handler;
        }


        public INotifyCollectionChanged Source { get { return source; } }

        public NotifyCollectionChangedEventHandler Handler { get { return handler; } }


        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            handler(sender, (NotifyCollectionChangedEventArgs)e);
            return true;
        }
    }
}
