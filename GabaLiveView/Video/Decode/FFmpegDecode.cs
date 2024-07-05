using FFmpeg.AutoGen;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace GabaLiveView.Video.Decode
{
    public class VideoReceiveArgs : EventArgs
    {
        public SKBitmap videoBmp { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public float framerate { get; set; }
        public string format { get; set; }
        public float bitrate { get; set; }
    }

    internal unsafe partial class FFmpegHelp : IDisposable
    {
        internal float _bitrate = 0;

        object lockObj = new object();
        bool isBusy = false;
        
        internal unsafe void Decode(AVCodecContext * pCodecContext, AVPacket * pPacket, AVFrame* pFrame, float bitrate)
        {
            if (pPacket->stream_index != videoStreamIndex)
            {
                return;
            }

            _bitrate = bitrate;

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
                    ffmpeg.sws_scale(pConvertContext, pFrame->data, pFrame->linesize, 0,
                                        pCodecContext->height, dstData, dstLinesize);

                    // Console.WriteLine("4");
                    lastFrameDateTime = DateTime.Now;
                    Render((int)width, (int)height, convertedFrameBufferPtr, dstLinesize[0]);
                }
            }
        }

        internal unsafe void Render(int width, int height, IntPtr frameBufferPtr, int rowByte)
        {
            try
            {
                // for thread safe, process one thing at a time
                lock (lockObj)
                {
                    if (videoBmp.InstallPixels(imageInfo, frameBufferPtr, width * 4))
                    {
                        if (videoBmp != null)
                        {
                            // package the video frame
                            VideoReceiveArgs videoReceiveArgs = new VideoReceiveArgs()
                            {
                                videoBmp = videoBmp,
                                width = width,
                                height = height,
                                framerate = framerate,
                                bitrate = _bitrate,
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
