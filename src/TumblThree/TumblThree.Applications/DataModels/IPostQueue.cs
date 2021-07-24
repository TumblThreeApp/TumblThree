using System.Threading;
using System.Threading.Tasks;

namespace TumblThree.Applications.DataModels
{
    public interface IPostQueue<T>
    {
        void Add(T post);

        Task<T> ReceiveAsync();

        Task CompleteAdding();

        Task<bool> OutputAvailableAsync(CancellationToken cancellationToken);
    }
}
