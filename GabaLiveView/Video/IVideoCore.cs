using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GabaLiveView.Video
{
    interface IVideoCore
    {
        public event EventHandler<VideoReceiveArgs> OnVideoReceived;
        public event EventHandler<LogArgs> OnLogReceived;
        void Start();
        void Stop();
        void Dispose();
        void changeUrl(string url);
    }
}
