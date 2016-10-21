using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        private string hour;
        private string minute;
        private string password;
        private string userName;

        public MainWindow()
        {
            InitializeComponent();
            deDate.SelectedDate = DateTime.Today;
            deDate.PreviewMouseUp += (s, e) => Mouse.Capture(null);

            ceHour.PreviewTextInput += (s, e) =>
            {
                e.Handled = !IsTextAllowed(e.Text);
            };
        }

        private static bool IsTextAllowed(string input)
        {
            Regex regex = new Regex("[^0-9]+"); //regex that matches disallowed text
            return !regex.IsMatch(input);
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

            userName = teUserName.Text;
            password = pePassword.Password;
            hour = ceHour.Text;
            minute = ceMinute.Text;

            var dates = deDate.SelectedDates;

            var fakeData = new System.Collections.Specialized.NameValueCollection();
            fakeData.Add("ScriptManager1", "UpdatePanel1|Timer1");
            fakeData.Add("txtId", userName);
            fakeData.Add("txtPwd", password);
            fakeData.Add("txtDate", "");
            fakeData.Add("txtHour", hour);
            fakeData.Add("txtMinute", minute);
            fakeData.Add("__EVENTTARGET", "Timer1");
            fakeData.Add("__EVENTARGUMENT", "");
            fakeData.Add("__VIEWSTATE", "lBdSiGyvm2g+j9WFpi08PwELUL3F9jR+UFLOiqpg68YITGiJl3AV4PA63zR5IvLu04hagDbul363PhuuKs3NPXSm1Ah971sEcd59DxVWragla8WcpT/zR/0sdVSirGOCznRfEyAi85xOHA573VJhO+I3oALuuQ4a9AItEZV9p5190nhg46t9b1ckYzVd6Vsu");
            fakeData.Add("__EVENTVALIDATION", "v/ZUk7/+dWiddmwCOQ7swPhy8JhEbSNoU+pSgV/RmhQbuDY1oBvpG8uwFJzHNAdv6tOWw1/eRA0lnELzEUblDoHLWLjSclIkSALnLmdrX840FhPSk1eLMUIcFT5dfF7kXiXDIs27NuBntYB732N7zyP42iWd6h3y58MPBbX2ExtGZJRwZO6TbfugEc/DtGcYTjmgmAy6RVch9lwcnOtTOE6wPI7uNToXmFIjgDZGVVM=");
            //fakeData.Add("__ASYNCPOST", "true");

            var data = new System.Collections.Specialized.NameValueCollection();
            data.Add("txtId", userName);
            data.Add("txtPwd", password);
            data.Add("txtDate", "");
            data.Add("txtHour", hour);
            data.Add("txtMinute", minute);
            data.Add("btnAddSignIn", "確定補登");
            data.Add("__EVENTTARGET", "");
            data.Add("__EVENTARGUMENT", "");
            data.Add("__VIEWSTATE", "");
            data.Add("__EVENTVALIDATION", "");

            var client = new CookieAwareWebClient();
            var results = new List<string>();

            foreach (var date in dates.OrderBy(d => d))
            {
                fakeData["txtDate"] = date.ToString("yyyy-MM-dd");
                string trialResult = client.Post(url, fakeData);
                if (trialResult != null)
                {
                    data["txtDate"] = date.ToString("yyyy-MM-dd");
                    data["__VIEWSTATE"] = GetViewState(trialResult);
                    data["__EVENTVALIDATION"] = GetEventValidation(trialResult);
                    string result = client.Post(url, data);
                    if (result != null)
                    {
                    }
                    if ((result = GetAlertMessage(result)) != null)
                    {
                        if (result == "帳號密碼錯誤")
                        {
                            MessageBox.Show("帳號密碼錯誤");
                            return;
                        }
                        results.Add(string.Format("{0}\t{1}", date.ToString("yyyy-MM-dd"), result));
                    }
                }
            }
            MessageBox.Show(string.Format("{0}筆資料中，有{1}筆失敗：\r\n{2}",
                dates.Count,
                results.Count,
                string.Join("\r\n", results)));
        }

        private string GetAlertMessage(string input)
        {
            if (input == null) return null;
            string pattern = @"<script>alert\('(\S*)'\)</script>";
            Match m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[1].Value : null;
        }

        private string GetEventValidation(string input)
        {
            if (input == null) return string.Empty;
            //string pattern = @"__EVENTVALIDATION\|([\w+-=/]+)\|";
            string pattern = @"id=""__EVENTVALIDATION"" value=""([\w+-=/]+)""";
            Match m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        private string GetViewState(string input)
        {
            if (input == null) return string.Empty;
            //string pattern = @"__VIEWSTATE\|([\w+-=/]+)\|";
            string pattern = @"id=""__VIEWSTATE"" value=""([\w+-=/]+)""";
            Match m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[1].Value : string.Empty;
        }

        // Use the DataObject.Pasting Handler
        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
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