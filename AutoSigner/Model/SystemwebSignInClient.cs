using AutoSigner.Component;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoSigner.Model
{
    internal sealed class SystemwebSignInClient : SignInClient
    {
        private readonly DateTime[] exceptionalDates;

        public SystemwebSignInClient() : base()
        {
            exceptionalDates = GetExceptionalDates(client.Get(Url)).ToArray();
            ApplyDateRestriction = true;
        }

        private enum ExtractTextType
        {
            AlertMessage,
            EventValidation,
            Success,
            ViewState,
        }

        protected override string Url => "http://www.systemweb.com.tw:8080/AddSignInRecord.aspx";

        public override bool IsDateAllowed(DateTime date)
        {
            try
            {
                bool isHoliday = date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday;

                if (exceptionalDates.Contains(date.Date))
                    return isHoliday;
                else
                {
                    DateTime lastMonth = DateTime.Today.AddMonths(-1);
                    return !isHoliday
                        && date.Date >= new DateTime(lastMonth.Year, lastMonth.Month, 26)
                        && date.Date <= DateTime.Today;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override NameValueCollection GetDataToBePosted(NetworkCredential credential, DateTime dateTime, WebPageToken token)
        {
            return new NameValueCollection()
            {
                { "txtId", credential.UserName },
                { "txtPwd", credential.Password },
                { "txtDate", dateTime.ToString("yyyy-MM-dd") },
                { "txtHour", dateTime.Hour.ToString() },
                { "txtMinute", dateTime.Minute.ToString() },
                { "btnAddSignIn", "確定補登" },
                { "__EVENTTARGET", "" },
                { "__EVENTARGUMENT", "" },
                { "__VIEWSTATE", token.ViewState },
                { "__EVENTVALIDATION", token.EventValidation },
            };
        }

        protected override SignInResult GetSignInResult(string input, out string message)
        {
            if (TryExtractText(input, ExtractTextType.Success, out message)
                || TryExtractText(input, ExtractTextType.AlertMessage, out message))
                return ParseToSignInResult(message);
            else
                throw new FormatException();
        }

        protected override WebPageToken GetWebPageToken(string input)
        {
            if (TryExtractText(input, ExtractTextType.EventValidation, out string eventValidation)
                && TryExtractText(input, ExtractTextType.ViewState, out string viewState))
                return new WebPageToken(eventValidation, viewState);
            else
                throw new FormatException();
        }

        private static IEnumerable<DateTime> GetExceptionalDates(string input)
        {
            string pattern = @"var disabledSpecificDays ?= ?\[ ?(.+) ?\];";
            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                var days = new List<DateTime>();
                foreach (string s in match.Groups[1].Value.Split(','))
                {
                    if (DateTime.TryParse(s.Trim(' ', '"'), out DateTime date))
                        days.Add(date);
                    else
                        return Enumerable.Empty<DateTime>();
                }
                return days;
            }
            else
                return Enumerable.Empty<DateTime>();
        }

        private static SignInResult ParseToSignInResult(string input)
        {
            switch (input)
            {
                case "登打成功":
                case "補登成功":
                    return SignInResult.Success;

                case "帳號密碼錯誤":
                    return SignInResult.InvalidCredential;

                case "今日已簽到":
                    return SignInResult.DuplicateSignIn;

                default:
                    return SignInResult.Other;
            }
        }

        private static bool TryExtractText(string input, ExtractTextType type, out string extractedText)
        {
            extractedText = string.Empty;
            if (input == null) throw new ArgumentNullException();
            string pattern;
            switch (type)
            {
                case ExtractTextType.AlertMessage:
                    pattern = @"<script>alert\('(\S*)'\)</script>";
                    break;

                case ExtractTextType.EventValidation:
                    pattern = @"id=""__EVENTVALIDATION"" value=""([\w+-=/]+)""";
                    break;

                case ExtractTextType.Success:
                    pattern = @"class=""success?"">(補登成功|登打成功)</span>";
                    break;

                case ExtractTextType.ViewState:
                    pattern = @"id=""__VIEWSTATE"" value=""([\w+-=/]+)""";
                    break;

                default:
                    return false;
            }
            Match m = Regex.Match(input, pattern);
            extractedText = m.Groups[1].Value;
            return m.Success;
        }
    }
}