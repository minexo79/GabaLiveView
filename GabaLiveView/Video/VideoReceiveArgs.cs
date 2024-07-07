using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GabaLiveView.Video
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
}
