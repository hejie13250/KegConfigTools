using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace 小科狗配置.Contorl
{
  // 定义事件参数类
  public class ColorChangedEventArgs : EventArgs
  {
    /// <inheritdoc />
    public ColorChangedEventArgs()
    {
    }
  }


  public partial class ColorPicker
  {
    private WriteableBitmap Bitmap { get; set; }
    public SolidColorBrush RgbColor { get; private set; }
    public event EventHandler<ColorChangedEventArgs> ColorChanged;

    protected virtual void OnColorChanged(SolidColorBrush oldColor, SolidColorBrush newColor)
    {
      ColorChanged?.Invoke(this, new ColorChangedEventArgs());
    }
    public string RgbText { get; private set; }
    //public event EventHandler<string> RGBTextChanged;

    //protected virtual void OnRGBTextChanged(string oldRGBText, string newRGBText)
    //{
    //  RGBTextChanged?.Invoke(this, newRGBText);
    //}

    private readonly int _width;
    private readonly int _height;

    public ColorPicker()
    {
      InitializeComponent();
      _width  = (int)canvas.Width;
      _height = (int)canvas.Height;

      //Bitmap = new WriteableBitmap(170, 170, 170, 170, PixelFormats.Bgra32, null);
      Bitmap = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgra32, null);
      imageBrush.ImageSource = Bitmap;
      UpdateBitmap();
    }



    // 颜色转换 HSVToRGB
    private static void HsvToRgb(double h, double s, double v, out byte r, out byte g, out byte b)
    {
      if (s == 0)
        r = g = b = (byte)(v * 255);
      else
      {
        var hue = h * 6.0;
        var i = (int)Math.Floor(hue);
        var f = hue - i;
        var p = v * (1.0 - s);
        var q = v * (1.0 - (s * f));
        var t = v * (1.0 - (s * (1.0 - f)));
        switch (i)
        {
          case 0:
            r = (byte)(v * 255);
            g = (byte)(t * 255);
            b = (byte)(p * 255);
            break;
          case 1:
            r = (byte)(q * 255);
            g = (byte)(v * 255);
            b = (byte)(p * 255);
            break;
          case 2:
            r = (byte)(p * 255);
            g = (byte)(v * 255);
            b = (byte)(t * 255);
            break;
          case 3:
            r = (byte)(p * 255);
            g = (byte)(q * 255);
            b = (byte)(v * 255);
            break;
          case 4:
            r = (byte)(t * 255);
            g = (byte)(p * 255);
            b = (byte)(v * 255);
            break;
          default:
            r = (byte)(v * 255);
            g = (byte)(p * 255);
            b = (byte)(q * 255);
            break;
        }
      }
    }



    // 取色器更新取色位图
    private void UpdateBitmap()
    {
      var hue = hue_Slider.Value / 360; // Hue 值现在来自滑动条
      Bitmap.Lock();
      var backBuffer = Bitmap.BackBuffer;
      var stride = Bitmap.BackBufferStride;

      for (var y = 0; y < _height; y++)
      {
        for (var x = 0; x < _width; x++)
        {
          var normalizedX = (double)x / (_width - 1);
          var normalizedY = (double)y / (_height - 1);

          // 传递给HSVToRGB函数的Hue值现在是0-360度的范围
          HsvToRgb(hue, normalizedX, 1 - normalizedY, out var r, out var g, out var b);

          var pixelOffset = y * stride + x * 4;
          Marshal.WriteByte(backBuffer, pixelOffset + 0, b);
          Marshal.WriteByte(backBuffer, pixelOffset + 1, g);
          Marshal.WriteByte(backBuffer, pixelOffset + 2, r);
          Marshal.WriteByte(backBuffer, pixelOffset + 3, 255); // Alpha 通道设为最大值255（不透明）
        }
      }

      Bitmap.AddDirtyRect(new Int32Rect(0, 0, 170, 170));
      Bitmap.Unlock();

      if (Bitmap != null)
      {
        // 强制 UI 刷新 ImageBrush
        canvas.InvalidateVisual();
      }
    }

    // 画布 canvas 点击取色
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var canvasPosition = e.GetPosition(canvas);
      var newLeft = canvasPosition.X - thumb.ActualWidth / 2;
      var newTop = canvasPosition.Y - thumb.ActualHeight / 2;

      SetThumbPosition(newLeft, newTop);
    }


    private       DateTime _lastUpdate     = DateTime.Now;
    private const int      UpdateInterval = 10; // 更新间隔（毫秒）

    // 画布 canvas 的 thumb 移动取色
    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
      if ((DateTime.Now - _lastUpdate).TotalMilliseconds < UpdateInterval)
        return; // 如果距离上次更新时间小于间隔，则不进行更新

      var newLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;
      var newTop  = Canvas.GetTop(thumb)  + e.VerticalChange;

      SetThumbPosition(newLeft, newTop);
      _lastUpdate = DateTime.Now; // 更新最后更新时间
    }

    private void SetThumbPosition(double newLeft, double newTop)
    {

      // 计算画布边界
      var canvasRight = canvas.ActualWidth - thumb.ActualWidth + 5;
      var canvasBottom = canvas.ActualHeight - thumb.ActualHeight + 5;

      // 限制 Thumb 在画布内部移动
      newLeft = Math.Max(-6, Math.Min(newLeft, canvasRight));
      newTop = Math.Max(-6, Math.Min(newTop, canvasBottom));

      Canvas.SetLeft(thumb, newLeft);
      Canvas.SetTop(thumb, newTop);
      GetAreaColor();
    }

    // 获取当前选中颜色
    void GetAreaColor()
    {
      Point? thumbPosition = thumb.TranslatePoint(new Point(thumb.ActualWidth / 2, thumb.ActualHeight / 2), canvas);

      if (!(thumbPosition.Value.X >= 0) || !(thumbPosition.Value.X < Bitmap.PixelWidth) ||
          !(thumbPosition.Value.Y >= 0) || !(thumbPosition.Value.Y < Bitmap.PixelHeight)) return;
      var xCoordinate = (int)thumbPosition.Value.X;
      var yCoordinate = (int)thumbPosition.Value.Y;

      var stride = Bitmap.PixelWidth * (Bitmap.Format.BitsPerPixel / 8);
      var pixels = new byte[Bitmap.PixelHeight * stride];
      Bitmap.CopyPixels(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight), pixels, stride, 0);

      var pixelIndex = (yCoordinate * stride) + (xCoordinate * (Bitmap.Format.BitsPerPixel / 8));
      var color      = Color.FromArgb(pixels[pixelIndex + 3], pixels[pixelIndex + 2], pixels[pixelIndex + 1], pixels[pixelIndex]);

      // 更新Thumb的BorderBrush，取反色
      thumb.BorderBrush = new SolidColorBrush(Color.FromRgb((byte)(255 - color.R), (byte)(255 - color.G), (byte)(255 - color.B)));

      RgbText = $"({color.R}, {color.G}, {color.B})";
      var newColor = new SolidColorBrush(color);
      RgbColor = newColor;
      OnColorChanged(RgbColor, newColor);
    }




    private void Hue_slider_MouseUp(object sender, MouseButtonEventArgs e)
    {
      if (sender is not Slider slider) return;
      var point = e.GetPosition(slider);
      // 计算点击位置相对于Slider的比例
      var newValue = (point.Y / slider.ActualHeight) * slider.Maximum;
      slider.Value = slider.Maximum - newValue;  // 反转值，因为Slider垂直方向是从上到下
    }

    // Hue_slider 值改变事件
    private void Hue_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      UpdateBitmap();
      GetAreaColor();
    }

    // Hue_slider 滚轮事件
    private void Hue_slider_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var step = 5;
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= 10;

      switch (e.Delta)
      {
        case > 0 when hue_Slider.Value + step <= hue_Slider.Maximum:
          hue_Slider.Value += step;
          break;
        case < 0 when hue_Slider.Value - step >= hue_Slider.Minimum:
          hue_Slider.Value -= step;
          break;
      }
      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }


  }
}
