using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TumblThree.Applications.DataModels
{
    public class PostQueue<T> : IPostQueue<T>
    {
        private readonly BlockingCollection<T> postQueue;

        public PostQueue(IProducerConsumerCollection<T> postTaskCollection)
        {
            postQueue = new BlockingCollection<T>(postTaskCollection);
        }

        public void Add(T post)
        {
            postQueue.Add(post);
        }

        public void CompleteAdding()
        {
            postQueue.CompleteAdding();
        }

        public IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken)
        {
            return postQueue.GetConsumingEnumerable(cancellationToken);
        }
    }
}
