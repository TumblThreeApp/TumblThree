using System.Net;

namespace TumblThree.Applications
{
    public class ResponseDetails
    {
        public string RedirectUrl { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public string Response { get; set; }
    }
}
