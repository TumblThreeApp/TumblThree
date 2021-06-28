using System.Waf.Applications;

namespace TumblThree.Applications.Views
{
    public interface IDetailsView : IView
    {
        int TabsCount { get; }
    }
}
