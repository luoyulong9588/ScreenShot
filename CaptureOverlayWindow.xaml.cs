using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;

namespace ScreenShot
{
    public partial class CaptureOverlayWindow : Window
    {
        private readonly BitmapSource _screenImage;
        private Matrix _dipToPixel = Matrix.Identity;
        private Image _backgroundImage;
        private Rectangle _selectionRect;
        private Rectangle _dimOverlay;
        private Point _startPoint;
        private bool _isDragging;

        public Int32Rect? SelectedRegion { get; private set; }

        public CaptureOverlayWindow(BitmapSource screenImage)
        {
            InitializeComponent();
            _screenImage = screenImage;

            var bounds = WinForms.Screen.PrimaryScreen.Bounds;
            Left = bounds.X;
            Top = bounds.Y;
            Width = bounds.Width;
            Height = bounds.Height;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var ps = PresentationSource.FromVisual(this);
            if (ps != null && ps.CompositionTarget != null)
                _dipToPixel = ps.CompositionTarget.TransformToDevice;

            OverlayCanvas.Width = ActualWidth;
            OverlayCanvas.Height = ActualHeight;

            _backgroundImage = new Image
            {
                Source = _screenImage,
                Width = ActualWidth,
                Height = ActualHeight
            };
            Canvas.SetLeft(_backgroundImage, 0);
            Canvas.SetTop(_backgroundImage, 0);
            OverlayCanvas.Children.Add(_backgroundImage);

            _dimOverlay = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                Width = ActualWidth,
                Height = ActualHeight
            };
            Canvas.SetLeft(_dimOverlay, 0);
            Canvas.SetTop(_dimOverlay, 0);
            OverlayCanvas.Children.Add(_dimOverlay);

            _selectionRect = new Rectangle
            {
                Stroke = Brushes.White,
                StrokeThickness = 1.5,
                Fill = Brushes.Transparent
            };
            OverlayCanvas.Children.Add(_selectionRect);

            OverlayCanvas.MouseLeftButtonDown += OnMouseLeftButtonDown;
            OverlayCanvas.MouseMove += OnMouseMove;
            OverlayCanvas.MouseLeftButtonUp += OnMouseLeftButtonUp;
            KeyDown += OnKeyDown;
        }

        private Point DipToPixel(Point dip)
        {
            return _dipToPixel.Transform(dip);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(OverlayCanvas);
            _isDragging = true;
            OverlayCanvas.CaptureMouse();

            Canvas.SetLeft(_selectionRect, _startPoint.X);
            Canvas.SetTop(_selectionRect, _startPoint.Y);
            _selectionRect.Width = 0;
            _selectionRect.Height = 0;

            SizeLabel.Visibility = Visibility.Visible;
            ActionBar.Visibility = Visibility.Collapsed;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var current = e.GetPosition(OverlayCanvas);
            var x = Math.Min(_startPoint.X, current.X);
            var y = Math.Min(_startPoint.Y, current.Y);
            var w = Math.Abs(current.X - _startPoint.X);
            var h = Math.Abs(current.Y - _startPoint.Y);

            Canvas.SetLeft(_selectionRect, x);
            Canvas.SetTop(_selectionRect, y);
            _selectionRect.Width = w;
            _selectionRect.Height = h;

            var tl = DipToPixel(new Point(x, y));
            var br = DipToPixel(new Point(x + w, y + h));
            var physW = (int)(br.X - tl.X);
            var physH = (int)(br.Y - tl.Y);
            SizeLabelText.Text = $"{physW} x {physH}";

            Canvas.SetLeft(SizeLabel, x + w + 8);
            Canvas.SetTop(SizeLabel, y + h + 8);

            if (x + w + 8 + 80 > ActualWidth)
                Canvas.SetLeft(SizeLabel, x - 8 - 80);
            if (y + h + 8 + 30 > ActualHeight)
                Canvas.SetTop(SizeLabel, y - 8 - 24);
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            OverlayCanvas.ReleaseMouseCapture();

            var current = e.GetPosition(OverlayCanvas);
            var dipX = Math.Min(_startPoint.X, current.X);
            var dipY = Math.Min(_startPoint.Y, current.Y);
            var dipW = Math.Abs(current.X - _startPoint.X);
            var dipH = Math.Abs(current.Y - _startPoint.Y);

            if (dipW < 1 || dipH < 1)
            {
                SizeLabel.Visibility = Visibility.Collapsed;
                DialogResult = false;
                Close();
                return;
            }

            var tl = DipToPixel(new Point(dipX, dipY));
            var br = DipToPixel(new Point(dipX + dipW, dipY + dipH));

            var x = Math.Max(0, (int)tl.X);
            var y = Math.Max(0, (int)tl.Y);
            var w = (int)(br.X - tl.X);
            var h = (int)(br.Y - tl.Y);

            w = Math.Min(w, _screenImage.PixelWidth - x);
            h = Math.Min(h, _screenImage.PixelHeight - y);

            SelectedRegion = new Int32Rect(x, y, w, h);

            SizeLabel.Visibility = Visibility.Collapsed;

            var actionX = dipX + dipW - ActionBar.ActualWidth;
            var actionY = dipY + dipH + 8;
            if (actionY + 50 > ActualHeight)
                actionY = dipY - 44;
            if (actionX < 0) actionX = dipX;

            Canvas.SetLeft(ActionBar, actionX);
            Canvas.SetTop(ActionBar, actionY);
            ActionBar.Visibility = Visibility.Visible;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            else if (e.Key == Key.Return && SelectedRegion.HasValue)
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedRegion = null;
            DialogResult = false;
            Close();
        }
    }
}
