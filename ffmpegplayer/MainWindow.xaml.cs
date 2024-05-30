using ffmpegplayer.Video;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ffmpegplayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VideoCore core;
        VideoReceiveArgs receiveArgs;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() => btnOpen.IsEnabled = false);

            core = new VideoCore(rtspInput.Text);
            core.OnVideoReceived += Core_OnVideoReceived;
            core.Start();
        }

        private void Core_OnVideoReceived(object? sender, VideoReceiveArgs e)
        {
            receiveArgs = e;

            canvas.Dispatcher.Invoke(() => canvas.InvalidateVisual());
        }

        private void canvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (receiveArgs != null && receiveArgs.videoBmp != null)
            {
                e.Surface.Canvas.DrawBitmap(receiveArgs.videoBmp, e.Info.Rect);
            }    
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            core.OnVideoReceived -= Core_OnVideoReceived;
            core.Stop();

            receiveArgs = null;                                         // 重置接收參數
            canvas.Dispatcher.Invoke(() => canvas.InvalidateVisual());  // 重繪畫布

            this.Dispatcher.Invoke(() => btnOpen.IsEnabled = true);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ButtonStop_Click(this, null);
        }
    }
}