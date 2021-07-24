using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TumblThree.Applications.DataModels
{
    public class PostQueue<T> : IPostQueue<T>
    {
        private readonly BufferBlock<T> postQueue;

        public PostQueue()
        {
            postQueue = new BufferBlock<T>();
        }

        public void Add(T post)
        {
            postQueue.Post(post);
        }

        public async Task<T> ReceiveAsync()
        {
            return await postQueue.ReceiveAsync();
        }

        public Task CompleteAdding()
        {
            postQueue.Complete();
            return postQueue.Completion;
        }

        public async Task<bool> OutputAvailableAsync(CancellationToken cancellationToken)
        {
            return await postQueue.OutputAvailableAsync(cancellationToken);
        }
    }
}
