using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoSigner
{
    public class CookieAwareWebClient : WebClient
    {
        public CookieAwareWebClient(CookieContainer container)
        {
            Encoding = Encoding.UTF8;
            CookieContainer = container;
            ServicePointManager.Expect100Continue = false;
        }

        public CookieAwareWebClient()
            : this(new CookieContainer())
        { }

        public CookieContainer CookieContainer { get; private set; }

        public string Get(string address)
        {
            return DownloadString(address);
        }

        public string Post(string address, NameValueCollection postData)
        {
            return Encoding.GetString(UploadValues(address, postData));
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);
            request.Host = "www.systemweb.com.tw:8080";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:50.0) Gecko/20100101 Firefox/50.0";
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.KeepAlive = true;
            request.Proxy = new WebProxy();
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.CookieContainer = CookieContainer;
            request.Headers.Add("Accept-Language", "en-US,en;q=0.7,zh-TW;q=0.3");
            request.ContentType = "application/x-www-form-urlencoded";

            return request;
        }
    }
}