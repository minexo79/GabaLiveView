using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Timer = System.Timers.Timer;
using System.Threading.Tasks;
using System.IO;

namespace GabaLiveView.Video.FFmpegVideoCore.Utilities
{
    internal class NetworkUsageUtilities
    {
        private Process process { get; set; }
        private int process_id { get; set; } = 0;
        private string instanceName { get; set; } = String.Empty;

        PerformanceCounterCategory performanceCounterCategory = new PerformanceCounterCategory("Process");
        PerformanceCounter bytesSentCounter, bytesReceivedCounter;


        private Timer networkUsageTimer;
        public float ReceivedBytes { get; set; } = 0;
        public float SentBytes { get; set; } = 0;

        public NetworkUsageUtilities()
        {
            process = Process.GetCurrentProcess();

            process_id = process.Id;
            instanceName = process.ProcessName ?? String.Empty;
            

            if (instanceName == String.Empty)
            {
                throw new Exception("No instance found for current process");
            }

            bytesReceivedCounter = new PerformanceCounter("Process", "IO Read Bytes/sec", instanceName);
            bytesSentCounter    = new PerformanceCounter("Process", "IO Write Bytes/sec", instanceName);

            networkUsageTimer = new Timer();
            networkUsageTimer.Interval = 1000;
            networkUsageTimer.Elapsed += NetworkUsageTimer_Elapsed;
        }

        public void Start()
        {
            networkUsageTimer.Start();
        }

        public void Stop()
        {
            networkUsageTimer.Stop();
        }

        private void NetworkUsageTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            ReceivedBytes   = GetReceivedBytes();
            SentBytes       = GetSentBytes();
        }

        private float GetReceivedBytes()
        {
            if (instanceName != String.Empty)
            {
                return bytesReceivedCounter.NextValue();
            }

            return 0;
        }

        private float GetSentBytes()
        {
            if (instanceName != String.Empty)
            {
                return bytesSentCounter.NextValue();
            }

            return 0;
        }
    }
}
