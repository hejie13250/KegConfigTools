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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using System.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static 小科狗配置.GlobalSetting;
using static 小科狗配置.ModiflyTable;
using LiveChartsCore.Defaults;
using Newtonsoft.Json.Linq;
using System.Drawing.Text;
using System.Text.RegularExpressions;


namespace 小科狗配置
{
  /// <summary>
  /// ModiflyTable.xaml 的交互逻辑
  /// </summary>
  public partial class ModiflyTable : BasePage
  {

    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion

    public class ListViewDataItem : INotifyPropertyChanged
    {
      private int _num;
      private string _key;
      private string _value;
      private double _weight;
      private string _fc;
      private bool _isDel;
      private bool _isMod;
      private bool _isAdd;

      public int Num
      {
        get => _num;
        set => SetProperty(ref _num, value);
      }
      public string Key
      {
        get => _key;
        set => SetProperty(ref _key, value);
      }

      public string Value
      {
        get => _value;
        set => SetProperty(ref _value, value);
      }

      public double Weight
      {
        get => _weight;
        set => SetProperty(ref _weight, value);
      }

      public string FC
      {
        get => _fc;
        set => SetProperty(ref _fc, value);
      }

      public bool IsDel
      {
        get => _isDel;
        set => SetProperty(ref _isDel, value);
      }
      public bool IsMod
      {
        get => _isMod;
        set => SetProperty(ref _isMod, value);
      }
      public bool IsAdd
      {
        get => _isAdd;
        set => SetProperty(ref _isAdd, value);
      }
      public event PropertyChangedEventHandler PropertyChanged;

      protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
      {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
      }
    }

    public ObservableCollection<ListViewDataItem> ListViewData { get; set; }

    readonly string dbPath;     // Keg.db 
    string tableName;           // 表名称
    int    pageCount;           // 总页码
    int  currentPage;           // 当前页码
    int      pageLen;           // 每页数据行数       
    bool editingStatus = false; // 编缉状态

    public ModiflyTable()
    {
      dbPath = Base.kegPath + "Keg.db";
      InitializeComponent();
      LoadFontNames();
      LoadTableNames();
      //listViewDataItems = new ObservableCollection<ListViewDataItem>();
    }

    #region 读写db
    /// <summary>
    /// 从 db 读取表名到 ComboBox
    /// </summary>
    private void LoadTableNames()
    {
      SQLiteConnection connection = new ($"Data Source={dbPath}");
      connection.Open();
      try
      {
        comboBox.Items.Clear();
        using var command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
          var labelName = reader.GetString(0);
          comboBox.Items.Add(labelName);
          comboBox.SelectedIndex = 0;
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error loading table names: {ex.Message}");
      }
      finally
      {
        connection.Close();
      }
    }

    /// <summary>
    /// 获取指定表内词条总数
    /// </summary>
    /// <param name="tableName">表名称</param>
    /// <returns></returns>
    private int GetTableRowCount(string tableName)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT COUNT(*) FROM '{tableName}'";
      connection.Open();
      using SQLiteCommand command = new SQLiteCommand(query, connection);
      int rowCount = Convert.ToInt32(command.ExecuteScalar());
      return rowCount;
    }

    /// <summary>
    /// 从指定表内获取指定条数的数据
    /// </summary>
    /// <param name="offset">起始索引</param>
    /// <param name="limit">数据的条数</param>
    /// <returns></returns>
    private DataTable GetTopRecordsFromTable(int offset, int limit)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT * FROM '{tableName}' LIMIT {limit} OFFSET {offset}";
      connection.Open();
      using SQLiteDataAdapter adapter = new(query, connection);
      DataTable dataTable = new();
      adapter.Fill(dataTable);
      return dataTable;
    }





    #endregion











    /// <summary>
    /// 将数据填充到 ListView
    /// </summary>
    /// <param name="tableName"></param>
    private void PopulateListView(DataTable dataTable)
    {
      ListViewData = new ObservableCollection<ListViewDataItem>();
      int num = currentPage * pageLen + 1;

      foreach (DataRow row in dataTable.Rows)
      {
        ListViewData.Add(new ListViewDataItem
        {
          Num = num ++,
          Key = row["Key"].ToString(),
          Value = row["Value"].ToString(),
          Weight = Convert.ToDouble(row["Weight"]),
          FC = row["FC"].ToString()
        });
      }

      listView.ItemsSource = ListViewData;
    }







    // 切换表
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      tableName = comboBox.SelectedValue as string;
      int count = GetTableRowCount(tableName);
      pageLen = 5000;
      pageCount = count / pageLen;
      if (count % pageLen > 0) pageCount++;

      textBox1.Text = $"词条总数：{count}";
      currentPage = 0;
      textBox.Text = $"{currentPage + 1}/{pageCount}";
      DataTable dataTable = GetTopRecordsFromTable(currentPage * pageLen, pageLen);
      PopulateListView(dataTable);
      string fontName = GetFontToTable(tableName);
      if (fontName != null)
        for (int i = 0; i < fontComboBox.Items.Count; i++)
          if (fontComboBox.Items[i].ToString() == fontName)
            fontComboBox.SelectedIndex = i;


    }

    // 改 ListView 的字体
    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (fontComboBox.SelectedValue == null) return;

      string fontName = fontComboBox.SelectedItem.ToString();
      listView.FontFamily = new System.Windows.Media.FontFamily(fontName);
      WriteFontToTable(fontName);
    }

    /// <summary>
    /// 读取系统字体列表
    /// </summary>
    private void LoadFontNames()
    {
      foreach (System.Drawing.FontFamily font in System.Drawing.FontFamily.Families)
      {
        if (ContainsChineseCharacters(font.Name))
          fontComboBox.Items.Add(font.Name);
      }
    }

    /// <summary>
    /// 正则排除 字体名称 里没有中文的字体
    /// </summary>
    /// <param name="fontName"></param>
    /// <returns></returns>
    private bool ContainsChineseCharacters(string fontName)
    {
      Regex chineseRegex = new(@"[\u4e00-\u9fff]");

      return chineseRegex.IsMatch(fontName);
    }

    // ComboBox 获取焦点
    private void ComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      var cb = sender as ComboBox;
      cb.Focus();
    }


    /// <summary>
    /// 从指定表 tableName 内读取 key 列为""时 value 的值
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    private string GetFontToTable(string tableName)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT value FROM '{tableName}' WHERE key = '字体'";
      connection.Open();
      using SQLiteCommand command = new(query, connection);

      string result;
      using (SQLiteDataReader reader = command.ExecuteReader())
      {
        if (reader.Read()) result = reader["value"].ToString();
        else result = null;
      }
      connection.Close();
      return result;
    }

    /// <summary>
    /// 保存字体名称到指定表
    /// </summary>
    /// <param name="value"></param>
    private void WriteFontToTable(string fontName)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      connection.Open();
      // 检查是否已存在该键，如果存在则更新，否则插入新记录
      string checkQuery = $"SELECT COUNT(*) FROM '{tableName}' WHERE key = '字体'";
      using SQLiteCommand checkCommand = new(checkQuery, connection);
      long count = (long)checkCommand.ExecuteScalar();

      if (count > 0)
      {
        // 更新已存在的记录
        string updateQuery = $"UPDATE '{tableName}' SET value = @font WHERE key = '字体'";
        using SQLiteCommand updateCommand = new(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("@font", fontName);
        updateCommand.ExecuteNonQuery();
      }
      else
      {
        // 插入新记录
        //string insertQuery = $"INSERT INTO '{tableName}' (key, value) VALUES ('字体', @font)";
        //using SQLiteCommand insertCommand = new(insertQuery, connection);
        //insertCommand.Parameters.AddWithValue("@font", fontName);
        //insertCommand.ExecuteNonQuery();
      }

      connection.Close();
    }


    // 第一页
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      if (currentPage == 0) return;
      if (editingStatus == true){
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      currentPage = 0;
      textBox.Text = $"{currentPage + 1}/{pageCount}";
      DataTable dataTable = GetTopRecordsFromTable(currentPage * pageLen, pageLen);
      PopulateListView(dataTable);
    }


    // 上一页
    private void Button1_Click(object sender, RoutedEventArgs e)
    {
      if (currentPage == 0) return;
      if (editingStatus == true)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      currentPage --;
      textBox.Text = $"{currentPage + 1}/{pageCount}";
      DataTable dataTable = GetTopRecordsFromTable(currentPage * pageLen, pageLen);
      PopulateListView(dataTable);
    }

    // 鼠标滚轮翻页
    private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      e.Handled = true;                                         // 阻止滚轮事件继续向上冒泡
      if (editingStatus == true)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }
      int step = 1; // 设置滚动步长，默认每次滚动增加或减少1
      if (Keyboard.Modifiers == ModifierKeys.Control) step = 5; // 如果按住Ctrl键，步长为5
      if (Keyboard.Modifiers == ModifierKeys.Shift) step = 10;  // 如果按住Shift键，步长为10
      int value = currentPage + 1;

      if (e.Delta > 0 && value + step <= pageCount)             // 滚动向上
        value += step;
      if (e.Delta < 0 && value - step >= 1)                     // 滚动向下
        value -= step;

      currentPage  = value -1;
      textBox.Text = $"{currentPage + 1}/{pageCount}";
      DataTable dataTable = GetTopRecordsFromTable(currentPage * pageLen, pageLen);
      PopulateListView(dataTable);
    }

    // 下一页
    private void Button2_Click(object sender, RoutedEventArgs e)
    {
      if (currentPage == pageCount - 1) return;
      if (editingStatus == true)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      currentPage ++;
      textBox.Text = $"{currentPage + 1}/{pageCount}";
      DataTable dataTable = GetTopRecordsFromTable(currentPage * pageLen, pageLen);
      PopulateListView(dataTable);
    }

    // 最后页
    private void Button3_Click(object sender, RoutedEventArgs e)
    {
      if (currentPage == pageCount -1) return;
      if (editingStatus == true)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      currentPage = pageCount - 1;
      textBox.Text = $"{currentPage + 1}/{pageCount}";
      DataTable dataTable = GetTopRecordsFromTable(currentPage * pageLen, pageLen);
      PopulateListView(dataTable);
    }

    // 刷新
    private void Button4_Click(object sender, RoutedEventArgs e)
    {

    }














    private void GetList_button_Click(object sender, RoutedEventArgs e)
    {

      //listViewDataItems.Clear();
      //LoadTableNames();
    }


    private void Reload_button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void Del_button_Click(object sender, RoutedEventArgs e)
    {

      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        item.IsDel = true;
      }
      editingStatus = true;
      comboBox.Visibility = Visibility.Collapsed;

    }

    private void Add_button_Click(object sender, RoutedEventArgs e)
    {
      if (currentPage != pageCount - 1)
      {
        MessageBox.Show("添加词条请转到最后一页。");
        return;
      }


      ListViewDataItem newItem = new()
      {
        Key = "新键",
        Value = "新值",
        Weight = 1450,
        FC = "90EE90",
        IsAdd = true
      };

      ListViewData.Add(newItem);
      editingStatus = true;
      comboBox.Visibility = Visibility.Collapsed;
    }

    private void Mod_button_Click(object sender, RoutedEventArgs e)
    {
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        item.IsMod = true;
      }
      editingStatus = true;
      comboBox.Visibility = Visibility.Collapsed;

    }


  }
}
