using AutoSigner.Component;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoSigner.Model
{
    internal abstract class SignInClient
    {
        protected readonly CookieAwareWebClient client;

        public SignInClient() : this(new CookieAwareWebClient())
        { }

        public SignInClient(CookieAwareWebClient client)
        {
            this.client = client;
        }

        public virtual bool ApplyDateRestriction { get; set; }

        protected abstract string Url { get; }

        public abstract bool IsDateAllowed(DateTime date);

        public SignInResult SignIn(NetworkCredential credential, DateTime dateTime, out string message)
        {
            try
            {
                if (ApplyDateRestriction && !IsDateAllowed(dateTime))
                    throw new ArgumentOutOfRangeException(nameof(dateTime));

                string result = client.Get(Url);
                if (result == null)
                    throw new ArgumentException();

                var token = GetWebPageToken(result);
                result = client.Post(Url, GetDataToBePosted(credential, dateTime, token));
                return GetSignInResult(result, out message);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return SignInResult.Other;
            }
        }

        protected abstract NameValueCollection GetDataToBePosted(NetworkCredential credential, DateTime dateTime, WebPageToken token);

        protected abstract SignInResult GetSignInResult(string input, out string message);

        protected abstract WebPageToken GetWebPageToken(string input);
    }
}