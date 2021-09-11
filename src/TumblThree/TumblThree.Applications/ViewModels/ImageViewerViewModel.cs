using System.ComponentModel.Composition;
using System.Waf.Applications;
using TumblThree.Applications.Views;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class ImageViewerViewModel : ViewModel<IImageViewerView>
    {
        [ImportingConstructor]
        public ImageViewerViewModel(IImageViewerView view)
            : base(view)
        {
        }

        public string ImageFolder { get; set; }

        public void ShowDialog(object owner) => ViewCore.ShowDialog(owner);
    }
}
