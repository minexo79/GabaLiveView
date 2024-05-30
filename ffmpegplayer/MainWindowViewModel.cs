using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ffmpegplayer.Video;

namespace ffmpegplayer
{
    internal partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
    {

        VideoCore videoCore;

        [ObservableProperty]
        private VideoReceiveArgs receiveArgs = null;

        [ObservableProperty]
        private string rtspUrl = "rtsp://localhost:554/mihoyo";

        [ObservableProperty]
        private bool isButtonOpenEnabled = true;

        [ObservableProperty]
        private bool isButtonStopEnabled = true;

        [RelayCommand]
        public void ButtonOpen()
        {
            IsButtonOpenEnabled = false;

            videoCore = new VideoCore(RtspUrl);
            videoCore.OnVideoReceived += videoCore_OnVideoReceived;
            videoCore.Start();
        }

        [RelayCommand]
        public void ButtonStop()
        {
            IsButtonOpenEnabled = true;

            if (videoCore != null)
            {
                videoCore.OnVideoReceived -= videoCore_OnVideoReceived;
                videoCore.Stop();
            }
        }

        private void videoCore_OnVideoReceived(object? sender, VideoReceiveArgs e)
        {
            ReceiveArgs = e;
        }
    }
}
