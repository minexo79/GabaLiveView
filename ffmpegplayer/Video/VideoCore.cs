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
        static object lockObj = new object();

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
            if (DateTime.Now.Subtract(lastFrameDateTime).TotalSeconds > 10)
            {
                Stop();
                Thread.Sleep(500);
                Start();
            }
        }

        public void Start()
        {
            cts = new CancellationTokenSource();

            startPlay = new Task(() => GetImage());
            startPlay.Start();


            decodeImage = new Task(() => startFFmpeg());
            decodeImage.Start();


            if (connectLostTimer == null)
            {
                connectLostTimer = new Timer();
                connectLostTimer.Interval = 10000;
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
                lock (lockObj)      // for thread safe, process one thing at a time
                {
                    SKBitmap bmp = new SKBitmap(width, height);
                    SKImageInfo imageInfo = new SKImageInfo(width, height, SKColorType.Rgb888x, SKAlphaType.Opaque);

                    bmp.InstallPixels(imageInfo, buffer, width * 4);

                    decodeBmpQueue.Enqueue(bmp);
                    lastFrameDateTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void GetImage()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    lock (lockObj)  // for thread safe, process one thing at a time
                    {
                        if (decodeBmpQueue.Count > 0)
                        {
                            videoReceiveArgs = new VideoReceiveArgs();
                            videoReceiveArgs.videoBmp = decodeBmpQueue.Dequeue();

                            if (videoReceiveArgs.videoBmp != null)
                            {
                                if (OnVideoReceived != null)
                                    OnVideoReceived(this, videoReceiveArgs);
                            }
                        }
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
                startPlay.Wait(100, cts.Token);
                decodeImage.Wait(100, cts.Token);
                cts.Cancel();

                decodeBmpQueue.Clear();

                ffmpegReceive.Stop();
                ffmpegReceive.Dispose();

                connectLostTimer.Stop();
            }
        }
    }
}
