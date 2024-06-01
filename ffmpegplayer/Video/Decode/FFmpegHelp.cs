using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using ffmpegplayer.Video.Utilities;
using SkiaSharp;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ffmpegplayer.Video.Decode
{
    internal unsafe class FFmpegHelp
    {
        // ffmpeg
        int errCode = 0;
        int videoStreamIndex = -1;
        AVStream* pStream;
        AVFormatContext * pFc;
        AVCodecID codec_id;
        AVCodecParameters pCodecParams;
        AVCodecContext* pCodecContext;
        SwsContext* pConvertContext;
        AVPixelFormat srcPixfmt = AVPixelFormat.AV_PIX_FMT_NONE;
        AVPixelFormat dstPixfmt = AVPixelFormat.AV_PIX_FMT_NONE;
        IntPtr convertedFrameBufferPtr = IntPtr.Zero;
        byte_ptrArray4 dstData;
        int_array4 dstLinesize;

        internal float width = 0;
        internal float height = 0;
        internal float framerate = 0;
        internal string codecName = "";

        // rtsp url
        string RtspUrl;

        // delegate
        internal delegate void ReceiveCallback(int width, int height, IntPtr buffer, int size);
        private ReceiveCallback Callback;

        // while loop
        bool CanRun;

        public FFmpegHelp(string _url, ReceiveCallback _callback)
        {
            RtspUrl = _url;
            Callback = _callback;

            CanRun = true;
        }

        internal void Dispose()
        {
            Callback = null;
        }

        void FFmpegLogCallback(void* ptr, int level, string fmt, byte* vl)
        {
            // get log
            byte[] bytes = new byte[1024];
            fixed (byte* p = bytes)
            {
                ffmpeg.av_log_format_line(ptr, level, fmt, vl, p, bytes.Length, null);
                string message = Encoding.UTF8.GetString(bytes);
                Console.WriteLine("==> " + message);
            }
        }

        void Register()
        {
            // find ffmpeg binaries
            FFmpegBinaryHelp.RegisterFFmpegBinaries();

            // ffmpeg init & Register
            ffmpeg.avformat_network_init();

            // set log 
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
            av_log_set_callback_callback callback = new av_log_set_callback_callback(FFmpegLogCallback);
        }

        internal void Init()
        {
            Console.WriteLine("==> FFmpeg init");
            Register();

            // set optimize flag
            AVDictionary* options = null;
            ffmpeg.av_dict_set(&options, "rtsp_transport", "tcp", 0);
            ffmpeg.av_dict_set(&options, "stimeout", "500000", 0);          // max timeout 1 seconds
            ffmpeg.av_dict_set(&options, "fflags", "nobuffer", 0);          // no buffer
            ffmpeg.av_dict_set(&options, "fflags", "discardcorrupt", 0);    // discard corrupted frames
            ffmpeg.av_dict_set(&options, "flags", "low_delay", 0);          // no delay
            ffmpeg.av_dict_set(&options, "threads", "auto", 0);

            pFc = ffmpeg.avformat_alloc_context();
            AVFormatContext* pFcPtr = pFc;

            // open rtsp
            errCode = ffmpeg.avformat_open_input(&pFcPtr, RtspUrl, null, &options);
            if (errCode < 0)
            {
                Console.WriteLine("==> Error: " + errCode);
                return;
            }

            // find stream info
            errCode = ffmpeg.avformat_find_stream_info(pFc, null);
            if (errCode < 0)
            {
                Console.WriteLine("==> Error: " + errCode);
                return;
            }
        }

        internal void GetEncode()
        {
            // find video stream
            //pStream = null;

            for (int i = 0; i < pFc->nb_streams; i++)
            {
                if (pFc->streams[i]->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    videoStreamIndex = i;
                    pStream = pFc->streams[i];
                    break;
                }
            }

            if (pStream == null)
            {
                Console.WriteLine("==> Could not found video stream!!");
                return;
            }

            // get context
            pCodecParams = *pStream->codecpar;

            // get pixel format, width, height
            width = pCodecParams.width;
            height = pCodecParams.height;
            srcPixfmt = (AVPixelFormat)pCodecParams.format;
            dstPixfmt = AVPixelFormat.AV_PIX_FMT_RGB0;

            // force to YUV420P
            if (srcPixfmt != AVPixelFormat.AV_PIX_FMT_YUV420P)
            {
                srcPixfmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
            }

            // find codec_id
            codec_id = pCodecParams.codec_id;

            // declare convert context
            pConvertContext = ffmpeg.sws_getContext((int)width, (int)height, srcPixfmt,
                                                        (int)width, (int)height, dstPixfmt,
                                                        ffmpeg.SWS_FAST_BILINEAR, null, null, null);

            if (pConvertContext == null)
            {
                Console.WriteLine("==> Could not initialize the conversion context!!!");
                return;
            }

            // calculate dest buffer size
            int convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(dstPixfmt, (int)width, (int)height, 1);

            // allocate frame Pointer
            convertedFrameBufferPtr = Marshal.AllocHGlobal(convertedFrameBufferSize);
            dstData = new byte_ptrArray4();
            dstLinesize = new int_array4();

            // set graphic fill
            ffmpeg.av_image_fill_arrays(ref dstData, ref dstLinesize, (byte*)convertedFrameBufferPtr.ToPointer(),
                                        dstPixfmt, (int)width, (int)height, 1);
        }

        internal void Decode()
        {
            AVFormatContext * pFcPtr = pFc;
            AVCodecParameters _pCodecParams = pCodecParams;

            // find decoder using codec id
            AVCodec* pCodec = ffmpeg.avcodec_find_decoder(codec_id);
            if (pCodec == null)
            {
                Console.WriteLine("==> Codec not found!!");
                return;
            }

            codecName = UnsafeUtilities.PtrToStringUTF8(pCodec->name);
            framerate = pStream->r_frame_rate.num / (float)pStream->r_frame_rate.den;

            // allocate codec context
            AVCodecContext* pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);

            errCode = ffmpeg.avcodec_parameters_to_context(pCodecContext, &_pCodecParams);
            if (errCode < 0)
            {
                Console.WriteLine("==> Could not allocate codec context!!");
                return;
            }

            // ffmpeg.av_opt_set(pCodecContext->priv_data, "preset", "superfast", 0);

            if ((pCodec->capabilities & ffmpeg.AV_CODEC_CAP_TRUNCATED) == ffmpeg.AV_CODEC_CAP_TRUNCATED)
                pCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_TRUNCATED;

            // open codec
            errCode = ffmpeg.avcodec_open2(pCodecContext, pCodec, null);
            if (errCode < 0)
            {
                Console.WriteLine("==> Could not open codec!!");
                return;
            }

            AVPacket* pPacket = ffmpeg.av_packet_alloc();   // allocate packet
            AVFrame* pFrame = ffmpeg.av_frame_alloc();      // allocate frame

            do
            {
                ffmpeg.av_frame_unref(pFrame);
                ffmpeg.av_packet_unref(pPacket);

                if (ffmpeg.av_read_frame(pFcPtr, pPacket) != ffmpeg.AVERROR_EOF)
                {
                    if (pPacket->stream_index == videoStreamIndex)
                    {
                        if (ffmpeg.avcodec_send_packet(pCodecContext, pPacket) == 0)
                        {
                            // 如果Packet有破損，則丟棄
                            if (pPacket->flags == ffmpeg.AV_PKT_FLAG_CORRUPT)
                                continue;

                            if (ffmpeg.avcodec_receive_frame(pCodecContext, pFrame) == 0)
                            {

                                // convert frame YUV->RGB
                                ffmpeg.sws_scale(pConvertContext, pFrame->data, pFrame->linesize, 0,
                                                    pCodecContext->height, dstData, dstLinesize);

                                // callback
                                if (Callback != null)
                                    Callback((int)width, (int)height, convertedFrameBufferPtr, dstLinesize[0]);

                                ffmpeg.av_packet_unref(pPacket);
                            }
                        }
                    }
                }
            } while (CanRun);

            ffmpeg.av_frame_free(&pFrame);
            ffmpeg.av_packet_free(&pPacket);

            Release();
        }

        void Release()
        {
            AVFormatContext* pFcPtr = pFc;
            AVCodecContext* pCodecContextPtr = pCodecContext;

            Marshal.FreeHGlobal(convertedFrameBufferPtr);

            ffmpeg.sws_freeContext(pConvertContext);
            ffmpeg.avcodec_free_context(&pCodecContextPtr);
            ffmpeg.avformat_close_input(&pFcPtr);

            width = 0;
            height = 0;
            framerate = 0;
            codecName = "";
        }

        internal void Stop()
        {
            CanRun = false;

        }
    }
}
