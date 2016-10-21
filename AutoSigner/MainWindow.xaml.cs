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

            var userName = teUserName.Text;
            var password = pePassword.Password;
            var dates = deDate.SelectedDates;
            var url = "http://www.systemweb.com.tw:8080/AddSignInRecord.aspx";

            var data = new System.Collections.Specialized.NameValueCollection();
            data.Add("txtId", userName);
            data.Add("txtPwd", password);
            data.Add("txtDate", "");
            data.Add("txtHour", "10");
            data.Add("txtMinute", "0");
            data.Add("btnAddSignIn", "確定補登");
            data.Add("__EVENTTARGET", "");
            data.Add("__EVENTARGUMENT", "");
            data.Add("__VIEWSTATE", "L9/eyelMZyZyPhnM2YwvwndTBG+W5DWS05eW8nuh81CLcv0v17zNQdwucGwLNs5//fqkcjLvm67XN/bjuLsneNfXt1D5Ly2UfqNOcV8ihzOA9F004RqOpf1r5qSJHW6wAMGDGMgcLzlg4bcyYq6TIkqXo0Js2nHS7B8qf8sU+Z5hhck90y/W/dRsETDpEIJO");
            data.Add("__EVENTVALIDATION", "c2REKaN0MKv4N/fO//cWXewb7eriLOLLY58Bv/TyTkW27jIDqbuy2jLOwRzGb9zx195pTW093Gq/PmC28FL9bicRiDJWPlwU3P+aPBb7i4aWZZ1CHyvvJ/tcwdlvMJcm/ZcOc6EIvw4Wm118VpXVgcrfmcJUY8RBLBE/72efTpnCku6AtTXZD+CzULnN4GCa4WbdPVZoRF+7E4r7n6kCt+kBs6Tv5CKFA84YdXSWPns=");

            var client = new CookieAwareWebClient();

            var results = new List<string>();

            foreach (var date in dates.OrderBy(d => d))
            {
                string result;
                data["txtDate"] = date.ToString("yyyy-MM-dd");
                client.Login(url, data, out result);
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
            MessageBox.Show(string.Format("{0}筆資料中，有{1}筆失敗：\r\n{2}",
                dates.Count,
                results.Count,
                string.Join("\r\n", results)));
        }

        private string GetAlertMessage(string input)
        {
            string pattern = @"<script>alert\('(\S*)'\)</script>";
            Match m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[1].Value : null;
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