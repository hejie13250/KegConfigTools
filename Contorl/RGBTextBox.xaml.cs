using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace 小科狗配置
{
  /// <summary>
  /// RGBTextBox.xaml 的交互逻辑
  /// </summary>
  public partial class RGBTextBox : UserControl
  {
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            "RGBText",
            typeof(string),
            typeof(RGBTextBox),
            new FrameworkPropertyMetadata(
                "(255, 255, 255)", // 修改这里为与 RGBText 属性类型相匹配的默认字符串值
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnValuePropertyChanged));

    public event RoutedPropertyChangedEventHandler<string> ValueChanged;

    private string rgbText;
    public string RGBText
    {
      get { return rgbText; }
      set { SetColorText(value); }
    }

    public RGBTextBox()
    {
      InitializeComponent();
      R.TextChanged += RGBTextBoxTextChanged;
      G.TextChanged += RGBTextBoxTextChanged;
      B.TextChanged += RGBTextBoxTextChanged;
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      RGBTextBox rgbText = (RGBTextBox)d;
      string oldValue = (string)e.OldValue;
      string newValue = (string)e.NewValue;

      rgbText.OnValueChanged(new RoutedPropertyChangedEventArgs<string>(oldValue, newValue));
    }

    protected virtual void OnValueChanged(RoutedPropertyChangedEventArgs<string> args)
    {
      ValueChanged?.Invoke(this, args);
    }

    private void RGBTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
      TextBox textBox = sender as TextBox;

      if (int.TryParse(textBox.Text, out int value))
      {
        if (value < 0)
          value = 0;
        else if (value > 255)
          value = 255;
        textBox.Text = value.ToString(); // 更新文本框内容为有效范围内的整数值
      }
      else
        textBox.Text = "0"; // 输入文本无法转换为有效整数，重置为默认值0

      RGBText = $"({R.Text}, {G.Text}, {B.Text})";
    }


    private void SetColorText(string colorText)
    {
      string oldRgbText = rgbText; // 声明并初始化旧值变量
      //string rgb = colorText.Replace("(", "").Replace(")", "");
      string[] rgbValues = colorText.Trim('(', ')').Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
      R.Text = rgbValues[0];
      G.Text = rgbValues[1];
      B.Text = rgbValues[2];
      rgbText = $"({R.Text}, {G.Text}, {B.Text})"; // 更新 rgbText 成员变量
      OnValueChanged(new RoutedPropertyChangedEventArgs<string>(oldRgbText, rgbText));
    }



    private void ColorTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      int step = 1; // 设置滚动步长，默认每次滚动增加或减少1
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= 2; // 如果按住Ctrl键，则增大步长

      TextBox textBox = sender as TextBox;
      int value = int.Parse(textBox.Text);

      if (e.Delta > 0) // 滚动向上
        value = Math.Min(value + step, 255); // 确保增加后的值不超过255

      else if (e.Delta < 0) // 滚动向下
        value = Math.Max(value - step, 0); // 确保减小后的值不低于0

      textBox.Text = value.ToString();

      OnValueChanged(new RoutedPropertyChangedEventArgs<string>(rgbText, $"({R.Text}, {G.Text}, {B.Text})"));
      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }


  }
}
