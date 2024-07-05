﻿using GabaLiveView.Video;
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

namespace GabaLiveView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel vm;

        public MainWindow()
        {
            InitializeComponent();

            vm = new MainWindowViewModel(ref canvas);

            this.DataContext = vm;
            videoInfoControl.DataContext = vm;
        }

        private void canvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (vm.ReceiveArgs != null && vm.ReceiveArgs.videoBmp != null)
            {
                e.Surface.Canvas.DrawBitmap(vm.ReceiveArgs.videoBmp, e.Info.Rect);
            }
            else
            {
                e.Surface.Canvas.Clear();
            }
        }


    }
}