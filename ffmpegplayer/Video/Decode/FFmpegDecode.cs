using FFmpeg.AutoGen;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ffmpegplayer.Video.Decode
{
    public class VideoReceiveArgs : EventArgs
    {
        public SKBitmap videoBmp { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public float framerate { get; set; }
        public string format { get; set; }
    }

    internal unsafe partial class FFmpegHelp
    {
        object lockObj = new object();
        bool isBusy = false;

        internal unsafe void Decode(AVCodecContext * pCodecContext, AVPacket * pPacket, AVFrame* pFrame)
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }

            if (pPacket->stream_index != videoStreamIndex)
            {
                return;
            }

            // Console.WriteLine("1");
            if (ffmpeg.avcodec_send_packet(pCodecContext, pPacket) == 0)
            {
                // Console.WriteLine("2");
                // 如果Packet有破損，則丟棄
                if (pPacket->flags == ffmpeg.AV_PKT_FLAG_CORRUPT)
                    return;

                if (ffmpeg.avcodec_receive_frame(pCodecContext, pFrame) == 0)
                {
                    // Console.WriteLine("3");
                    // convert frame YUV->RGB
                    ffmpeg.sws_scale(pConvertContext, pFrame->data, pFrame->linesize, 0,
                                        pCodecContext->height, dstData, dstLinesize);

                    // Console.WriteLine("4");
                    lastFrameDateTime = DateTime.Now;
                    Render((int)width, (int)height, convertedFrameBufferPtr, dstLinesize[0]);

                    ffmpeg.av_packet_unref(pPacket);
                }
            }
        }

        internal unsafe void Render(int width, int height, IntPtr frameBufferPtr, int rowByte)
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }

            try
            {
                // for thread safe, process one thing at a time
                lock (lockObj)
                {
                    SKBitmap bmp = new SKBitmap(width, height);
                    SKImageInfo imageInfo = new SKImageInfo(width, height, SKColorType.Rgb888x, SKAlphaType.Opaque);

                    if (bmp.InstallPixels(imageInfo, frameBufferPtr, width * 4))
                    {
                        if (bmp != null)
                        {
                            // package the video frame
                            VideoReceiveArgs videoReceiveArgs = new VideoReceiveArgs() 
                            {
                                videoBmp = bmp,
                                width = width,
                                height = height,
                                framerate = framerate,
                                format = codecName
                            };

                            // use event to send video frame
                            if (OnVideoReceived != null)
                                OnVideoReceived?.Invoke(this, videoReceiveArgs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
