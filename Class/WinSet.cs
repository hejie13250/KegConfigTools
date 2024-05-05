using System.ComponentModel;

namespace 小科狗配置.Class
{
    public class WinSet : INotifyPropertyChanged
    {
        private static double _blurRadius;

        public double BlurRadius
        {
            get => _blurRadius;
            set
            {
                _blurRadius = value;
                OnPropertyChanged("BlurRadius");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
