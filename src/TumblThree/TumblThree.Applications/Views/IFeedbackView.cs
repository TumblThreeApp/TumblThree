using System.Waf.Applications;

namespace TumblThree.Applications.Views
{
    public interface IFeedbackView : IView
    {
        void ShowDialog(object owner);

        void Close();
    }
}
