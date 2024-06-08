using DaFenPlayer.Video.Decode;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace DaFenPlayer.Video
{
    internal class VideoCore
    {
        public event EventHandler<VideoReceiveArgs> OnVideoReceived;
        public event EventHandler<LogArgs> OnLogReceived;

        string streamUrl;
        FFmpegHelp ffmpegHelp;

        Task startPlay;
        Timer connectLostTimer;

        public VideoCore(string url) 
        { 
            streamUrl = url;
        }

        public void Dispose()
        {
            ffmpegHelp?.Dispose();
        }

        private void connectLostTimerCallback(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (DateTime.Now.Subtract(ffmpegHelp.lastFrameDateTime).TotalSeconds > 10)
            {
                Console.WriteLine("==> Timeout, Restart...");

                Stop();
                Start();
            }
        }

        public void Start()
        {
            ffmpegHelp  = new FFmpegHelp(streamUrl, OnVideoReceived, OnLogReceived);
            startPlay   = new Task(() => ffmpegHelp.StartFFmpeg());
            startPlay.Start();


            if (connectLostTimer == null)
            {
                connectLostTimer = new Timer();
                connectLostTimer.Interval = 1000;
                connectLostTimer.Elapsed += connectLostTimerCallback;
                connectLostTimer.AutoReset = true;
            }

            connectLostTimer.Start();
        }

        public void Stop()
        {
            if (ffmpegHelp != null)
            {
                connectLostTimer.Stop();

                ffmpegHelp.StopFFmpeg();
                ffmpegHelp.Dispose();
                ffmpegHelp = null;

                startPlay.Wait(100);
            }
        }
    }
}
