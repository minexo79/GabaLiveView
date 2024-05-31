using System;
using System.Collections.Generic;
using System.ComponentModel;
using Timer = System.Timers.Timer;
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

        public EventHandler onVideoDrawFrontend;

        [ObservableProperty]
        private VideoReceiveArgs receiveArgs = null;

        [ObservableProperty]
        private string rtspUrl = "rtsp://localhost:554/mihoyo";

        [ObservableProperty]
        private bool isButtonOpenEnabled = true;

        [ObservableProperty]
        private bool isButtonStopEnabled = true;

        [ObservableProperty]
        private string videoResolution = "";

        [ObservableProperty]
        private string videoFramerate = "";

        [ObservableProperty]
        private string videoBitrate = "";

        Timer infoTimer = new Timer();

        public MainWindowViewModel()
        {
            infoTimer.Interval = 1000;
            infoTimer.Elapsed += updateVideoInfoTimer;
        }

        private void updateVideoInfoTimer(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (ReceiveArgs != null)
            {
                VideoResolution = ReceiveArgs.width + "x" + ReceiveArgs.height;
                VideoFramerate = ReceiveArgs.framerate.ToString("0.00") + " Fps";
                VideoBitrate = ReceiveArgs.bitrate.ToString("0.00") + " kbps";
            }
        }

        [RelayCommand]
        public void ButtonOpen()
        {
            IsButtonOpenEnabled = false;

            videoCore = new VideoCore(RtspUrl);
            videoCore.OnVideoReceived += videoCore_OnVideoReceived;
            videoCore.Start();

            infoTimer.Start();
        }

        [RelayCommand]
        public void ButtonStop()
        {
            IsButtonOpenEnabled = true;

            if (videoCore != null)
            {
                videoCore.OnVideoReceived -= videoCore_OnVideoReceived;
                videoCore.Stop();

                infoTimer.Stop();
            }
        }

        private void videoCore_OnVideoReceived(object? sender, VideoReceiveArgs e)
        {
            ReceiveArgs = e;

            onVideoDrawFrontend?.Invoke(this, null);
        }
    }
}
