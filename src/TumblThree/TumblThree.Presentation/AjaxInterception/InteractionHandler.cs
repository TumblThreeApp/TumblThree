using CefSharp.Wpf;

namespace TumblThree.Presentation.AjaxInterception
{
    internal class InteractionHandler
    {
        public static ChromiumWebBrowser browser;

        public static void HookEventHandler(jsMapInterface.hookEvent hookEventArgs)
        {

            // And your AJAX response will be caught here!
            System.Diagnostics.Debug.WriteLine("Ajax Url: " + hookEventArgs.url);

        }
    }
}
