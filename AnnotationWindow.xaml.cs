using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScreenShot
{
    public partial class AnnotationWindow : Window
    {
        private readonly BitmapSource _screenshot;
        private Point _startPoint;
        private bool _isDrawing;
        private string _currentTool = "Rectangle";
        private UIElement _currentElement;
        private readonly List<UIElement> _drawnElements = new List<UIElement>();
        private readonly Stack<UIElement> _undoStack = new Stack<UIElement>();
        private Polyline _currentPolyline;

        private static readonly Brush RedBrush = new SolidColorBrush(Colors.Red);
        private static readonly Pen RedPen = new Pen(Brushes.Red, 2);

        public AnnotationWindow(BitmapSource screenshot)
        {
            InitializeComponent();
            _screenshot = screenshot;
            ScreenshotImage.Source = screenshot;
            Title = $"截图标注 - {screenshot.PixelWidth} x {screenshot.PixelHeight}";

            RedPen.Freeze();

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };
        }

        private void Tool_Checked(object sender, RoutedEventArgs e)
        {
            if (ToolRectangle != null && ToolRectangle.IsChecked == true) _currentTool = "Rectangle";
            else if (ToolArrow != null && ToolArrow.IsChecked == true) _currentTool = "Arrow";
            else if (ToolLine != null && ToolLine.IsChecked == true) _currentTool = "Line";
            else if (ToolPen != null && ToolPen.IsChecked == true) _currentTool = "Pen";
            else if (ToolText != null && ToolText.IsChecked == true) _currentTool = "Text";
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentTool == "Text")
            {
                var pos = e.GetPosition(DrawingCanvas);
                ShowTextBox(pos);
                return;
            }

            _startPoint = e.GetPosition(DrawingCanvas);
            _isDrawing = true;
            DrawingCanvas.CaptureMouse();

            switch (_currentTool)
            {
                case "Rectangle":
                    var rect = new Rectangle
                    {
                        Stroke = RedBrush,
                        StrokeThickness = 2,
                        StrokeDashArray = null
                    };
                    Canvas.SetLeft(rect, _startPoint.X);
                    Canvas.SetTop(rect, _startPoint.Y);
                    DrawingCanvas.Children.Add(rect);
                    _currentElement = rect;
                    break;

                case "Line":
                    var line = new Line
                    {
                        X1 = _startPoint.X,
                        Y1 = _startPoint.Y,
                        X2 = _startPoint.X,
                        Y2 = _startPoint.Y,
                        Stroke = RedBrush,
                        StrokeThickness = 2
                    };
                    DrawingCanvas.Children.Add(line);
                    _currentElement = line;
                    break;

                case "Arrow":
                    var arrowPath = new System.Windows.Shapes.Path
                    {
                        Stroke = RedBrush,
                        StrokeThickness = 2
                    };
                    DrawingCanvas.Children.Add(arrowPath);
                    _currentElement = arrowPath;
                    break;

                case "Pen":
                    _currentPolyline = new Polyline
                    {
                        Stroke = RedBrush,
                        StrokeThickness = 2,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    _currentPolyline.Points.Add(_startPoint);
                    DrawingCanvas.Children.Add(_currentPolyline);
                    break;
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            var current = e.GetPosition(DrawingCanvas);

            switch (_currentTool)
            {
                case "Rectangle":
                    if (_currentElement is Rectangle rect)
                    {
                        var x = Math.Min(_startPoint.X, current.X);
                        var y = Math.Min(_startPoint.Y, current.Y);
                        var w = Math.Abs(current.X - _startPoint.X);
                        var h = Math.Abs(current.Y - _startPoint.Y);
                        Canvas.SetLeft(rect, x);
                        Canvas.SetTop(rect, y);
                        rect.Width = w;
                        rect.Height = h;
                    }
                    break;

                case "Line":
                    if (_currentElement is Line line)
                    {
                        line.X2 = current.X;
                        line.Y2 = current.Y;
                    }
                    break;

                case "Arrow":
                    if (_currentElement is System.Windows.Shapes.Path arrowPath)
                    {
                        arrowPath.Data = CreateArrowGeometry(_startPoint, current);
                    }
                    break;

                case "Pen":
                    _currentPolyline?.Points.Add(current);
                    break;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;
            _isDrawing = false;
            DrawingCanvas.ReleaseMouseCapture();

            if (_currentElement != null)
            {
                _drawnElements.Add(_currentElement);
                _undoStack.Push(_currentElement);
                _currentElement = null;
            }
            _currentPolyline = null;
        }

        private void ShowTextBox(Point position)
        {
            var tb = new TextBox
            {
                Width = 200,
                Height = 30,
                FontSize = 16,
                Foreground = Brushes.Red,
                Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(1),
                AcceptsReturn = false
            };

            Canvas.SetLeft(tb, position.X);
            Canvas.SetTop(tb, position.Y);
            DrawingCanvas.Children.Add(tb);
            tb.Focus();

            Action finishText = () =>
            {
                if (!string.IsNullOrWhiteSpace(tb.Text))
                {
                    var textBlock = new TextBlock
                    {
                        Text = tb.Text,
                        Foreground = RedBrush,
                        FontSize = 16,
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(textBlock, position.X);
                    Canvas.SetTop(textBlock, position.Y);
                    DrawingCanvas.Children.Add(textBlock);
                    _drawnElements.Add(textBlock);
                    _undoStack.Push(textBlock);
                }
                DrawingCanvas.Children.Remove(tb);
            };

            tb.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    finishText();
            };
            tb.LostFocus += (s, e) => finishText();
        }

        private static StreamGeometry CreateArrowGeometry(Point from, Point to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1)
                return new StreamGeometry();

            var unitX = dx / length;
            var unitY = dy / length;

            var arrowLen = Math.Min(16.0, length * 0.3);
            var arrowWidth = arrowLen * 0.5;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                // 箭身（直线）
                ctx.BeginFigure(from, true, false);
                ctx.LineTo(to, true, false);

                // 箭头（三角形填充）
                var p1 = new Point(to.X - arrowLen * unitX + arrowWidth * unitY,
                                   to.Y - arrowLen * unitY - arrowWidth * unitX);
                var p2 = new Point(to.X - arrowLen * unitX - arrowWidth * unitY,
                                   to.Y - arrowLen * unitY + arrowWidth * unitX);

                ctx.BeginFigure(to, true, true);
                ctx.LineTo(p1, true, false);
                ctx.LineTo(p2, true, false);
            }
            geometry.Freeze();
            return geometry;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                var last = _undoStack.Pop();
                DrawingCanvas.Children.Remove(last);
                _drawnElements.Remove(last);
            }
        }

        private BitmapSource RenderToImage()
        {
            var width = _screenshot.PixelWidth;
            var height = _screenshot.PixelHeight;

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                ctx.DrawImage(_screenshot, new Rect(0, 0, width, height));

                foreach (var el in _drawnElements)
                {
                    var left = Canvas.GetLeft(el);
                    var top = Canvas.GetTop(el);
                    var offset = new Vector(left, top);

                    if (el is Rectangle rect)
                    {
                        var geo = new RectangleGeometry(new Rect(0, 0, rect.Width, rect.Height));
                        ctx.DrawGeometry(null, new Pen(RedBrush, 2), geo);
                    }
                    else if (el is Line line)
                    {
                        ctx.DrawLine(new Pen(RedBrush, 2),
                            new Point(line.X1, line.Y1), new Point(line.X2, line.Y2));
                    }
                    else if (el is System.Windows.Shapes.Path path && path.Data != null)
                    {
                        ctx.DrawGeometry(null, new Pen(RedBrush, 2), path.Data);
                    }
                    else if (el is Polyline polyline)
                    {
                        if (polyline.Points.Count < 2) continue;
                        var figure = new PathFigure { StartPoint = polyline.Points[0], IsFilled = false };
                        for (int i = 1; i < polyline.Points.Count; i++)
                            figure.Segments.Add(new LineSegment(polyline.Points[i], true));
                        var geom = new PathGeometry();
                        geom.Figures.Add(figure);
                        var pen = new Pen(RedBrush, 2);
                        ctx.DrawGeometry(null, pen, geom);
                    }
                    else if (el is TextBlock tb)
                    {
                        var ft = new FormattedText(
                            tb.Text,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                            tb.FontSize,
                            tb.Foreground,
                            VisualTreeHelper.GetDpi(dv).PixelsPerDip);
                        ctx.DrawText(ft, new Point(left, top));
                    }
                }
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }

        private static void SetClipboardWithRetry(BitmapSource image)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetImage(image);
                    return;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG 图片|*.png|JPEG 图片|*.jpg|位图|*.bmp",
                DefaultExt = ".png",
                FileName = $"截图_{DateTime.Now:yyyyMMdd_HHmmss}.png"
            };

            if (dlg.ShowDialog() == true)
            {
                var rendered = RenderToImage();
                using (var stream = new FileStream(dlg.FileName, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rendered));
                    encoder.Save(stream);
                }
                SetClipboardWithRetry(rendered);
                DialogResult = true;
                Close();
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var rendered = RenderToImage();
            SetClipboardWithRetry(rendered);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
