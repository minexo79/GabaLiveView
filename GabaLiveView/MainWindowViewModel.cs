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
using GabaLiveView.Video;
using GabaLiveView.Video.Decode;
using System.Windows;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;
using SkiaSharp.Views;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace GabaLiveView
{
    internal partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
    {
        VideoCore videoCore;
        public bool isDrawing = false;

        [ObservableProperty]
        private VideoReceiveArgs receiveArgs = null;

        [ObservableProperty]
        private int streamProtocol = App.StreamProtocol;

        [ObservableProperty]
        private string streamUrl = App.StreamUrl;

        [ObservableProperty]
        private bool isButtonOpenEnabled = true;

        [ObservableProperty]
        private Visibility infoVisible = Visibility.Hidden;

        [ObservableProperty]
        private string videoResolution = "";

        [ObservableProperty]
        private string videoFramerate = "";

        [ObservableProperty]
        private string videoBitrate = "";

        [ObservableProperty]
        private string videoFormat = "";

        [ObservableProperty]
        private string logMessage = "";

        Timer infoTimer = new Timer();
        DispatcherTimer refreshTimer;
        SKElement frontendCanvas;

        public MainWindowViewModel(ref SKElement canvas)
        {
            frontendCanvas = canvas;

            infoTimer.Interval = 500;
            infoTimer.Elapsed += updateVideoInfoTimer;

            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMilliseconds(16);
            refreshTimer.Tick += RefreshTimer_Tick;

        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;

                frontendCanvas.InvalidateVisual();
            }
        }

        private void updateVideoInfoTimer(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (ReceiveArgs != null)
            {
                VideoResolution = ReceiveArgs.width + "x" + ReceiveArgs.height;
                VideoFramerate  = ReceiveArgs.framerate.ToString("0.00") + " Fps";
                VideoFormat     = ReceiveArgs.format;
                VideoBitrate    = ReceiveArgs.bitrate.ToString("0.00") + " Kbps";
            }
        }

        [RelayCommand]
        public void ButtonOpen()
        {
            IsButtonOpenEnabled = false;

            string protocol = (StreamProtocol == 0) ? "rtsp" :
                              (StreamProtocol == 1) ? "rtmp" : "hls";

            if (videoCore == null)
            {
                videoCore = new VideoCore(protocol + "://" + StreamUrl);
                videoCore.OnVideoReceived   += videoCore_OnVideoReceived;
                videoCore.OnLogReceived     += VideoCore_OnLogReceived;
            }
            else
            {
                videoCore.changeUrl(protocol + "://" + StreamUrl);
            }

            InfoVisible = Visibility.Visible;
            videoCore.Start();
            infoTimer.Start();
            refreshTimer.Start();
        }

        [RelayCommand]
        public void ButtonStop()
        {
            IsButtonOpenEnabled = true;
            isDrawing = false;
            refreshTimer.Stop();

            if (videoCore != null)
            {
                videoCore.Stop();

                // 2024.6.5 Blackcat: Add Blank Frame To Clear The Video
                ReceiveArgs = null;
                frontendCanvas.InvalidateVisual();

                InfoVisible = Visibility.Hidden;
                infoTimer.Stop();
            }
        }

        [RelayCommand]
        public void MenuAbout()
        {
            MessageBox.Show($"Version: {App.ver}\nAuthor: XOT(minexo79)\nMail: minexo79@gmail.com", "About", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        public void OpenRepo()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/minexo79/GabaLiveView",
                UseShellExecute = true
            });
        }

        private void videoCore_OnVideoReceived(object? sender, VideoReceiveArgs e)
        {
            ReceiveArgs = e;

            isDrawing = true;
        }

        private void VideoCore_OnLogReceived(object? sender, LogArgs e)
        {
            LogMessage = e.logMessage ?? "";
        }

    }
}
