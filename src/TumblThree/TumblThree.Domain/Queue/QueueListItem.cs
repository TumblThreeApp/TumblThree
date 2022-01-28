using System;
using System.Waf.Foundation;

using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Domain.Queue
{
    [Serializable]
    public class QueueListItem : Model
    {
        private string progress;

        public event EventHandler InterruptionRequested;

        public QueueListItem(IBlog blog)
        {
            Blog = blog;
        }

        public IBlog Blog { get; }

        public string Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
        }

        public void RequestInterruption()
        {
            EventHandler handler = InterruptionRequested;
            handler?.Invoke(this, EventArgs.Empty);
        }
    }
}
