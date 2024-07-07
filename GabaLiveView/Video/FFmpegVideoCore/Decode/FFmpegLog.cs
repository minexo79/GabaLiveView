using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GabaLiveView.Video;

namespace GabaLiveView.Video.FFmpegVideoCore.Decode
{

    internal unsafe partial class FFmpegHelp : IDisposable
    {
        // TODO: Prevent ExecutionEngineException
        void FFmpegLogCallback(void* ptr, int level, string fmt, byte* vl)
        {
            if (level > ffmpeg.av_log_get_level())
                return;

            var printPrefix = 1;
            var printLength = 1024;
            var printBuffer = stackalloc byte[printLength];
            ffmpeg.av_log_format_line(ptr, level, fmt, vl, printBuffer, printLength, &printPrefix);
            var message = Marshal.PtrToStringAnsi((IntPtr)printBuffer) ?? "";

            LogArgs logArgs = new LogArgs()
            {
                dateTime = DateTime.Now,
                logMessage = message
            };

            Console.Write(DateTime.Now + " " + message);

            if (OnLogReceived != null)
                OnLogReceived?.Invoke(this, logArgs);
        }
    }
}