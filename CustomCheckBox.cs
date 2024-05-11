using System.Windows;
using System.Windows.Controls;

namespace 小科狗配置
{
    public class CustomCheckBox : CheckBox
    {
    // 定义"其它"和"主库"的依赖属性
    public static readonly DependencyProperty LeftTextProperty =
            DependencyProperty.Register(nameof(LeftText), typeof(string), typeof(CustomCheckBox), new PropertyMetadata("其它"));

    public static readonly DependencyProperty RightTextProperty =
            DependencyProperty.Register(nameof(RightText), typeof(string), typeof(CustomCheckBox), new PropertyMetadata("主库"));

        // 将依赖属性封装在自定义控件中
        public string LeftText
        {
            get => (string)GetValue(LeftTextProperty);
            set => SetValue(LeftTextProperty, value);
        }

        public string RightText
        {
            get => (string)GetValue(RightTextProperty);
            set => SetValue(RightTextProperty, value);
        }

        static CustomCheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomCheckBox), new FrameworkPropertyMetadata(typeof(CustomCheckBox)));
        }
    }
}