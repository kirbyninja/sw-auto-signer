using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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
            CookieContainer = container;
        }

        public CookieAwareWebClient()
          : this(new CookieContainer())
        { }

        public CookieContainer CookieContainer { get; private set; }

        public void Login(string loginPageAddress, NameValueCollection loginData, out string responseText)
        {
            CookieContainer container;

            var request = (HttpWebRequest)WebRequest.Create(loginPageAddress);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            var buffer = Encoding.ASCII.GetBytes(string.Join("&", GetDataStrings(loginData)));
            request.ContentLength = buffer.Length;

            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(buffer, 0, buffer.Length);
            }

            container = request.CookieContainer = new CookieContainer();

            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        responseText = streamReader.ReadToEnd();
                    }
                }
            }

            CookieContainer = container;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);
            request.CookieContainer = CookieContainer;
            return request;
        }

        private IEnumerable<string> GetDataStrings(NameValueCollection loginData)
        {
            foreach (var key in loginData.AllKeys)
                foreach (var value in loginData.GetValues(key))
                    yield return key + "=" + Uri.EscapeDataString(value);
        }
    }
}