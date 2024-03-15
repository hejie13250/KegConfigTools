using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;



namespace 小科狗配置
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  /// 
  public partial class MainWindow : Window
  {
    public WriteableBitmap Bitmap { get; set; }
    public class ColorScheme
    {
      public string 名称 { get; set; }
      public bool 显示背景图 { get; set; }
      public bool 显示候选窗圆角 { get; set; }
      public bool 显示选中项背景圆角 { get; set; }
      public int 候选窗圆角 { get; set; }
      public int 选中项圆角 { get; set; }
      public int 边框线宽 { get; set; }
      public string 下划线色 { get; set; }
      public string 光标色 { get; set; }
      public string 分隔线色 { get; set; }
      public string 窗口边框色 { get; set; }
      public string 窗背景底色 { get; set; }
      public string 选中背景色 { get; set; }
      public string 选中字体色 { get; set; }
      public string 编码字体色 { get; set; }
      public string 候选字色 { get; set; }
    }
    public class ColorSchemesCollection
    {
      public List<ColorScheme> 配色方案 { get; set; }
    }

    List<ColorScheme> 配色方案 = new();
    ColorScheme colorScheme = new()
    {
      名称 = "默认",
      显示背景图 = false,
      显示候选窗圆角 = true,
      显示选中项背景圆角 = true,
      候选窗圆角 = 15,
      选中项圆角 = 10,
      边框线宽 = 1,
      下划线色 = "#FF0000",
      光标色 = "#004CFF",
      分隔线色 = "#000000",
      窗口边框色 = "#000000",
      窗背景底色 = "#FFFFFF",
      选中背景色 = "#000000",
      选中字体色 = "#333333",
      编码字体色 = "#000000",
      候选字色 = "#000000"
    };
    readonly String filePath = "配色方案.json";
    int select_color_label = 0;


    public MainWindow()
    {
      InitializeComponent();
      this.Height = 420;
      Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {

      //button3_Copy9.Visibility = Visibility.Hidden;
      //color_textBox_0_Copy.Visibility = Visibility.Hidden;

      // 确保窗口和其包含的所有控件已经加载完成
      //hxc_checkBox.Click += hxc_checkBox_Click;
      //hxcbj_checkBox.Click += hxcbj_checkBox_Click;



      Bitmap = new WriteableBitmap(255, 255, 255, 255, PixelFormats.Bgra32, null);
      DataContext = this;
      UpdateBitmap();

      //Canvas.SetLeft(thumb, 50);
      //Canvas.SetTop(thumb, 50);
      //var thumbPosition = new Point(1128, 128);
      //GetAreaColor(thumbPosition);
      //hxk_border.CornerRadius = new CornerRadius(nud11.Value);

      LoadJson();
    }


    void LoadJson()
    {
      if (File.Exists(filePath))
      {
        // 读取整个文件内容,将JSON字符串反序列化为对象
        string json = File.ReadAllText(filePath);
        ColorSchemesCollection colorSchemesJson = JsonConvert.DeserializeObject<ColorSchemesCollection>(json);
        配色方案 = colorSchemesJson.配色方案;

        foreach (var scheme in 配色方案)
        {
          colorSchemeListBox.Items.Add(scheme.名称);
        }
      }
      else
      {
        配色方案.Add(colorScheme);
        string json = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
        File.WriteAllText(filePath, json);

        colorSchemeListBox.Items.Add("默认");
      }
    }


    //检查是否有同名配色
    bool IsMatch(String name)
    {

      foreach (var item in colorSchemeListBox.Items)
      {
        if (item.ToString() == name) return true;
      }
      return false;
    }


    public static string RemoveChars(string str, int n)
    {
      str = str.Replace("#", ""); // 移除可能存在的井号
      return "#" + str.Substring(2, str.Length - n);
    }


    private void UpdateBitmap()
    {
      int width = 255;
      int height = 255;
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

      Bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
      Bitmap.Unlock();
    }




    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
      var thumb = (Thumb)sender;
      double newLeft = Canvas.GetLeft(thumb) + e.HorizontalChange;
      double newTop = Canvas.GetTop(thumb) + e.VerticalChange;
      double canvasRight = canvas.ActualWidth - thumb.ActualWidth + 5;
      double canvasBottom = canvas.ActualHeight - thumb.ActualHeight + 5;

      if (newLeft < -6)
        newLeft = -6;
      else if (newLeft > canvasRight)
        newLeft = canvasRight;

      if (newTop < -6)
        newTop = -6;
      else if (newTop > canvasBottom)
        newTop = canvasBottom;

      Canvas.SetLeft(thumb, newLeft);
      Canvas.SetTop(thumb, newTop);

      GetAreaColor();
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var canvasPosition = e.GetPosition(canvas);
      double newLeft = canvasPosition.X - thumb.ActualWidth / 2;
      double newTop = canvasPosition.Y - thumb.ActualHeight / 2;

      double canvasRight = canvas.ActualWidth - thumb.ActualWidth;
      double canvasBottom = canvas.ActualHeight - thumb.ActualHeight;

      if (newLeft < 0)
        newLeft = 0;
      else if (newLeft > canvasRight)
        newLeft = canvasRight;

      if (newTop < 0)
        newTop = 0;
      else if (newTop > canvasBottom)
        newTop = canvasBottom;

      Canvas.SetLeft(thumb, newLeft);
      Canvas.SetTop(thumb, newTop);
      var thumbPosition = e.GetPosition(canvas);
      GetAreaColor(thumbPosition);
    }

    private void Hue_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      color_textBox_0.Text = (hue_slider.Value / 360).ToString();
      UpdateBitmap();
      GetAreaColor();
    }

    private void Hue_slider_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      int step = -5;
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= -10;

      if (e.Delta > 0 && hue_slider.Value + step <= hue_slider.Maximum)
      {
        hue_slider.Value += step;
      }
      else if (e.Delta < 0 && hue_slider.Value - step >= hue_slider.Minimum)
      {
        hue_slider.Value -= step;
      }

      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }

    void GetAreaColor(Point? thumbPosition = null)
    {
      thumbPosition = thumbPosition == null ? _ = thumb.TranslatePoint(new Point(thumb.ActualWidth / 2, thumb.ActualHeight / 2), canvas) : thumbPosition;
      int xCoordinate = (int)thumbPosition?.X;
      int yCoordinate = (int)thumbPosition?.Y;

      if (xCoordinate >= 0 && xCoordinate < Bitmap.PixelWidth && yCoordinate >= 0 && yCoordinate < Bitmap.PixelHeight)
      {
        int stride = Bitmap.PixelWidth * (Bitmap.Format.BitsPerPixel / 8);

        byte[] pixels = new byte[Bitmap.PixelHeight * stride];
        Bitmap.CopyPixels(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight), pixels, stride, 0);
        int pixelIndex = (yCoordinate * stride) + (xCoordinate * (Bitmap.Format.BitsPerPixel / 8));
        Color color = Color.FromArgb(pixels[pixelIndex + 3], pixels[pixelIndex + 2], pixels[pixelIndex + 1], pixels[pixelIndex]);
        var c_color = new SolidColorBrush(color);
        color_textBox_0.Text = $"({color.R}, {color.G}, {color.B})"; // 格式化 color 为字符串
        color_label_10.Background = c_color;

        Color invertedColor = Color.FromRgb((byte)(255 - color.R), (byte)(255 - color.G), (byte)(255 - color.B)); // 计算反色
        thumb.BorderBrush = new SolidColorBrush(invertedColor);

        //var i_color = new SolidColorBrush(invertedColor);
        //color_label_content.Foreground = i_color;

        // 依select_color_label的值更新指定控件相关属性
        switch (select_color_label)
        {
          //case 0:
          //  color_border_0.Background = c_color; break;
          case 1: //嵌入下划线色
            color_label_1.Background = c_color;
            hxz_label_xhx.BorderBrush = c_color;
            break;
          case 2: //光标色
            color_label_2.Background = c_color;
            hxz_label_gb.BorderBrush = c_color;
            break;
          case 3: //分隔线色
            color_label_3.Background = c_color;
            hxz_label_fgx.BorderBrush = c_color;
            break;
          case 4: //候选窗口边框色
            color_label_4.Background = c_color;
            hxk_border.BorderBrush = c_color;
            break;
          case 5: //候选窗背景底色
            color_label_5.Background = c_color;
            hxk_border.Background = c_color;
            break;
          case 6: //候选选中背景色
            color_label_6.Background = c_color;
            hxz_border.Background = c_color;
            break;
          case 7: //候选选中字体色
            color_label_7.Background = c_color;
            //hxz_label_1.Foreground = c_color;
            hxz_label_3.Foreground = c_color;
            break;
          case 8: //编码字体色
            color_label_8.Background = c_color;
            hxz_label_0.Foreground = c_color;
            break;
          case 9: //候选字体色
            color_label_9.Background = c_color;
            hxz_label_1.Foreground = c_color;
            hxz_label_2.Foreground = c_color;
            //hxz_label_3.Foreground = c_color;
            hxz_label_4.Foreground = c_color;
            hxz_label_5.Foreground = c_color;
            hxz_label_6.Foreground = c_color;
            break;
          case 10:
            color_label_10.Background = c_color; break;
        }
      }
    }

    // 颜色转换 HSVToRGB
    private static void HSVToRGB(double h, double s, double v, out byte r, out byte g, out byte b)
    {
      if (s == 0)
      {
        r = g = b = (byte)(v * 255);
      }
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

    // 颜色转换 RgbToHex
    public static string RgbToHex(string rgb)
    {
      // 预期rgb字符串格式如 "255, 128, 0"
      string[] rgbValues = rgb.Trim().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

      if (rgbValues.Length != 3)
      {
        throw new ArgumentException("Invalid RGB string format. Expecting a string like '255, 255, 255'.");
      }

      if (!byte.TryParse(rgbValues[0], out byte r) || !byte.TryParse(rgbValues[1], out byte g) || !byte.TryParse(rgbValues[2], out byte b))
      {
        throw new FormatException("RGB values must be integers between 0 and 255.");
      }

      // 将字节转换为十六进制字符串，并去掉前导零
      return $"{r:X2}{g:X2}{b:X2}";
    }

    // 颜色转换 HexToRgb
    public static string HexToRgb(string hex)
    {
      // 预期hex字符串格式如 "FF8000" 或 "#FF8000"
      hex = hex.Replace("#", ""); // 移除可能存在的井号

      if (hex.Length != 6)
      {
        throw new ArgumentException("Invalid HEX color string. Expecting a string like '#FF8000' or 'FF8000'.");
      }

      byte r = Convert.ToByte(hex.Substring(0, 2), 16);
      byte g = Convert.ToByte(hex.Substring(2, 2), 16);
      byte b = Convert.ToByte(hex.Substring(4, 2), 16);

      return $"{r}, {g}, {b}";
    }


    // 显示颜色的 label 鼠标放开事件
    private void Color_label_MouseUp(object sender, MouseButtonEventArgs e)
    {
      Label label = sender as Label;
      switch (label.Name)
      {
        case "color_label_1":
          color_label_content.Content = "嵌入下划线色";
          select_color_label = 1;
          break;
        case "color_label_2":
          select_color_label = 2;
          color_label_content.Content = "光标色";
          break;
        case "color_label_3":
          color_label_content.Content = "分隔线色";
          select_color_label = 3;
          break;
        case "color_label_4":
          select_color_label = 4;
          color_label_content.Content = "候选窗口边框色";
          break;
        case "color_label_5":
          select_color_label = 5;
          color_label_content.Content = "候选窗背景底色";
          break;
        case "color_label_6":
          select_color_label = 6;
          color_label_content.Content = "候选选中背景色";
          break;
        case "color_label_7":
          select_color_label = 7;
          color_label_content.Content = "候选选中字体色";
          break;
        case "color_label_8":
          select_color_label = 8;
          color_label_content.Content = "编码字体色";
          break;
        case "color_label_9":
          select_color_label = 9;
          color_label_content.Content = "候选字体色";
          break;
        case "color_label_10":
          select_color_label = 0;
          color_label_content.Content = "";
          break;
      }
    }

    // 显示颜色的 label 鼠标进入事件
    private void Color_label_MouseEnter(object sender, MouseEventArgs e)
    {
      Label label = sender as Label;
      label.BorderThickness = new Thickness(3);
      color_label_10.Background = label.Background;
      var hex = RemoveChars(label.Background.ToString(), 2);
      var rgb = HexToRgb(hex);
      color_textBox_0.Text = $"({rgb})";
    }

    // 显示颜色的 label 鼠标离开事件
    private void Color_label_MouseLeave(object sender, MouseEventArgs e)
    {
      Label label = sender as Label;
      label.BorderThickness = new Thickness(1);
    }

    // 候选框圆角、选中项背景圆角 和 候选框边框调节
    private void Nud11_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      if (hxk_border != null)
      {
        if (hxc_checkBox.IsChecked == true)
          hxk_border.CornerRadius = new CornerRadius(nud11.Value);
        else
          hxk_border.CornerRadius = new CornerRadius(0);
        if (hxcbj_checkBox.IsChecked == true)
          hxz_border.CornerRadius = new CornerRadius(nud12.Value);
        else
          hxz_border.CornerRadius = new CornerRadius(0);
        hxk_border.BorderThickness = new Thickness(nud13.Value);
      }
    }

    // 候选框圆角 复选框
    private void Hxc_checkBox_Click(object sender, RoutedEventArgs e)
    {
      if (nud11.IsEnabled == true)
      {
        hxk_border.CornerRadius = new CornerRadius(nud11.Value);
      }
      else
      {
        hxk_border.CornerRadius = new CornerRadius(0);
      }
    }

    // 选中项背景圆角 复选框
    private void Hxcbj_checkBox_Click(object sender, RoutedEventArgs e)
    {
      if (nud12.IsEnabled == true)
      {
        hxz_border.CornerRadius = new CornerRadius(nud12.Value);
      }
      else
      {
        hxz_border.CornerRadius = new CornerRadius(0);
      }
    }

    // 配色列表双击事件
    private void ColorSchemeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left && colorSchemeListBox.SelectedItem != null)
      {
        var colorScheme = 配色方案[colorSchemeListBox.SelectedIndex];
        checkBox_Copy42.IsChecked = colorScheme.显示背景图;
        hxc_checkBox.IsChecked = colorScheme.显示候选窗圆角;
        hxcbj_checkBox.IsChecked = colorScheme.显示选中项背景圆角;
        nud11.Value = colorScheme.候选窗圆角;
        nud12.Value = colorScheme.选中项圆角;
        nud13.Value = colorScheme.边框线宽;
        color_label_1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.下划线色));
        color_label_2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.光标色));
        color_label_3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.分隔线色));
        color_label_4.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.窗口边框色));
        color_label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.窗背景底色));
        color_label_6.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.选中背景色));
        color_label_7.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.选中字体色));
        color_label_8.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.编码字体色));
        color_label_9.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorScheme.候选字色));
      }
    }

    // 配色列表选中项改变事件
    private void ColorSchemeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem != null)
      {
        if (saveButton.Content.ToString() == "保存配色")
        {
          color_scheme_name_textBox.Text = "";
        }
        if (saveButton.Content.ToString() == "修改配色")
        {
          color_scheme_name_textBox.Text = colorSchemeListBox.SelectedItem.ToString();
        }
      }
    }

    // 新建配色方案
    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
      saveButton.Content = "保存配色";
      saveButton.Visibility = Visibility.Visible;
      color_scheme_name_textBox.Visibility = Visibility.Visible;
    }

    // 修改配色方案
    private void MenuItem_Click_2(object sender, RoutedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem == null)
      {
        MessageBox.Show("您没有选中任何配色！",
        "修改操作",
        MessageBoxButton.OK,
        MessageBoxImage.Question);
        return;
      }
      saveButton.Content = "修改配色";
      saveButton.Visibility = Visibility.Visible;
      color_scheme_name_textBox.Visibility = Visibility.Visible;
      color_scheme_name_textBox.Text += colorSchemeListBox.SelectedItem.ToString();
    }

    // 删除选中配色方案
    private void MenuItem_Click_3(object sender, RoutedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem == null)
      {
        MessageBox.Show("您没有选中任何配色！",
        "删除操作",
        MessageBoxButton.OK,
        MessageBoxImage.Question);
        return;
      }
      var name = colorSchemeListBox.SelectedItem.ToString();
      var result = MessageBox.Show(
      $"您确定要删除 {name} 吗？",
      "删除操作",
      MessageBoxButton.OKCancel,
      MessageBoxImage.Question);

      if (result == MessageBoxResult.OK)
      {
        配色方案.RemoveAt(colorSchemeListBox.SelectedIndex);
        string json = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
        File.WriteAllText(filePath, json);

        colorSchemeListBox.Items.Remove(name);
        colorSchemeListBox.Items.Refresh();
      }

    }

    // 添加配色
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {

      var name = color_scheme_name_textBox.Text.Trim();
      colorScheme = new ColorScheme
      {
        名称 = name,
        显示背景图 = (bool)checkBox_Copy42.IsChecked,
        显示候选窗圆角 = (bool)hxc_checkBox.IsChecked,
        显示选中项背景圆角 = (bool)hxcbj_checkBox.IsChecked,
        候选窗圆角 = nud11.Value,
        选中项圆角 = nud12.Value,
        边框线宽 = nud13.Value,
        下划线色 = RemoveChars(color_label_1.Background.ToString(), 2),
        光标色 = RemoveChars(color_label_2.Background.ToString(), 2),
        分隔线色 = RemoveChars(color_label_3.Background.ToString(), 2),
        窗口边框色 = RemoveChars(color_label_4.Background.ToString(), 2),
        窗背景底色 = RemoveChars(color_label_5.Background.ToString(), 2),
        选中背景色 = RemoveChars(color_label_6.Background.ToString(), 2),
        选中字体色 = RemoveChars(color_label_7.Background.ToString(), 2),
        编码字体色 = RemoveChars(color_label_8.Background.ToString(), 2),
        候选字色 = RemoveChars(color_label_9.Background.ToString(), 2),
      };

      if (saveButton.Content.ToString() == "保存配色")
      {
        if (IsMatch(name))
        {
          MessageBox.Show("存在同名配色！");
          return;
        }
        配色方案.Add(colorScheme);
        colorSchemeListBox.Items.Add(name);
      }
      if (saveButton.Content.ToString() == "修改配色" && colorSchemeListBox.SelectedItem != null)
      {
        var n = colorSchemeListBox.SelectedIndex;
        配色方案[n] = colorScheme;
        colorSchemeListBox.Items.Clear();

        foreach (var scheme in 配色方案)
          colorSchemeListBox.Items.Add(scheme.名称);

        colorSchemeListBox.SelectedIndex = n;
      }
      string json = JsonConvert.SerializeObject(new { 配色方案 }, Formatting.Indented);
      File.WriteAllText(filePath, json);
    }


    private void Nud1_ValueChanged(object sender, EventArgs e)
    {

    }
  }
}
