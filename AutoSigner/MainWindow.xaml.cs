using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
    public partial class MainWindow : Window
    {
        private const string url = "http://www.systemweb.com.tw:8080/AddSignInRecord.aspx";

        public MainWindow()
        {
            InitializeComponent();
            deDate.SelectedDate = DateTime.Today;
            deDate.PreviewMouseUp += (s, e) => { if (Mouse.Captured is CalendarItem) Mouse.Capture(null); };
            ceHour.PreviewTextInput += TextBox_PreviewTextInput;
            ceMinute.PreviewTextInput += TextBox_PreviewTextInput;
        }

        private enum ExtractTextType
        {
            AlertMessage,
            EventValidation,
            Success,
            ViewState,
        }

        private enum TimeTextType
        {
            Hour,
            Minute,
            None,
        }

        private SelectedDatesCollection SelectedDates { get { return deDate.SelectedDates; } }

        private static string ExtractText(string input, ExtractTextType type)
        {
            string extractedText;
            TryExtractText(input, type, out extractedText);
            return extractedText;
        }

        private static bool IsTextAllowed(string input, TimeTextType type)
        {
            int number;
            if (int.TryParse(input, out number) && number.ToString() == input)
            {
                switch (type)
                {
                    case TimeTextType.Hour:
                        return number <= 18 && number >= 8;

                    case TimeTextType.Minute:
                        return number < 60 && number >= 0;

                    default:
                        return true;
                }
            }
            else
                return false;
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

        private TimeTextType GetTimeTextType(object sender)
        {
            if (sender == ceHour)
                return TimeTextType.Hour;
            else if (sender == ceMinute)
                return TimeTextType.Minute;
            else
                return TimeTextType.None;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox)
            {
                var textBox = sender as TextBox;

                string input = textBox.Text.Insert(textBox.CaretIndex, e.Text);

                e.Handled = !IsTextAllowed(input, GetTimeTextType(sender));
            }
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text, GetTimeTextType(sender)))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}