using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace 小科狗配置.DialogBox
{
  /// <summary>
  /// ReNameDialogBox.xaml 的交互逻辑
  /// </summary>
  public partial class ReNameDialogBox : Window
  {
    public new string Name  { get; set; }
    public new string Title { get; }

    public ReNameDialogBox(string title, string name)
    {
      InitializeComponent();
      Title                  = title;
      Name                   = name;
      titleTextBlock.Content = Title;
      nameTextBox.Text       = Name;
    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
      if (nameTextBox.Text == Name)
      {
        MessageBox.Show("名称没改");
        return;
      }
      Name = nameTextBox.Text;
      Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }
  }
}
