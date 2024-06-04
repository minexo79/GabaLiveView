using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ffmpegplayer.Video.Decode
{
    public class LogArgs : EventArgs
    {
        public DateTime dateTime { get; set; }
        public string logMessage { get; set; } = String.Empty;
    }
}
