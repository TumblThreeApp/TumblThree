using System.Collections.Generic;
using System.Threading;

namespace TumblThree.Applications.DataModels
{
    public interface IPostQueue<T>
    {
        void Add(T post);

        void CompleteAdding();

        IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken);
    }
}
