using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WpfApp
{
    public partial class PdfWindow : Window
    {
        private readonly string _pdfPath;
        private Point _lastPoint;
        private bool _isPanning;
        private bool _loaded;
        private bool _isLandscape = false;

        public PdfWindow(string pdfPath)
        {
            InitializeComponent();
            _pdfPath = pdfPath;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await LoadPdfAsync(_pdfPath);
        }

        private async Task LoadPdfAsync(string path)
        {
            try
            {
                HintText.Text = "正在加载图纸…";
                string fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    HintText.Text = $"找不到 PDF 文件：\n{fullPath}";
                    return;
                }

                var file = await StorageFile.GetFileFromPathAsync(fullPath);
                var doc = await PdfDocument.LoadFromFileAsync(file);
                var page = doc.GetPage(0);

                var stream = new InMemoryRandomAccessStream();
                var options = new PdfPageRenderOptions { DestinationWidth = 2000 };
                await page.RenderToStreamAsync(stream, options);

                var bytes = new byte[stream.Size];
                var reader = new DataReader(stream.GetInputStreamAt(0));
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(bytes);
                reader.Dispose();
                stream.Dispose();

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = new MemoryStream(bytes);
                bmp.EndInit();
                bmp.Freeze();

                PageImage.Source = bmp;
                _loaded = true;
                HintText.Text = "";
                ResetView();
            }
            catch (Exception ex)
            {
                HintText.Text = "加载失败：" + ex.Message;
            }
        }

        private void ToggleOrientationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLandscape)
            {
                Width = 900; Height = 700;
                ViewHost.Width = 842; ViewHost.Height = 595;
                _isLandscape = true;
            }
            else
            {
                Width = 700; Height = 900;
                ViewHost.Width = 595; ViewHost.Height = 842;
                _isLandscape = false;
            }

            // 【核心修改】图纸跟着窗口一起旋转 90 度
            ImageRotate.Angle = (ImageRotate.Angle + 90) % 360;

            // 重置缩放和平移，让转完后的图纸重新自适应铺满新尺寸
            ImageScale.ScaleX = 1;
            ImageScale.ScaleY = 1;
            ImageTranslate.X = 0;
            ImageTranslate.Y = 0;

            // 确保窗口不会超出屏幕边缘
            var workArea = SystemParameters.WorkArea;
            if (Left + Width > workArea.Right) Left = workArea.Right - Width;
            if (Top + Height > workArea.Bottom) Top = workArea.Bottom - Height;
            if (Left < 0) Left = 0;
            if (Top < 0) Top = 0;
        }

        private void ResetView()
        {
            ImageScale.ScaleX = 1; ImageScale.ScaleY = 1;
            ImageRotate.Angle = 0;
            ImageTranslate.X = 0; ImageTranslate.Y = 0;
        }

        private void ViewHost_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_loaded) return;
            double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
            double newScale = ImageScale.ScaleX * factor;

            if (newScale >= 0.05 && newScale <= 50)
            {
                Point p = e.GetPosition(ViewHost);
                double vw = ViewHost.ActualWidth;
                double vh = ViewHost.ActualHeight;
                double offsetX = p.X - vw / 2;
                double offsetY = p.Y - vh / 2;

                ImageTranslate.X -= offsetX * (factor - 1);
                ImageTranslate.Y -= offsetY * (factor - 1);
                ImageScale.ScaleX = newScale;
                ImageScale.ScaleY = newScale;
            }
            e.Handled = true;
        }

        private void ViewHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_loaded) return;
            if (e.ClickCount == 2) { ResetView(); return; }
            _isPanning = true;
            _lastPoint = e.GetPosition(ViewHost);
            ViewHost.CaptureMouse();
            ViewHost.Cursor = Cursors.SizeAll;
        }

        private void ViewHost_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;
            Point p = e.GetPosition(ViewHost);
            ImageTranslate.X += p.X - _lastPoint.X;
            ImageTranslate.Y += p.Y - _lastPoint.Y;
            _lastPoint = p;
        }

        private void ViewHost_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            ViewHost.ReleaseMouseCapture();
            ViewHost.Cursor = Cursors.Arrow;
        }

        private void ViewHost_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_loaded) return;
            ImageRotate.Angle = (ImageRotate.Angle + 90) % 360;
        }
    }
}