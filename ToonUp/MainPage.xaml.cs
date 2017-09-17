using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Windows.Threading;

namespace ToonUp
{
    public sealed class VideoCapture : VideoSink, IDisposable
    {
        private VideoCaptureDevice device;
        private CaptureSource source;
        private VideoFormat format;
        private WriteableBitmap bitmap;
        private Canvas canvas;
        private Dispatcher dispatcher;

        public VideoCapture(Dispatcher dispatcher, Canvas canvas)
        {
            if (null == dispatcher)
            {
                throw new ArgumentNullException("dispatcher");
            }

            this.canvas = canvas;
            this.dispatcher = dispatcher;
        }

        public DownSampler DownSampler
        {
            get;
            private set;
        }

        public void Initialize()
        {
            if (null == (device = CaptureDeviceConfiguration.GetDefaultVideoCaptureDevice()))
            {
                // TODO: put some UI up for this later - fuck that shit for now
                throw new NotSupportedException("No video capture device available");
            }

            this.source = new CaptureSource();

            this.AllocationMode = SampleAllocationMode.ReuseBuffer;
            this.CaptureSource = this.source;

            this.source.Start();
        }

        public void Dispose()
        {
        }

        #region VideoSink Implementation

        protected override void OnCaptureStarted()
        {
        }

        protected override void OnCaptureStopped()
        {
        }

        protected override void OnFormatChange(VideoFormat videoFormat)
        {
            this.format = videoFormat;
            this.dispatcher.BeginInvoke(() => this.bitmap = new WriteableBitmap(this.format.PixelWidth, this.format.PixelHeight));
            this.DownSampler = new DownSampler(videoFormat, 16);

            if (null != this.canvas)
            {
                this.dispatcher.BeginInvoke(
                    () =>
                    this.canvas.Background = new ImageBrush()
                    {
                        ImageSource = this.bitmap
                    }
                );
            }
        }

        protected override void OnSample(long sampleTimeInHundredNanoseconds, long frameDurationInHundredNanoseconds, byte[] sampleData)
        {
            Canny.EdgeDetection(sampleData, sampleData, this.format.PixelWidth, this.format.PixelHeight, this.format.Stride, 32, Colors.Red, DownSampler);

            this.dispatcher.BeginInvoke(
                () =>
                {
                    Buffer.BlockCopy(sampleData, 0, this.bitmap.Pixels, 0, sampleData.Length);
                    this.bitmap.Invalidate();
                }
            );
        }

        #endregion
    }

    public partial class MainPage : PhoneApplicationPage
    {
        private VideoCapture capture;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (null == this.capture)
            {
                this.capture = new VideoCapture(this.Dispatcher, this.videoCanvas);
                this.capture.Initialize();
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            this.capture.Dispose();
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            base.OnOrientationChanged(e);
        }

        // Simple button Click event handler to take us to the second page
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/GamePage.xaml", UriKind.Relative));
        }

        private void CanvasDoubleTap(object sender, GestureEventArgs e)
        {
            string title;

            this.capture.DownSampler.RotateSampler();

            title = string.Format("Toon Up - {0} {1}", this.capture.DownSampler.SamplerIndex, this.capture.DownSampler.SamplerName);

            this.Dispatcher.BeginInvoke(() => this.ApplicationTitle.Text = title);
        }
    }
}