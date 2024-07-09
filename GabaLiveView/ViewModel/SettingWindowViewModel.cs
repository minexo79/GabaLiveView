using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GabaLiveView
{
    internal partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
    {
        [ObservableProperty]
        private int streamProtocol = App.StreamProtocol;

        [ObservableProperty]
        private string streamUrl = App.StreamUrl;

        [ObservableProperty]
        private string savePath = App.SavePath;

        [RelayCommand]
        public void ButtonSave()
        {
            App.SaveIni(StreamProtocol, StreamUrl, SavePath);

            settingWindow.Close();
            settingWindow = null;
        }

        [RelayCommand]
        public void ButtonBrowse()
        {

        }
    }
}
