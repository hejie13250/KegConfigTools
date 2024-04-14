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

    public void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
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

  }
}
