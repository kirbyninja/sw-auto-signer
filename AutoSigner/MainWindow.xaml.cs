using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoSigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string url = "http://www.systemweb.com.tw:8080/AddSignInRecord.aspx";

        private IEnumerable<DateTime> exceptionalDates = GetExceptionalDates();
        private int hour = 10;
        private int minute = 0;

        public MainWindow()
        {
            InitializeComponent();
            if (IsDateAllowed(DateTime.Today))
                deDate.SelectedDate = DateTime.Today;
            deDate.SelectedDatesChanged += deDate_SelectedDatesChanged;
            deDate.PreviewMouseUp += deDate_PreviewMouseUp;

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Title = string.Format("{0} {1}", fileVersionInfo.ProductName, fileVersionInfo.FileVersion);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private enum ExtractTextType
        {
            AlertMessage,
            EventValidation,
            Success,
            ViewState,
        }

        public string Hour
        {
            get
            {
                return this.hour.ToString();
            }
            set
            {
                int tempHour;
                if (int.TryParse(value, out tempHour))
                {
                    if (tempHour > 18) tempHour = 18;
                    else if (tempHour < 8) tempHour = 8;

                    if (tempHour != this.hour)
                    {
                        this.hour = tempHour;
                        OnPropertyChanged("Hour");
                    }
                }
            }
        }

        public string Minute
        {
            get
            {
                return this.minute.ToString();
            }
            set
            {
                int tempMinute;
                if (int.TryParse(value, out tempMinute))
                {
                    if (tempMinute > 59) tempMinute = 59;
                    else if (tempMinute < 0) tempMinute = 0;

                    if (tempMinute != this.minute)
                    {
                        this.minute = tempMinute;
                        OnPropertyChanged("Minute");
                    }
                }
            }
        }

        private SelectedDatesCollection SelectedDates { get { return deDate.SelectedDates; } }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string ExtractText(string input, ExtractTextType type)
        {
            string extractedText;
            TryExtractText(input, type, out extractedText);
            return extractedText;
        }

        private static IEnumerable<DateTime> GetExceptionalDates()
        {
            var client = new CookieAwareWebClient();
            string input = client.Get(url);

            string pattern = @"var disabledSpecificDays ?= ?\[ ?(.+) ?\];";
            var match = Regex.Match(input, pattern);
            if (match.Success)
            {
                var days = new List<DateTime>();
                foreach (string s in match.Groups[1].Value.Split(','))
                {
                    DateTime dt;
                    if (DateTime.TryParse(s.Trim(' ', '"'), out dt))
                        days.Add(dt);
                    else
                        return Enumerable.Empty<DateTime>();
                }
                return days;
            }
            else
                return Enumerable.Empty<DateTime>();
        }

        private static bool TryExtractText(string input, ExtractTextType type, out string extractedText)
        {
            extractedText = string.Empty;
            if (input == null) return false;
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

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (teUserName.Text == string.Empty)
            {
                MessageBox.Show("請輸入帳號");
                teUserName.Focus();
                return;
            }

            if (pePassword.Password == string.Empty)
            {
                MessageBox.Show("請輸入密碼");
                pePassword.Focus();
                return;
            }
            if (deDate.SelectedDates.Count <= 0)
            {
                MessageBox.Show("請選擇日期");
                pePassword.Focus();
                return;
            }

            var data = new NameValueCollection();
            data.Add("txtId", teUserName.Text);
            data.Add("txtPwd", pePassword.Password);
            data.Add("txtDate", "");
            data.Add("txtHour", ceHour.Text);
            data.Add("txtMinute", ceMinute.Text);
            data.Add("btnAddSignIn", "確定補登");
            data.Add("__EVENTTARGET", "");
            data.Add("__EVENTARGUMENT", "");
            data.Add("__VIEWSTATE", "");
            data.Add("__EVENTVALIDATION", "");

            var client = new CookieAwareWebClient();
            var messages = new List<Tuple<bool, DateTime, string>>();

            foreach (var date in SelectedDates.OrderBy(d => d))
            {
                string trialResult = client.Get(url);
                if (trialResult != null)
                {
                    data["txtDate"] = date.ToString("yyyy-MM-dd");
                    data["__VIEWSTATE"] = ExtractText(trialResult, ExtractTextType.ViewState);
                    data["__EVENTVALIDATION"] = ExtractText(trialResult, ExtractTextType.EventValidation);
                    string result = client.Post(url, data);
                    string message = string.Empty;
                    if (result != null)
                    {
                        if (TryExtractText(result, ExtractTextType.Success, out message))
                            messages.Add(new Tuple<bool, DateTime, string>(true, date, message));
                        else if (TryExtractText(result, ExtractTextType.AlertMessage, out message))
                        {
                            if (message == "帳號密碼錯誤")
                            {
                                MessageBox.Show(message);
                                return;
                            }
                            messages.Add(new Tuple<bool, DateTime, string>(false, date, message));
                        }
                        else
                        {
                            MessageBox.Show(result);
                            return;
                        }
                    }
                }
            }
            MessageBox.Show(string.Format("{0}筆資料中，有{1}筆成功、{2}筆失敗：\r\n{3}",
                SelectedDates.Count,
                messages.Count(t => t.Item1),
                messages.Count(t => !t.Item1),
                string.Join("\r\n", messages.Select(t =>
                    string.Format("{0}\t{1}", t.Item2.ToString("yyyy-MM-dd"), t.Item3)))));
        }

        private void chkRestrictDate_Checked(object sender, RoutedEventArgs e)
        {
            if (chkRestrictDate.IsChecked ?? false)
            {
                try
                {
                    exceptionalDates = GetExceptionalDates();
                }
                catch (Exception ex)
                {
                    exceptionalDates = Enumerable.Empty<DateTime>();

                    chkRestrictDate.IsEnabled = false;
                    chkRestrictDate.IsChecked = false;

                    MessageBox.Show(string.Format("發生以下錯誤，無法讀取例外日期。\n{0}", ex.Message));
                }

                deDate_SelectedDatesChanged(null, null);
            }
        }

        private void deDate_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured is CalendarItem)
            {
                var size = Mouse.GetPosition(deDate);
                if (size.X > 0 && size.Y > 0 && size.X < deDate.RenderSize.Width && size.Y < deDate.RenderSize.Height)
                    Mouse.Capture(null);
            }
        }

        private void deDate_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            deDate.SelectedDatesChanged -= deDate_SelectedDatesChanged;
            var dates = SelectedDates.Where(date => IsDateAllowed(date)).ToArray();
            SelectedDates.Clear();
            foreach (DateTime date in dates)
                SelectedDates.Add(date);
            deDate.SelectedDatesChanged += deDate_SelectedDatesChanged;
        }

        private bool IsDateAllowed(DateTime date)
        {
            if (chkRestrictDate.IsChecked ?? false)
            {
                bool isHoliday = date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday;

                if (exceptionalDates.Contains(date))
                    return isHoliday;
                else
                {
                    DateTime lastMonth = DateTime.Today.AddMonths(-1);
                    return !isHoliday
                        && date.Date >= new DateTime(lastMonth.Year, lastMonth.Month, 26)
                        && date.Date <= DateTime.Today;
                }
            }
            else
                return true;
        }
    }
}