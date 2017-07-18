using AutoSigner.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace AutoSigner.View
{
    /// <summary>
    /// Interaction logic for SignInClientView.xaml
    /// </summary>
    public partial class SignInClientView : UserControl
    {
        private readonly SignInClientViewModel viewModel;

        public SignInClientView()
        {
            InitializeComponent();
            viewModel = TryFindResource("client") as SignInClientViewModel ?? throw new ArgumentException();
            calendar.SelectedDatesChanged += viewModel.Calendar_SelectedDatesChanged;
            checkBox.Checked += (sender, e) => viewModel.Calendar_SelectedDatesChanged(calendar, null);
            calendar.SelectedDate = DateTime.Today;
        }

        private void Calendar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Calendar calendar && Mouse.Captured is CalendarItem)
            {
                var size = Mouse.GetPosition(calendar);
                if (size.X > 0
                    && size.Y > 0
                    && size.X < calendar.RenderSize.Width
                    && size.Y < calendar.RenderSize.Height)
                    Mouse.Capture(null);
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
                viewModel.SecurePassword = passwordBox.SecurePassword;
        }
    }
}