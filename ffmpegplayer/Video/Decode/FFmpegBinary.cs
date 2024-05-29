using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace ffmpegplayer.Video.Decode
{
    internal class FFmpegBinary
    {
        internal static void RegisterFFmpegBinaries()
        {
            // get program current directory
            string currentDir = System.IO.Directory.GetCurrentDirectory();

            // get ffmpeg path
            string ffmpegPath = System.IO.Path.Combine(currentDir, "FFmpeg");

            // check if ffmpeg path exists
            if (!System.IO.Directory.Exists(ffmpegPath))
            {
                throw new System.IO.DirectoryNotFoundException("FFmpeg directory not found");
            }

            // register
            ffmpeg.RootPath = ffmpegPath;
        }
    }
}
