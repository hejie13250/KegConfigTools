using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FormsDialogResult = System.Windows.Forms.DialogResult;
using GroupBox = System.Windows.Controls.GroupBox;
using Label = System.Windows.Controls.Label;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using Thumb = System.Windows.Controls.Primitives.Thumb;
using UserControl = System.Windows.Controls.UserControl;
using Window = System.Windows.Window;
namespace 小科狗配置
{
  // 定义事件参数类
  public class ColorChangedEventArgs : EventArgs
  {
    public SolidColorBrush OldColor { get; }
    public SolidColorBrush NewColor { get; }

    public ColorChangedEventArgs(SolidColorBrush oldColor, SolidColorBrush newColor)
    {
      OldColor = oldColor;
      NewColor = newColor;
    }
  }


  public partial class ColorPicker : UserControl
  {
    private WriteableBitmap Bitmap { get; set; }
    public SolidColorBrush RGBcolor { get; private set; }
    public event EventHandler<ColorChangedEventArgs> ColorChanged;

    protected virtual void OnColorChanged(SolidColorBrush oldColor, SolidColorBrush newColor)
    {
      ColorChanged?.Invoke(this, new ColorChangedEventArgs(oldColor, newColor));
    }
    public string RGBText { get; private set; }
    //public event EventHandler<string> RGBTextChanged;

    //protected virtual void OnRGBTextChanged(string oldRGBText, string newRGBText)
    //{
    //  RGBTextChanged?.Invoke(this, newRGBText);
    //}

    readonly int width;
    readonly int height;

    public ColorPicker()
    {
      InitializeComponent();
      width  = (int)canvas.Width;
      height = (int)canvas.Height;

      //Bitmap = new WriteableBitmap(170, 170, 170, 170, PixelFormats.Bgra32, null);
      Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
      imageBrush.ImageSource = Bitmap;
      UpdateBitmap();
    }



    // 颜色转换 HSVToRGB
    private static void HSVToRGB(double h, double s, double v, out byte r, out byte g, out byte b)
    {
      if (s == 0)
        r = g = b = (byte)(v * 255);
      else
      {
        double hue = h * 6.0;
        int i = (int)Math.Floor(hue);
        double f = hue - i;
        double p = v * (1.0 - s);
        double q = v * (1.0 - (s * f));
        double t = v * (1.0 - (s * (1.0 - f)));
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
      double hue = hue_slider.Value / 360; // Hue 值现在来自滑动条
      Bitmap.Lock();
      IntPtr backBuffer = Bitmap.BackBuffer;
      int stride = Bitmap.BackBufferStride;

      for (int y = 0; y < height; y++)
      {
        for (int x = 0; x < width; x++)
        {
          double normalizedX = (double)x / (width - 1);
          double normalizedY = (double)y / (height - 1);

          // 传递给HSVToRGB函数的Hue值现在是0-360度的范围
          HSVToRGB(hue, normalizedX, 1 - normalizedY, out byte r, out byte g, out byte b);

          int pixelOffset = y * stride + x * 4;
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
      double newLeft = canvasPosition.X - thumb.ActualWidth / 2;
      double newTop = canvasPosition.Y - thumb.ActualHeight / 2;

      SetThumbPosition(newLeft, newTop);
    }

    // 画布 canvas 的 thumb 移动取色
    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
      double newLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;
      double newTop = Canvas.GetTop(thumb) + e.VerticalChange;

      SetThumbPosition(newLeft, newTop);
    }

    private void SetThumbPosition(double newLeft, double newTop)
    {

      // 计算画布边界
      double canvasRight = canvas.ActualWidth - thumb.ActualWidth + 5;
      double canvasBottom = canvas.ActualHeight - thumb.ActualHeight + 5;

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

      if (thumbPosition.HasValue && thumbPosition.Value.X >= 0 && thumbPosition.Value.X < Bitmap.PixelWidth && thumbPosition.Value.Y >= 0 && thumbPosition.Value.Y < Bitmap.PixelHeight)
      {
        int xCoordinate = (int)thumbPosition.Value.X;
        int yCoordinate = (int)thumbPosition.Value.Y;

        int stride = Bitmap.PixelWidth * (Bitmap.Format.BitsPerPixel / 8);
        byte[] pixels = new byte[Bitmap.PixelHeight * stride];
        Bitmap.CopyPixels(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight), pixels, stride, 0);

        int pixelIndex = (yCoordinate * stride) + (xCoordinate * (Bitmap.Format.BitsPerPixel / 8));
        Color color = Color.FromArgb(pixels[pixelIndex + 3], pixels[pixelIndex + 2], pixels[pixelIndex + 1], pixels[pixelIndex]);

        // 更新Thumb的BorderBrush，取反色
        thumb.BorderBrush = new SolidColorBrush(Color.FromRgb((byte)(255 - color.R), (byte)(255 - color.G), (byte)(255 - color.B)));

        RGBText = $"({color.R}, {color.G}, {color.B})";
        var newColor = new SolidColorBrush(color);
        RGBcolor = newColor;
        OnColorChanged(RGBcolor, newColor);
      }
    }




    private void Hue_slider_MouseUp(object sender, MouseButtonEventArgs e)
    {
      if (sender is Slider slider)
      {
        var point = e.GetPosition(slider);
        // 计算点击位置相对于Slider的比例
        double newValue = (point.Y / slider.ActualHeight) * slider.Maximum;
        slider.Value = slider.Maximum - newValue;  // 反转值，因为Slider垂直方向是从上到下
      }
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
      int step = 5;
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= -10;

      if (e.Delta > 0 && hue_slider.Value + step <= hue_slider.Maximum)
        hue_slider.Value += step;
      else if (e.Delta < 0 && hue_slider.Value - step >= hue_slider.Minimum)
        hue_slider.Value -= step;
      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }


  }
}
