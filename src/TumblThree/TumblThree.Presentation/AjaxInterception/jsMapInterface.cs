using System;

namespace TumblThree.Presentation.AjaxInterception
{
    internal class jsMapInterface
    {
        public Action<hookEvent> onHookEvent = null;

        public void hook(string message, string method, string url, bool async, string user, string pass, string context) // Must be lowercase!
        {
            if (onHookEvent != null)
            {
                onHookEvent(new hookEvent()
                {
                    message = message,
                    url = url,
                    async = async,
                    user = user,
                    pass = pass,
                    context = context
                });
            }
        }

        public class hookEvent
        {
            public string message { get; set; }
            public string url { get; set; }
            public bool async { get; set; }
            public string user { get; set; }
            public string pass { get; set; }
            public string context { get; set; }
        }
    }
}
