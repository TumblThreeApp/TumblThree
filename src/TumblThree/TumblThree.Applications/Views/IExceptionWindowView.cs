using System;
using System.Waf.Applications;

namespace TumblThree.Applications.Views
{
    public interface IExceptionWindowView : IView
    {
        void ShowDialog(object owner);

        event EventHandler Closed;
    }
}
