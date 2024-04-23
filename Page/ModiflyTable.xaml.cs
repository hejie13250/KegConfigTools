using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using GroupBox = System.Windows.Controls.GroupBox;
using ListView = System.Windows.Controls.ListView;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;


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
      private int? _weight;
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

      public int? Weight
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
          //comboBox.SelectedIndex = 0;
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
    /// 删除表内所有 key 为 null 和 "" 的行
    /// </summary>
    private void DelNullKey()
    {
      SQLiteConnection connection = new($"Data Source={dbPath}");
      connection.Open();
      try
      {
        using var command = new SQLiteCommand($"DELETE FROM '{tableName}' WHERE Key IS NULL OR TRIM(Key) = ''", connection);
        int rows = command.ExecuteNonQuery();
        if(rows != 0 ) MessageBox.Show($"已删除 {rows} 行空数据");
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
    /// 所有字段设为可空
    /// </summary>
    private void SetFieldCanBeNull()
    {
      using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath}"))
      {
        connection.Open();
        using (var transaction = connection.BeginTransaction())
        {
          try
          {
            // 1. 创建一个新表，其结构与旧表相同，但所有列都可以为null
            var tempTableName = "Temp_" + tableName;
            using (var command = new SQLiteCommand($"CREATE TABLE {tempTableName} (key TEXT NULL, value TEXT NULL, weight REAL NULL, fc TEXT NULL)", connection))
            {
              command.ExecuteNonQuery();
            }

            // 2. 将旧表的数据复制到新表中
            using (var command = new SQLiteCommand($"INSERT INTO {tempTableName} SELECT key, value, weight, fc FROM {tableName}", connection))
            {
              command.ExecuteNonQuery();
            }

            // 3. 删除旧表
            using (var command = new SQLiteCommand($"DROP TABLE {tableName}", connection))
            {
              command.ExecuteNonQuery();
            }

            // 4. 将新表重命名为旧表的名称
            using (var command = new SQLiteCommand($"ALTER TABLE {tempTableName} RENAME TO {tableName}", connection))
            {
              command.ExecuteNonQuery();
            }

            transaction.Commit();
            MessageBox.Show("所有字段设为可空");
          }
          catch (Exception ex)
          {
            transaction.Rollback();
            MessageBox.Show($"Error changing column nullability: {ex.Message}");
          }
        }
      }
    }










    /// <summary>
    /// 获取表内词条总数
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
    /// 从表内指定行开始获取指定行数的数据
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
          Num    = num++,
          Key    = row["Key"] != DBNull.Value ? row["Key"].ToString() : string.Empty,
          Value  = row["Value"] != DBNull.Value ? row["Value"].ToString() : string.Empty,
          Weight = row["Weight"] != DBNull.Value ? int.Parse(row["Weight"].ToString()) : null,
          FC     = row["FC"] != DBNull.Value ? row["FC"].ToString() : string.Empty
        });
      }

      listView.ItemsSource = ListViewData;
    }







    // 切换表
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      tableName = comboBox.SelectedValue as string;
      DelNullKey();         // 先删除空行

      int count = GetTableRowCount(tableName);
      pageLen = 5000;
      pageCount = count / pageLen;
      if (count % pageLen > 0) pageCount++;

      textBox1.Text = $"词条数：{count}";
      currentPage = 0;
      textBox.Text = $"{currentPage + 1}/{pageCount}";
      DataTable dataTable = GetTopRecordsFromTable(currentPage * pageLen, pageLen);
      PopulateListView(dataTable);
      string fontName = GetFontToTable(tableName);
      if (fontName != null)
        for (int i = 0; i < fontComboBox.Items.Count; i++)
          if (fontComboBox.Items[i].ToString() == fontName)
            fontComboBox.SelectedIndex = i;

      fontComboBox.IsEnabled = true;  //读取词条后才可以改字体
      stackPanel.IsEnabled   = true;  //读取词条后才可以翻页和搜索
    }

    // 改 ListView 的字体
    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (fontComboBox.SelectedValue == null) return;

      string fontName = fontComboBox.SelectedItem.ToString();
      var font        = new System.Windows.Media.FontFamily(fontName);
      listView.FontFamily      = font;
      keyTextBox.FontFamily    = font;
      valueTextBox.FontFamily  = font;
      weightTextBox.FontFamily = font;
      fcTextBox.FontFamily     = font;

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
        string insertQuery = $"INSERT INTO '{tableName}' (key, value) VALUES ('字体', @font)";
        using SQLiteCommand insertCommand = new(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@font", fontName);
        insertCommand.ExecuteNonQuery();
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
      if (editingStatus != true) return;
      var result = MessageBox.Show($"当前有数据没有提交，确定后将取消未提交的操作！", "刷新",
        MessageBoxButton.OKCancel, MessageBoxImage.Question);

      if (result != MessageBoxResult.OK) return;

      if (textBox2.Text != "")
      {
        DataTable dataTable = GetTopRecordsFromTable(currentPage * pageLen, pageLen);
        PopulateListView(dataTable); 
      }
      else 搜索();

      状态切换(true);
    }

    // 搜索
    private void Button5_Click(object sender, RoutedEventArgs e)
    {
      if (editingStatus == true)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }
      if (textBox2.Text == "" || comboBox.SelectedIndex < 0) return;

      搜索();
    }

    // 搜索
    private void TextBox2_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (textBox2.Text == "" || comboBox.SelectedIndex < 0) return;
      搜索();

    }

    private void 搜索()
    {
      string name = KeyOrValueCheckBox.IsChecked == true ? "value" : "key";
      string str = textBox2.Text;

      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      string query = $"SELECT * FROM '{tableName}' WHERE {name} LIKE @str";
      connection.Open();

      using SQLiteCommand command = new(query, connection);
      command.Parameters.AddWithValue("@str", $"{str}%");

      using SQLiteDataReader reader = command.ExecuteReader();
      DataTable dataTable = new();
      dataTable.Load(reader);

      PopulateListView(dataTable);
    }


    /// <summary>
    /// 编缉状态切换
    /// </summary>
    /// <param name="b"></param>
    private void 状态切换(bool b)
    {
      editingStatus           = b;
      comboBox    .IsEnabled  = b;
      fontComboBox.IsEnabled  = b;
      textBox2    .IsReadOnly = b;
    }






    private void GetList_button_Click(object sender, RoutedEventArgs e)
    {

      //listViewDataItems.Clear();
      //LoadTableNames();
    }


    private void Reload_button_Click(object sender, RoutedEventArgs e)
    {

    }

    // 标记删除选中项
    private void Del_button_Click(object sender, RoutedEventArgs e)
    {
      if (listView.SelectedItems.Count == 0) return;
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        item.IsDel = true;
      }
      状态切换(false);
    }

    // 添加或修改
    private void AddOrMod_button_Click(object sender, RoutedEventArgs e)
    {
      if (listView.Items.Count == 0) return;

      if ((bool)AddOrModCheckBox.IsChecked)
      {
        if (currentPage != pageCount - 1)
        {
          MessageBox.Show("添加词条请转到最后一页。");
          return;
        }
        if(是否有相同数据(keyTextBox.Text, valueTextBox.Text))
        {
          MessageBox.Show("存在相同编码和词条的数据。");
          return;
        }
        在最后插入数据();
      }
      else 修改选中行数据();

      状态切换(false);
    }

    // 权重只能输入数字
    private void WeightTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (!Regex.IsMatch(e.Text, "^[0-9]+$"))
        e.Handled = true;
    }

    // 权重只能输入数字
    private void WeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      weightTextBox.Text = Regex.Replace(weightTextBox.Text, "[^0-9]", "");
    }

    // 从剪切板接收数字
    private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
    {
      if (e.DataObject.GetDataPresent(typeof(String)))
      {
        string text = (string)e.DataObject.GetData(typeof(String));
        if (!Regex.IsMatch(text, "^[0-9]+$"))
        {
          e.CancelCommand();
        }
      }
      else
      {
        e.CancelCommand();
      }
    }





    private bool 是否有相同数据(string key, string value)
    {
      using SQLiteConnection connection = new($"Data Source={dbPath};Version=3;");
      connection.Open();
      string query = $"SELECT EXISTS(SELECT 1 FROM {tableName} WHERE key = @key AND value = @value)";
      using SQLiteCommand command = new(query, connection);
      command.Parameters.AddWithValue("@key", key);
      command.Parameters.AddWithValue("@value", value);
      var result = command.ExecuteScalar();
      return Convert.ToInt32(result) == 1;
    }




    private void 在最后插入数据()
    {
      if(keyTextBox.Text == "" || valueTextBox.Text == "") return;

      ListViewDataItem lastItem = new();
      if (listView.Items.Count - 1 >= 0)
        lastItem = ListViewData[listView.Items.Count - 1];

      ListViewDataItem newItem = new()
      {
        Num    = lastItem.Num + 1,
        Key    = keyTextBox.Text,
        Value  = valueTextBox.Text,
        Weight = weightTextBox.Text == "" ? 0 : int.Parse(weightTextBox.Text),
        FC     = fcTextBox.Text,
        IsAdd  = true
      };

      ListViewData.Add(newItem);

      listView.ScrollIntoView(lastItem);

      状态切换(false);
    }


    private void 修改选中行数据()
    {
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        item.Key    = keyTextBox.Text;
        item.Value  = valueTextBox.Text;
        item.Weight = weightTextBox.Text == "" ? 0 : int.Parse(weightTextBox.Text);
        item.FC     = fcTextBox.Text;
        item.IsMod  = true;
      }
      状态切换(false);
    }

    // 切换 添加或修改
    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
      var cb = (CheckBox)sender;
      if (cb.Name == "AddOrModCheckBox")
        add_mod_button.Content = cb.IsChecked == true ? "添加" : "修改";
      if (cb.Name == "KeyOrValueCheckBox")
        textBox2.Tag = cb.IsChecked == true ? "搜索词条" : "搜索编码";
    }













    // 提交
    private void Submit_button_Click(object sender, RoutedEventArgs e)
    {
      if (editingStatus != true) return;
      var result = MessageBox.Show($"要提交所有变更的数据吗？", "提交数据",
        MessageBoxButton.OKCancel, MessageBoxImage.Question);

      if (result != MessageBoxResult.OK) return;

      // 提交数据();

      状态切换(true);
    }

    // 列表双击事件
    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var listView = sender as ListView;
      if (listView.SelectedItem != null)
      {
        var item = listView.SelectedItem as ListViewDataItem;
        keyTextBox.Text = item.Key;
        valueTextBox.Text = item.Value;
        weightTextBox.Text = item.Weight.ToString();
        fcTextBox.Text = item.FC;
      }
    }


  }
}
