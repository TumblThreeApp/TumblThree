using CefSharp;

namespace TumblThree.Presentation.AjaxInterception
{
    internal class RenderHandler : IRenderProcessMessageHandler
    {
        public void OnContextReleased(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
        }

        public void OnFocusedNodeChanged(IWebBrowser browserControl, IBrowser browser, IFrame frame, IDomNode node)
        {
        }

        public void OnUncaughtException(IWebBrowser browserControl, IBrowser browser, IFrame frame, JavascriptException exception)
        {
        }

        void IRenderProcessMessageHandler.OnContextCreated(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
            var strScript = @"document.addEventListener('DOMContentLoaded',
                function () {
                (function(open) {
                XMLHttpRequest.prototype.open = function(method, url, async, user, pass) {
                this.addEventListener(""readystatechange"",
                function() {
                if (this.readyState === 4) {
                window.jsInterface.hook(this.responseText, method, url, async, user, pass, ""Context"");
                }
                },
                false);
                open.call(this, method, url, async, user, pass);
                };
                })(XMLHttpRequest.prototype.open);
                });";
            frame.ExecuteJavaScriptAsync(strScript);
        }
    }
}
