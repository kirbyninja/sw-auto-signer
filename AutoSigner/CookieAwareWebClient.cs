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

        public string Post(string address, NameValueCollection postData)
        {
            CookieContainer container;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            Byte[] buffer = Encoding.ASCII.GetBytes(string.Join("&", GetDataStrings(postData)));
            request.ContentLength = buffer.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(buffer, 0, buffer.Length);
            }

            container = request.CookieContainer = new CookieContainer();

            string responseText = null;
            using (WebResponse response = request.GetResponse())
            {
                if (response != null)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            responseText = streamReader.ReadToEnd();
                        }
                    }
                }
            }

            CookieContainer = container;
            return responseText;
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