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
using GabaLiveView.Video.FFmpegVideoCore;
using GabaLiveView.Video.FFmpegVideoCore.Decode;
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
        IVideoCore videoCore;
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
        private Visibility topBarVisible = Visibility.Visible;

        [ObservableProperty]
        private Visibility topButtonVisible = Visibility.Hidden;

        [ObservableProperty]
        private string videoResolution = "N/A";

        [ObservableProperty]
        private string videoFramerate = "N/A";

        [ObservableProperty]
        private string videoBitrate = "N/A";

        [ObservableProperty]
        private string videoFormat = "N/A";

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

            // 2024.7.5 Blackcat: Use Timer For Refreshing The Video (Prevent Calling Dispatcher Too Much Cause Playing Slowly)
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMilliseconds(16);
            refreshTimer.Tick += RefreshTimer_Tick;

        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            // if frame is new, draw it
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
                videoCore = new FFmpegVideoCore(protocol + "://" + StreamUrl);
                videoCore.OnVideoReceived   += videoCore_OnVideoReceived;
                videoCore.OnLogReceived     += videoCore_OnLogReceived;
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

        [RelayCommand]
        public void ButtonHideTopBar()
        {
            TopBarVisible = Visibility.Collapsed;
            TopButtonVisible = Visibility.Visible;
        }

        [RelayCommand]
        public void ButtonShowTopBar()
        {
            TopBarVisible = Visibility.Visible;
            TopButtonVisible = Visibility.Hidden;
        }


        private void videoCore_OnVideoReceived(object? sender, VideoReceiveArgs e)
        {
            ReceiveArgs = e;
            isDrawing = true;
        }

        private void videoCore_OnLogReceived(object? sender, LogArgs e)
        {
            LogMessage = e.logMessage ?? "";
        }

    }
}
