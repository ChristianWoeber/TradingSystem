using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Trading.DataStructures.Enums;

namespace Trading.UI.Wpf.ViewModels
{
    public class IndexBacktestSettings : IIndexBacktestSettings, INotifyPropertyChanged
    {
        private IndexType _typeOfIndex;

        public IndexBacktestSettings()
        {
            TypeOfIndex = IndexType.EuroStoxx50;
        }

        public IndexType TypeOfIndex
        {
            get => _typeOfIndex;
            set
            {
                if (value == _typeOfIndex)
                    return;
                _typeOfIndex = value;
                OnPropertyChanged();
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion

    }
}