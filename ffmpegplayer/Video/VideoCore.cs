using ffmpegplayer.Video.Decode;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace ffmpegplayer.Video
{
    public class VideoReceiveArgs : EventArgs
    {
        public SKBitmap videoBmp { get; set; }
    }

    internal class VideoCore
    {
        public event EventHandler<VideoReceiveArgs> OnVideoReceived;

        string RtspUrl;
        FFmpegReceive ffmpegReceive;

        CancellationTokenSource cts;
        VideoReceiveArgs videoReceiveArgs;
        Queue<SKBitmap> decodeBmpQueue = new Queue<SKBitmap>();

        Task startPlay, decodeImage;

        Timer connectLostTimer;
        DateTime lastFrameDateTime;

        public VideoCore(string url) 
        { 
            RtspUrl = url;
        }

        public void Dispose()
        {
            ffmpegReceive?.Dispose();
        }

        private void connectLostTimerCallback(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if ((DateTime.Now - lastFrameDateTime).Seconds > 1)
            {
                Stop();
                Start();
            }
        }

        public void Start()
        {
            cts = new CancellationTokenSource();

            startPlay = new Task(() => pushFrameToFrontend());
            startPlay.Start();


            decodeImage = new Task(() => startFFmpeg());
            decodeImage.Start();


            if (connectLostTimer == null)
            {
                connectLostTimer = new Timer();
                connectLostTimer.Interval = 5000;
                connectLostTimer.Elapsed += connectLostTimerCallback;
                connectLostTimer.AutoReset = true;
            }

            connectLostTimer.Start();
            lastFrameDateTime = DateTime.Now;
        }

        void startFFmpeg()
        {
            ffmpegReceive = new FFmpegReceive(RtspUrl, onFrameCallback);
            ffmpegReceive.Start();

            return;
        }

        void onFrameCallback(int width, int height, IntPtr buffer, int size)
        {
            try
            {
                SKBitmap bmp = new SKBitmap(width, height);
                SKImageInfo imageInfo = new SKImageInfo(width, height, SKColorType.Rgb888x, SKAlphaType.Opaque);

                bmp.InstallPixels(imageInfo, buffer, width * 4);

                if (decodeBmpQueue.Count > 5)
                {
                    decodeBmpQueue.Dequeue();
                }

                decodeBmpQueue.Enqueue(bmp);
                lastFrameDateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void pushFrameToFrontend()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (decodeBmpQueue.Count > 0)
                    {
                        videoReceiveArgs = new VideoReceiveArgs();
                        videoReceiveArgs.videoBmp = decodeBmpQueue.Dequeue();

                        if (OnVideoReceived != null)
                            OnVideoReceived(this, videoReceiveArgs);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("==> " + ex.Message);
                }

                Thread.Sleep(16);   // 60fps
            }

            return;
        }

        public void Stop()
        {
            if (ffmpegReceive != null)
            {
                ffmpegReceive.Stop();

                startPlay.Wait(1000, cts.Token);
                decodeImage.Wait(1000, cts.Token);
                cts.Cancel();

                ffmpegReceive.Dispose();
                connectLostTimer.Stop();
            }
        }
    }
}
