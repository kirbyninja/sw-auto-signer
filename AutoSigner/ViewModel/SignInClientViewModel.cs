using AutoSigner.Component;
using AutoSigner.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoSigner.ViewModel
{
    internal class SignInClientViewModel : ViewModelBase
    {
        private readonly SignInClient client;
        private readonly ObservableCollection<SignInResultViewModel> results;
        private readonly ICommand signInCommand;

        private int hour = 10;
        private int minute = 0;
        private DateTime[] selectedDates;
        private string userName;

        public SignInClientViewModel() : this(new SystemwebSignInClient())
        { }

        public SignInClientViewModel(SignInClient client)
        {
            this.client = client;
            results = new ObservableCollection<SignInResultViewModel>();
            signInCommand = new RelayCommand(SignIn, CanSignIn);
        }

        public bool ApplyDateRestriction
        {
            get { return client.ApplyDateRestriction; }
            set
            {
                if (client.ApplyDateRestriction != value)
                {
                    client.ApplyDateRestriction = value;
                    OnPropertyChanged(nameof(ApplyDateRestriction));
                }
            }
        }

        public string Hour
        {
            get { return hour.ToString(); }
            set
            {
                if (int.TryParse(value, out int newHour))
                {
                    if (newHour > 18) newHour = 18;
                    else if (newHour < 8) newHour = 8;

                    SetProperty(nameof(Hour), ref hour, newHour);
                }
            }
        }

        public string Minute
        {
            get { return minute.ToString(); }
            set
            {
                if (int.TryParse(value, out int newMinute))
                {
                    if (newMinute > 59) newMinute = 59;
                    else if (newMinute < 0) newMinute = 0;

                    SetProperty(nameof(Minute), ref minute, newMinute);
                }
            }
        }

        public ICollection<SignInResultViewModel> Results => results;

        public SecureString SecurePassword { private get; set; }

        public ICommand SignInCommand => signInCommand;

        public string UserName
        {
            get => userName;
            set => SetProperty(nameof(UserName), ref userName, value);
        }

        public void Calendar_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.Calendar calendar)
            {
                calendar.SelectedDatesChanged -= Calendar_SelectedDatesChanged;
                selectedDates = calendar.SelectedDates.OrderBy(date => date).
                    Where(date => !ApplyDateRestriction || client.IsDateAllowed(date)).ToArray();

                calendar.SelectedDates.Clear();
                foreach (DateTime date in selectedDates)
                    calendar.SelectedDates.Add(date);

                calendar.SelectedDatesChanged += Calendar_SelectedDatesChanged;
            }
        }

        private bool CanSignIn(object parameter)
        {
            return !string.IsNullOrWhiteSpace(userName)
                && SecurePassword?.Length > 0
                && selectedDates?.Count() > 0;
        }

        private void SignIn(object parameter)
        {
            results.Clear();
            var credential = new NetworkCredential(userName, SecurePassword);
            foreach (DateTime date in selectedDates)
            {
                DateTime dateTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

                var result = client.SignIn(credential, dateTime, out string message);

                results.Add(new SignInResultViewModel(date, result == SignInResult.Success, message));

                // 若帳號密碼有誤就不再繼續之後的嘗試，以免被封鎖。
                if (result == SignInResult.InvalidCredential)
                {
                    // 先暫時把顯示結果的程式碼放在這。
                    System.Windows.MessageBox.Show(message);
                    return;
                }
            }

            // 先暫時把顯示結果的程式碼放在這。
            System.Windows.MessageBox.Show(string.Format("{0}筆資料中，有{1}筆成功、{2}筆失敗：\r\n{3}",
                results.Count,
                results.Count(r => r.Success),
                results.Count(r => !r.Success),
                string.Join("\r\n", results)));
        }
    }
}