using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoSigner.Model
{
    internal class WebPageToken
    {
        public WebPageToken(string eventValidation, string viewState)
        {
            EventValidation = eventValidation;
            ViewState = viewState;
        }

        public string EventValidation { get; protected set; }

        public string ViewState { get; protected set; }
    }
}