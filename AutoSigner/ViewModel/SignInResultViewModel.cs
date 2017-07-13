using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSigner.ViewModel
{
    internal class SignInResultViewModel : ViewModelBase
    {
        public SignInResultViewModel(DateTime date, bool successs, string message)
        {
            Date = date;
            Success = successs;
            Message = message;
        }

        public DateTime Date { get; protected set; }

        public string Message { get; protected set; }

        public bool Success { get; protected set; }

        public override string ToString()
        {
            return $"{Date:yyyy-MM-dd}\t{Message}";
        }
    }
}