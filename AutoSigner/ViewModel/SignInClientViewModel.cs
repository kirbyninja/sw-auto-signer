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
        private readonly ICommand signInCommand;

        private int hour = 10;
        private int minute = 0;
        private bool isBusy = false;
        private bool postponedSignIn = true;
        private double progress = 0d;
        private DateTime[] selectedDates;
        private string userName;

        public SignInClientViewModel() : this(new SystemwebSignInClient())
        { }

        public SignInClientViewModel(SignInClient client)
        {
            this.client = client;
            signInCommand = new RelayCommand(async p => await SignIn(p), CanSignIn);
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

        public bool PostponedSignIn
        {
            get => postponedSignIn;
            set => SetProperty(nameof(PostponedSignIn), ref postponedSignIn, value);
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

        public Action<string> Logger { get; set; }

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

        public double Progress
        {
            get => progress;
            set => SetProperty(nameof(Progress), ref progress, value);
        }

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
                && selectedDates?.Count() > 0
                && !isBusy;
        }

        private async Task SignIn(object parameter)
        {
            isBusy = true;
            var results = new List<SignInResultViewModel>();
            var credential = new NetworkCredential(userName, SecurePassword);
            var random = new Random();

            foreach (DateTime date in selectedDates)
            {
                DateTime dateTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);

                if (PostponedSignIn) await Task.Delay(random.Next(1000, 3000));

                var result = client.SignIn(credential, dateTime, out string message);

                // 若帳號密碼有誤就不再繼續之後的嘗試，以免被封鎖。
                if (result == SignInResult.InvalidCredential)
                {
                    Logger?.Invoke(message);
                    Progress = 1.0;
                    isBusy = false;
                    return;
                }
                results.Add(new SignInResultViewModel(date, result == SignInResult.Success, message));
                Progress = 1.0 * results.Count() / selectedDates.Count();
            }

            Logger?.Invoke(string.Format("{0}筆資料中，有{1}筆成功、{2}筆失敗：\r\n{3}",
                results.Count,
                results.Count(r => r.Success),
                results.Count(r => !r.Success),
                string.Join("\r\n", results)));
            isBusy = false;
        }
    }
}
