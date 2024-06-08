using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;
using FFmpeg.AutoGen;
using DaFenPlayer.Video.Utilities;

namespace DaFenPlayer.Video.Decode
{
    internal unsafe partial class FFmpegHelp : IDisposable
    {
        event EventHandler<VideoReceiveArgs> OnVideoReceived;
        event EventHandler<LogArgs> OnLogReceived;

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

        internal bool isOpen = false;

        // SKBitmap
        SKBitmap videoBmp;
        SKImageInfo imageInfo;

        // last frame time record
        internal DateTime lastFrameDateTime;

        // stream url
        string streamUrl = "";

        // log lock
        object logLock = new object();

        // task
        Task decodeTask;
        CancellationTokenSource cts;

        public FFmpegHelp(string _url, EventHandler<VideoReceiveArgs> onVideoReceived, EventHandler<LogArgs> onLogReceived)
        {
            streamUrl = _url;

            OnVideoReceived = onVideoReceived;
            OnLogReceived = onLogReceived;

            lastFrameDateTime = DateTime.Now;
            cts = new CancellationTokenSource();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                OnVideoReceived = null;
                OnLogReceived   = null;

                videoBmp?.Dispose();
                videoBmp = null;

                cts?.Dispose();
                cts = null;

                logLock = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.Collect();
            GC.SuppressFinalize(this);
        }

        internal void StartFFmpeg()
        {
            Init();
            GetEncode();
            decodeTask = new Task(() => StartDecode(), cts.Token);
            decodeTask.Start();
        }

        void Register()
        {
            // find ffmpeg binaries
            FFmpegBinaryHelp.RegisterFFmpegBinaries();

            // ffmpeg init & Register
            ffmpeg.avformat_network_init();

            // set log 
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);

            // TODO: Prevent ExecutionEngineException
            //av_log_set_callback_callback callback = 
            //    (void* ptr, int level, string fmt, byte* vl) => FFmpegLogCallback(ptr, level, fmt, vl);

            //ffmpeg.av_log_set_callback(callback);
        }

        void Init()
        {
            Console.WriteLine("==> FFmpeg init");
            Register();

            // set optimize flag
            AVDictionary* options = null;
            ffmpeg.av_dict_set(&options, "rtsp_transport", "tcp", 0);
            ffmpeg.av_dict_set(&options, "stimeout", "2000000", 0);         // max timeout 2 seconds
            ffmpeg.av_dict_set(&options, "fflags", "nobuffer", 0);          // no buffer
            ffmpeg.av_dict_set(&options, "fflags", "discardcorrupt", 0);    // discard corrupted frames
            ffmpeg.av_dict_set(&options, "flags", "low_delay", 0);          // no delay
            ffmpeg.av_dict_set(&options, "threads", "auto", 0);

            pFc = ffmpeg.avformat_alloc_context();
            AVFormatContext* pFcPtr = pFc;

            // open rtsp
            errCode = ffmpeg.avformat_open_input(&pFcPtr, streamUrl, null, &options);
            if (errCode < 0)
            {
                Console.WriteLine("==> Error: " + errCode);
                return;
            }

            isOpen = true;


            // find stream info
            errCode = ffmpeg.avformat_find_stream_info(pFc, null);
            if (errCode < 0)
            {
                Console.WriteLine("==> Error: " + errCode);
                return;
            }
        }
        void GetEncode()
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

            // new SKBitmap
            videoBmp = new SKBitmap((int)width, (int)height);
            imageInfo = new SKImageInfo((int)width, (int)height, SKColorType.Bgra8888, SKAlphaType.Premul);


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

        Task StartDecode()
        {
            AVFormatContext * pFcPtr = pFc;
            AVCodecParameters _pCodecParams = pCodecParams;

            // find decoder using codec id
            AVCodec* pCodec = ffmpeg.avcodec_find_decoder(codec_id);
            if (pCodec == null)
            {
                Console.WriteLine("==> Codec not found!!");
                return Task.CompletedTask;
            }

            codecName = UnsafeUtilities.PtrToStringUTF8(pCodec->name);

            // allocate codec context
            AVCodecContext* pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);

            errCode = ffmpeg.avcodec_parameters_to_context(pCodecContext, &_pCodecParams);
            if (errCode < 0)
            {
                Console.WriteLine("==> Could not allocate codec context!!");
                return Task.CompletedTask;
            }

            // ffmpeg.av_opt_set(pCodecContext->priv_data, "preset", "superfast", 0);

            if ((pCodec->capabilities & ffmpeg.AV_CODEC_CAP_TRUNCATED) == ffmpeg.AV_CODEC_CAP_TRUNCATED)
                pCodecContext->flags |= ffmpeg.AV_CODEC_FLAG_TRUNCATED;

            // open codec
            errCode = ffmpeg.avcodec_open2(pCodecContext, pCodec, null);
            if (errCode < 0)
            {
                Console.WriteLine("==> Could not open codec!!");
                return Task.CompletedTask;
            }

            AVPacket* pPacket = ffmpeg.av_packet_alloc();   // allocate packet
            AVFrame* pFrame = ffmpeg.av_frame_alloc();      // allocate frame

            do
            {
                ffmpeg.av_frame_unref(pFrame);
                ffmpeg.av_packet_unref(pPacket);

                //Console.WriteLine("1");
                if (ffmpeg.av_read_frame(pFcPtr, pPacket) != ffmpeg.AVERROR_EOF)
                {
                    // 2024.6.5 Blackcat: Use av_guess_frame_rate instead r_framerate to get the **real** framerate
                    AVRational avFpsRational = ffmpeg.av_guess_frame_rate(pFc, pStream, null);
                    framerate = avFpsRational.num / (float)avFpsRational.den;
                    // 2024.6.5 Blackcat: Rescale the packet timestamp to the stream timebase
                    ffmpeg.av_packet_rescale_ts(pPacket, pStream->time_base, pCodecContext->time_base);
                    //Console.WriteLine("2");
                    Decode(pCodecContext, pPacket, pFrame);
                    //Console.WriteLine("3");
                }
            } 
            while (!cts.Token.IsCancellationRequested);

            ffmpeg.av_frame_free(&pFrame);
            ffmpeg.av_packet_free(&pPacket);

            return Task.CompletedTask;
        }

        void Release()
        {
            Console.WriteLine("==> FFmpeg Stop");

            AVFormatContext* pFcPtr = pFc;
            AVCodecContext* pCodecContextPtr = pCodecContext;

            Marshal.FreeHGlobal(convertedFrameBufferPtr);
            ffmpeg.sws_freeContext(pConvertContext);
            ffmpeg.avcodec_free_context(&pCodecContextPtr);

            if (isOpen)
                ffmpeg.avformat_close_input(&pFcPtr);

            ffmpeg.avformat_free_context(pFcPtr);

            width = 0;
            height = 0;
            framerate = 0;
            codecName = "";
        }

        internal void StopFFmpeg()
        {
            cts.Cancel();
            decodeTask?.Wait(1000);

            Release();
        }
    }
}
