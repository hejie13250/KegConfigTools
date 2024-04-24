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
using System.Windows.Media;
using 小科狗配置.Class;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using GroupBox = System.Windows.Controls.GroupBox;
using ListView = System.Windows.Controls.ListView;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;


namespace 小科狗配置.Page
{
  /// <summary>
  /// ModifyTable.xaml 的交互逻辑
  /// </summary>
  public partial class ModifyTable
  {

    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion

    public sealed class ListViewDataItem : INotifyPropertyChanged
    {
      private int    _rowNumber;
      private string _key;
      private string _value;
      private int?   _weight;
      private string _fc;
      private bool   _isDel;
      private bool   _isMod;
      private bool   _isAdd;
      public int RowNumber 
      {
        get => _rowNumber;
        set => SetProperty(ref _rowNumber, value);
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

      public int Weight
      {
        get
        {
          if (_weight != null) return (int)_weight;
          return 0;
        }
        set => SetProperty(ref _weight, value);
      }

      public string Fc
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

      private void OnPropertyChanged([CallerMemberName] string propertyName = null)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
      {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return;
        storage = value;
        OnPropertyChanged(propertyName);
      }
    }

    private ObservableCollection<ListViewDataItem> ListViewData { get; set; }

    private ObservableCollection<ListViewDataItem> 要删除项 { get; set; }
    private ObservableCollection<ListViewDataItem> 要添加项 { get; set; }
    private ObservableCollection<ListViewDataItem> 要修改项 { get; set; }
    private ObservableCollection<ListViewDataItem> 修改项值 { get; set; }

    private string _dbPath;        // Keg.db
    private string _dbPath2;       // 其它db
    private string _tableName;     // 表名称
    private int    _pageCount;     // 总页码
    private int    _currentPage;   // 当前页码
    private int    _pageLen;       // 每页数据行数
    private bool   _editingStatus; // 编缉状态

    public ModifyTable()
    {
      _dbPath = Base.KegPath + "Keg.db";
      InitializeComponent();
      读取系统字体列表();
      从数据库文件获取表名();
      要删除项 = new ObservableCollection<ListViewDataItem>();
      要添加项 = new ObservableCollection<ListViewDataItem>();
      要修改项 = new ObservableCollection<ListViewDataItem>();
      修改项值 = new ObservableCollection<ListViewDataItem>();
    }

    // 切换 添加或修改
    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
      var cb = (CheckBox)sender;
      switch (cb.Name)
      {
        case "dbCheckBox":  // 切换库
          open_Button.IsEnabled = cb.IsChecked != false;
          comboBox.Items.Clear();
          if (dbCheckBox.IsChecked != null && (bool)dbCheckBox.IsChecked)
          {
            if (_dbPath2 == null) return;
            _dbPath = _dbPath2;
            从数据库文件获取表名();
          }
          else
          {
            _dbPath = Base.KegPath + "Keg.db";
            从数据库文件获取表名();
          }
          break;
        case "addOrModCheckBox":    // 切换 添加 或 修改
          add_Mod_Button.Content = cb.IsChecked == true ? "添加" : "修改";
          break;
        case "keyOrValueCheckBox":  // 切换搜索项
          textBox2.Tag = cb.IsChecked == true ? "搜索词条" : "搜索编码";
          break;
      }
    }
    // 打开其它数据库
    private void Open_Button_Click(object sender, RoutedEventArgs e)
    {
      var openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = @"SQLite文件|*.db";
      openFileDialog.Title  = @"选择一个SQLite文件";

      if (openFileDialog.ShowDialog() != DialogResult.OK) return;
      var filePath = openFileDialog.FileName;

      Console.WriteLine($@"文件路径：{filePath}");
      _dbPath2 = filePath;
      _dbPath = filePath;
      comboBox.Items.Clear();
      // ListViewData.Clear();
      从数据库文件获取表名();
    }


    /// <summary>
    /// 从 db 读取表名到 ComboBox
    /// </summary>
    private void 从数据库文件获取表名()
    {
      SQLiteConnection connection = new ($"Data Source={_dbPath}");
      connection.Open();
      try
      {
        using var command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
          var labelName = reader.GetString(0);
          comboBox.Items.Add(labelName);
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
    private void 删除表内所有空行()
    {
      SQLiteConnection connection = new($"Data Source={_dbPath}");
      connection.Open();
      try
      {
        using var command = new SQLiteCommand($"DELETE FROM '{_tableName}' WHERE key IS NULL OR TRIM(key) = ''", connection);
        var rows = command.ExecuteNonQuery();
        if(rows != 0 ) MessageBox.Show($"已删除 {rows} 行空数据");
      }
      catch (Exception ex)
      {
        MessageBox.Show($"获取表名出错: {ex.Message}");
      }
      finally
      {
        connection.Close();
      }
    }


    /// <summary>
    /// 获取表内词条总数
    /// </summary>
    /// <param name="tableName">表名称</param>
    /// <returns></returns>
    private int 获取表数据总行数(string tableName)
    {
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      var query = $"SELECT COUNT(*) FROM '{tableName}'";
      connection.Open();
      using var command = new SQLiteCommand(query, connection);
      var rowCount = Convert.ToInt32(command.ExecuteScalar());
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
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      var query = $"SELECT ROW_NUMBER() OVER (ORDER BY ROWID) AS RowNumber, * FROM '{_tableName}' LIMIT {limit} OFFSET {offset}";
      connection.Open();
      using SQLiteDataAdapter adapter   = new(query, connection);
      DataTable               dataTable = new();
      adapter.Fill(dataTable);
      return dataTable;
    }












    /// <summary>
    /// 将数据填充到 ListView
    /// </summary>
    /// <param name="dataTable"></param>
    private void 数据填充到列表(DataTable dataTable)
    {
      ListViewData = new ObservableCollection<ListViewDataItem>();

      foreach (DataRow row in dataTable.Rows)
      {
        ListViewData.Add(new ListViewDataItem
        {
          // Number = number++,
          RowNumber =row["RowNumber"] != DBNull.Value ? int.Parse(row["RowNumber"].ToString()) : 0,
          Key    = row["Key"]    != DBNull.Value ? row["Key"].ToString() : string.Empty,
          Value  = row["Value"]  != DBNull.Value ? row["Value"].ToString() : string.Empty,
          Weight = row["Weight"] != DBNull.Value ? int.Parse(row["Weight"].ToString()) : 0,
          Fc     = row["FC"]     != DBNull.Value ? row["FC"].ToString() : string.Empty
        });
      }

      listView.ItemsSource = ListViewData;
    }







    // 切换表
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      _tableName = comboBox.SelectedValue as string;
      删除表内所有空行();         // 先删除空行

      var count = 获取表数据总行数(_tableName);
      _pageLen = 5000;
      _pageCount = count / _pageLen;
      if (count % _pageLen > 0) _pageCount++;

      textBox1.Text = $"词条数：{count}";
      _currentPage = 0;
      textBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
      var fontName = 从表里获取字体名称(_tableName);
      if (fontName != null)
        for (var i = 0; i < fontComboBox.Items.Count; i++)
          if (fontComboBox.Items[i].ToString() == fontName)
            fontComboBox.SelectedIndex = i;

      fontComboBox.IsEnabled = true;  //读取词条后才可以改字体
      stackPanel.IsEnabled   = true;  //读取词条后才可以翻页和搜索
    }

    // 改 ListView 的字体
    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (fontComboBox.SelectedValue == null) return;

      var fontName = fontComboBox.SelectedItem.ToString();
      var font        = new FontFamily(fontName);
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
    private void 读取系统字体列表()
    {
      foreach (var font in System.Drawing.FontFamily.Families)
      {
        if (排除字体名称里没有中文的字体(font.Name))
          fontComboBox.Items.Add(font.Name);
      }
    }

    /// <summary>
    /// 正则排除 字体名称 里没有中文的字体
    /// </summary>
    /// <param name="fontName"></param>
    /// <returns></returns>
    private bool 排除字体名称里没有中文的字体(string fontName)
    {
      Regex chineseRegex = new(@"[\u4e00-\u9fff]");

      return chineseRegex.IsMatch(fontName);
    }

    // ComboBox 获取焦点
    private void ComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      var cb = sender as ComboBox;
      cb?.Focus();
    }


    /// <summary>
    /// 从指定表 tableName 内读取 key 列为""时 value 的值
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    private string 从表里获取字体名称(string tableName)
    {
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      var query = $"SELECT value FROM '{tableName}' WHERE key = '字体'";
      connection.Open();
      using SQLiteCommand command = new(query, connection);

      string result;
      using (var reader = command.ExecuteReader())
      {
        result = reader.Read() ? reader["value"].ToString() : null;
      }
      connection.Close();
      return result;
    }

    /// <summary>
    /// 保存字体名称到指定表
    /// </summary>
    /// <param name="fontName"></param>
    private void WriteFontToTable(string fontName)
    {
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      connection.Open();
      // 检查是否已存在该键，如果存在则更新，否则插入新记录
      var checkQuery = $"SELECT COUNT(*) FROM '{_tableName}' WHERE key = '字体'";
      using SQLiteCommand checkCommand = new(checkQuery, connection);
      var count = (long)checkCommand.ExecuteScalar();

      if (count > 0)
      {
        // 更新已存在的记录
        var updateQuery = $"UPDATE '{_tableName}' SET value = @font WHERE key = '字体'";
        using SQLiteCommand updateCommand = new(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("@font", fontName);
        updateCommand.ExecuteNonQuery();
      }
      else
      {
        // 插入新记录
        var insertQuery = $"INSERT INTO '{_tableName}' (key, value) VALUES ('字体', @font)";
        using SQLiteCommand insertCommand = new(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@font", fontName);
        insertCommand.ExecuteNonQuery();
      }

      connection.Close();
    }


    // 第一页
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      if (_currentPage == 0 || textBox2.Text != "") return;
      if (_editingStatus){
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      _currentPage = 0;
      textBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }


    // 上一页
    private void Button1_Click(object sender, RoutedEventArgs e)
    {
      if (_currentPage == 0 || textBox2.Text != "") return;
      if (_editingStatus)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      _currentPage --;
      textBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 鼠标滚轮翻页
    private void TextBox_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      e.Handled = true;                                         // 阻止滚轮事件继续向上冒泡
      if (textBox2.Text != "") return;
      if (_editingStatus)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      var step = Keyboard.Modifiers switch
      {
        ModifierKeys.Control => 5 // 如果按住Ctrl键，步长为5
       ,
        ModifierKeys.Shift => 10 // 如果按住Shift键，步长为10
       ,
        _ => 1
      };

      var value = _currentPage + 1;

      switch (e.Delta)
      {
        case > 0 when value + step <= _pageCount: // 滚动向上
          value += step;
          break;
        case < 0 when value - step >= 1:         // 滚动向下
          value          -= step;
          break;
      }

      _currentPage  = value -1;
      textBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 下一页
    private void Button2_Click(object sender, RoutedEventArgs e)
    {
      if (_currentPage == _pageCount - 1 || textBox2.Text != "") return;
      if (_editingStatus)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      _currentPage ++;
      textBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 最后页
    private void Button3_Click(object sender, RoutedEventArgs e)
    {
      if (_currentPage == _pageCount - 1 || textBox2.Text != "") return;
      if (_editingStatus)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      _currentPage = _pageCount - 1;
      textBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 刷新
    private void Button4_Click(object sender, RoutedEventArgs e)
    {
      if (_editingStatus != true) return;
      var result = MessageBox.Show("当前有数据没有提交，确定后将取消未提交的操作！", "刷新",
        MessageBoxButton.OKCancel, MessageBoxImage.Question);

      if (result != MessageBoxResult.OK) return;

      if (textBox2.Text != "")
      {
        var dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
        数据填充到列表(dataTable);
      }
      else 搜索();

      状态切换(true);
    }


    private void Button5_Click(object sender, RoutedEventArgs e)
    {
      if (listView.Items.Count - 1 >= 0)
        listView.ScrollIntoView(ListViewData[0]);
    }

    private void Button6_Click(object sender, RoutedEventArgs e)
    {
      if (listView.Items.Count - 1 >= 0)
        listView.ScrollIntoView(ListViewData[listView.Items.Count - 1]);
    }

    // 搜索
    // private void Button5_Click(RoutedEventArgs e)
    // {
    //   if (_editingStatus)
    //   {
    //     MessageBox.Show("当前有数据没有提交。");
    //     return;
    //   }
    //   if (comboBox.SelectedIndex < 0) return;
    //
    //   搜索();
    // }
    //



    // 搜索
    private void TextBox2_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (comboBox.SelectedIndex < 0) return;
      搜索();
    }

    private void 搜索()
    {
      DataTable dataTable = new();
      if (textBox2.Text == "")
      {
        dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
        数据填充到列表(dataTable);
        var count = 获取表数据总行数(_tableName);
        textBox1.Text = $"词条数：{count}";
        textBox.Text  = $"{_currentPage + 1}/{_pageCount}";
        return;
      }

      var name = keyOrValueCheckBox.IsChecked == true ? "value" : "key";
      var str = textBox2.Text;

      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      var query = $"SELECT ROW_NUMBER() OVER (ORDER BY ROWID) AS RowNumber, * FROM '{_tableName}' WHERE {name} LIKE @str";
      connection.Open();

      using SQLiteCommand command = new(query, connection);
      command.Parameters.AddWithValue("@str", $"{str}%");

      using var reader = command.ExecuteReader();
      dataTable.Load(reader);
      textBox1.Text = $"词条数：{dataTable.Rows.Count}";
      textBox.Text  = "1/1";
      数据填充到列表(dataTable);
    }


    /// <summary>
    /// 编缉状态切换
    /// </summary>
    /// <param name="b"></param>
    private void 状态切换(bool b)
    {
      _editingStatus          = b;
      comboBox    .IsEnabled  = b;
      fontComboBox.IsEnabled  = b;
      textBox2    .IsReadOnly = b;
    }




    // 标记删除选中项
    private void Del_button_Click(object sender, RoutedEventArgs e)
    {
      if (listView.SelectedItems.Count == 0) return;
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        if(item.IsMod || item.IsAdd) return;
        item.IsDel = true;
        要删除项.Add(item);
      }
      状态切换(false);
    }

    // 添加或修改
    private void AddOrMod_button_Click(object sender, RoutedEventArgs e)
    {
      if (listView.Items.Count == 0) return;

      if (addOrModCheckBox.IsChecked != null && (bool)addOrModCheckBox.IsChecked)
      {
        if (_currentPage != _pageCount - 1)
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
      if (e.DataObject.GetDataPresent(typeof(string)))
      {
        var text = (string)e.DataObject.GetData(typeof(string));
        if (text != null && !Regex.IsMatch(text, "^[0-9]+$"))
          e.CancelCommand();
      }
      else
        e.CancelCommand();
    }





    private bool 是否有相同数据(string key, string value)
    {
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      connection.Open();
      var query = $"SELECT EXISTS(SELECT 1 FROM {_tableName} WHERE key = @key AND value = @value)";
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
        RowNumber = lastItem.RowNumber + 1,
        Key       = keyTextBox.Text,
        Value     = valueTextBox.Text,
        Weight    = weightTextBox.Text == "" ? 0 : int.Parse(weightTextBox.Text),
        Fc        = fcTextBox.Text,
        IsAdd     = true
      };

      ListViewData.Add(newItem);
      要添加项.Add(newItem);
      listView.ScrollIntoView(lastItem);

      状态切换(false);
    }


    private void 修改选中行数据()
    {
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        要修改项.Add(item);
        item.Key    = keyTextBox.Text;
        item.Value  = valueTextBox.Text;
        item.Weight = weightTextBox.Text == "" ? 0 : int.Parse(weightTextBox.Text);
        item.Fc     = fcTextBox.Text;
        item.IsMod  = true;
        修改项值.Add(item);
      }
      状态切换(false);
    }



    // 列表双击事件
    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var view = sender as ListView;
      if (view?.SelectedItem is not ListViewDataItem item) return;
      if(item.IsDel || item.IsAdd) return;
      keyTextBox.Text    = item.Key;
      valueTextBox.Text  = item.Value;
      weightTextBox.Text = item.Weight.ToString();
      fcTextBox.Text     = item.Fc;
    }





    // 提交
    private void Submit_button_Click(object sender, RoutedEventArgs e)
    {
      if (_editingStatus != true) return;
      var result = MessageBox.Show("要提交所有变更的数据吗？", "提交数据",
        MessageBoxButton.OKCancel, MessageBoxImage.Question);

      if (result != MessageBoxResult.OK) return;

      提交数据();
      MessageBox.Show("数据提交完成！");
      状态切换(true);

      要删除项.Clear();
      要添加项.Clear();
      要修改项.Clear();
      修改项值.Clear();

      var count = 获取表数据总行数(_tableName);
      _pageCount    = count / _pageLen;
      textBox1.Text = $"词条数：{count}";
      textBox.Text  = $"{_currentPage}/{_pageCount}";

      var dataTable = GetTopRecordsFromTable(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }



    private void 提交数据()
    {
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      connection.Open();
      if (要删除项.Count != 0)
      {
        foreach (var item in 要删除项)
        {
          删除数据行(connection, item.Key, item.Value);
        }
      }

      if (要添加项.Count != 0)
      {
        foreach (var item in 要添加项)
        {
          添加数据行(connection, item.Key, item.Value, item.Weight, item.Fc);
        }
      }

      if (修改项值.Count != 0)
      {
        var index = 0;
        foreach (var item in 修改项值)
        {
          修改数据行(connection, item.Key, item.Value, item.Weight, item.Fc, 要修改项[index].Key, 要修改项[index].Value);
          index++;
        }
      }
      connection.Close();
    }









    private void 删除数据行(SQLiteConnection con, string key, string value)
    {
      var command = new SQLiteCommand(con);
      command.CommandText = $"DELETE FROM {_tableName} WHERE key = @key AND value = @value";
      command.Parameters.AddWithValue("@key",   key);
      command.Parameters.AddWithValue("@value", value);

      command.Prepare();
      command.ExecuteNonQuery();
    }


    private void 添加数据行(SQLiteConnection con, string key, string value, int weight, string fc)
    {
      var command = new SQLiteCommand(con);
      command.CommandText     = $"INSERT INTO {_tableName}(key, value, weight, fc) VALUES(@key,@value,@weight,@fc)";
      command.Parameters.AddWithValue("@key",    key);
      command.Parameters.AddWithValue("@value",  value);
      command.Parameters.AddWithValue("@weight", weight);
      command.Parameters.AddWithValue("@fc",     fc);

      command.Prepare();
      command.ExecuteNonQuery();
    }

    private void 修改数据行(SQLiteConnection con, string key, string value, int weight, string fc, string oldKey, string oldValue)
    {
      var command = new SQLiteCommand(con);
      command.CommandText = $"UPDATE {_tableName} SET key=@key, value=@value, weight=@weight, fc=@fc WHERE key=@oldKey AND value=@oldValue";
      command.Parameters.AddWithValue("@key",      key);
      command.Parameters.AddWithValue("@value",    value);
      command.Parameters.AddWithValue("@weight",   weight);
      command.Parameters.AddWithValue("@fc",       fc);
      command.Parameters.AddWithValue("@oldKey",   oldKey);
      command.Parameters.AddWithValue("@oldValue", oldValue);

      command.Prepare();
      command.ExecuteNonQuery();
    }























  }
}
