using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GabaLiveView
{
    internal partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
    {
        [ObservableProperty]
        private string notifyMessage = "";

        [ObservableProperty]
        private Visibility notifyVisible = Visibility.Hidden;
    }
}
