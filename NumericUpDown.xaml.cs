using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace 小科狗配置
{
  /// <summary>
  /// NumericUpDown.xaml 的交互逻辑
  /// </summary>
  public partial class NumericUpDown : UserControl
  {
    public static readonly DependencyProperty ValueProperty =
              DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown),
                  new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuePropertyChanged));

    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register("MinValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0));

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register("MaxValue", typeof(int), typeof(NumericUpDown), new PropertyMetadata(100));

    public event RoutedPropertyChangedEventHandler<int> ValueChanged;

    public int Value
    {
      get { return (int)GetValue(ValueProperty); }
      set { SetValue(ValueProperty, value); }
    }

    public int MinValue
    {
      get { return (int)GetValue(MinValueProperty); }
      set { SetValue(MinValueProperty, value); }
    }

    public int MaxValue
    {
      get { return (int)GetValue(MaxValueProperty); }
      set { SetValue(MaxValueProperty, value); }
    }

    public NumericUpDown()
    {
      InitializeComponent();
      this.DataContext = this;
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      NumericUpDown numericUpDown = (NumericUpDown)d;
      int oldValue = (int)e.OldValue;
      int newValue = (int)e.NewValue;

      if (numericUpDown.ValueChanged != null)
      {
        // 创建事件参数并调用事件
        RoutedPropertyChangedEventArgs<int> args = new RoutedPropertyChangedEventArgs<int>(oldValue, newValue);
        numericUpDown.ValueChanged(numericUpDown, args);
      }
    }


    protected virtual void OnValueChanged(RoutedPropertyChangedEventArgs<int> args)
    {
      ValueChanged?.Invoke(this, args);
    }

    private void Increase_Click(object sender, RoutedEventArgs e)
    {
      if (Value < MaxValue) Value++;
    }

    private void Decrease_Click(object sender, RoutedEventArgs e)
    {
      if (Value > MinValue) Value--;
    }

    private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      int step = 1; // 设置滚动步长，默认每次滚动增加或减少1
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= 2; // 如果按住Ctrl键，则增大步长

      if (e.Delta > 0) // 滚动向上
      {
        if (Value + step <= MaxValue)
          Value += step;
      }
      else if (e.Delta < 0) // 滚动向下
      {
        if (Value - step >= MinValue)
          Value -= step;
      }

      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }

























    //public int Value
    //{
    //  get { return value; }
    //  set
    //  {
    //    this.value = value;
    //    NUDTextBox.Text = Value.ToString();
    //  }
    //}
    // 2. 修改Value属性，添加事件触发逻辑


    //public int Value
    //{
    //  get { return starValue; }
    //  set
    //  {
    //    // 只在值有变化时触发事件
    //    if (starValue != value)
    //    {
    //      starValue = value;
    //      NUDTextBox.Text = starValue.ToString();
    //      // 触发ValueChanged事件
    //      ValueChanged?.Invoke(this, EventArgs.Empty);
    //    }
    //}



    //// 定义依赖属性
    //public static readonly DependencyProperty ValueProperty =
    //  DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown), new FrameworkPropertyMetadata(0, new PropertyChangedCallback(OnValueChanged)));

    //// 1. 定义一个Value改变的事件
    //public event EventHandler ValueChanged;

    //public int MinValue { get; set; } = 0;
    //public int MaxValue { get; set; } = 100;
    //private int starValue = 0;

    //public int Value
    //{
    //  get { return (int)GetValue(ValueProperty); }
    //  set { SetValue(ValueProperty, value); }
    //}
    //private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //{
    //  NumericUpDown control = (NumericUpDown)d;
    //  // 触发ValueChanged事件
    //  control.ValueChanged?.Invoke(control, new RoutedEventArgs());
    //}



    //public NumericUpDown()
    //{
    //  InitializeComponent();
    //}

    //private void NUDButtonUP_Click(object sender, RoutedEventArgs e)
    //{
    //  int number;
    //  if (NUDTextBox.Text != "") number = Convert.ToInt32(NUDTextBox.Text);
    //  else number = 0;
    //  if (number < MaxValue)
    //    NUDTextBox.Text = Convert.ToString(number + 1);
    //}

    //private void NUDButtonDown_Click(object sender, RoutedEventArgs e)
    //{
    //  int number;
    //  if (NUDTextBox.Text != "") number = Convert.ToInt32(NUDTextBox.Text);
    //  else number = 0;
    //  if (number > MinValue)
    //    NUDTextBox.Text = Convert.ToString(number - 1);
    //}

    //private void NUDTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    //{

    //  if (e.Key == Key.Up)
    //  {
    //    NUDButtonUP.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    //    typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonUP, new object[] { true });
    //  }


    //  if (e.Key == Key.Down)
    //  {
    //    NUDButtonDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    //    typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonDown, new object[] { true });
    //  }
    //}

    //private void NUDTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
    //{
    //  if (e.Key == Key.Up)
    //    typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonUP, new object[] { false });

    //  if (e.Key == Key.Down)
    //    typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(NUDButtonDown, new object[] { false });
    //}

    //private void NUDTextBox_TextChanged(object sender, TextChangedEventArgs e)
    //{
    //  int number = 0;
    //  if (NUDTextBox.Text != "")
    //    if (!int.TryParse(NUDTextBox.Text, out number)) NUDTextBox.Text = Value.ToString();
    //  if (number > MaxValue) NUDTextBox.Text = MaxValue.ToString();
    //  if (number < MinValue) NUDTextBox.Text = MinValue.ToString();
    //  NUDTextBox.SelectionStart = NUDTextBox.Text.Length;
    //}

  }
}
