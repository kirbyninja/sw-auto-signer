using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSigner.ViewModel
{
    internal abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (GetType().GetProperty(propertyName) == null)
                throw new ArgumentOutOfRangeException(nameof(propertyName));
            else
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void SetProperty<T>(string propertyName, ref T currentValue, T newValue)
        {
            if (!Equals(currentValue, newValue))
            {
                currentValue = newValue;
                OnPropertyChanged(propertyName);
            }
        }
    }
}