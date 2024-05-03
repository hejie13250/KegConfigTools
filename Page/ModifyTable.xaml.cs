using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using 小科狗配置.Class;
using 小科狗配置.DialogBox;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using GroupBox = System.Windows.Controls.GroupBox;
using ListView = System.Windows.Controls.ListView;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.IO;
using System.Text;

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

    #region 定义和初始化
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
      
      // 自定义的克隆方法
      public ListViewDataItem Clone()
      {
        return new ListViewDataItem
        {
          RowNumber = RowNumber,
          Key       = Key,
          Value     = Value,
          Weight    = Weight,
          Fc        = Fc,
          IsMod     = IsMod
        };
      }
      
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

    private string _dbPath;      // 当前库路径
    private string _tableName;   // 当前表名称
    private string _otherDbPath; // 其它库路径
    private string _tableName1;  // 主库的表名称
    private string _tableName2;  // 其它库的表名称
    private int    _pageCount;   // 总页码
    private int    _currentPage; // 当前页码
    private int    _pageLen;     // 每页数据行数
    private bool   _onlyReading; // 编缉状态

    public ModifyTable()
    {
      _dbPath = Base.SQLiteDB_Path;
      // MessageBox.Show(_dbPath);
      InitializeComponent();
      读取系统字体列表();
      从数据库文件获取表名();
      要删除项 = new ObservableCollection<ListViewDataItem>();
      要添加项 = new ObservableCollection<ListViewDataItem>();
      要修改项 = new ObservableCollection<ListViewDataItem>();
      修改项值 = new ObservableCollection<ListViewDataItem>();
    }
    #endregion

    #region SQLite码表修改

    #region 控件事件

    #region 库切换控件事件

    private void 事件_切换库按钮_Click(object sender, RoutedEventArgs e)
    {
      var cb = (CheckBox)sender;
      open_Button.IsEnabled = cb.IsChecked != false;
      ListViewData?.Clear();
      comboBox.Items.Clear();
      fontComboBox.IsEnabled = false;
      stackPanel_2.IsEnabled = false;
      searchTextBox.Text     = "";

      if (dbCheckBox.IsChecked != null && (bool)dbCheckBox.IsChecked)
      {
        if (_otherDbPath == null) // 还没有选择其它库
        {
          stackPanel_2.IsEnabled = false;
          open_Button.IsEnabled  = cb.IsChecked != false;
          return;
        }
        _dbPath = _otherDbPath;  // 如果有其它库就打开它

        if (_tableName2 == null) return;
        _tableName = _tableName2;
      }
      else
      {
        _dbPath = Base.SQLiteDB_Path;
        if (_tableName1 != null)
          _tableName = _tableName1;
      }

      从数据库文件获取表名();
      for (var i = 0; i < comboBox.Items.Count; i++)
        if ((string)comboBox.Items[i] == _tableName)
          comboBox.SelectedIndex = i;
    }

    // 打开其它数据库
    private void 事件_打开按钮_Click(object sender, RoutedEventArgs e)
    {
      var openFileDialog = new OpenFileDialog
      {
        Filter = @"SQLite文件|*.db",
        Title  = @"选择一个SQLite文件"
      };

      if (openFileDialog.ShowDialog() != DialogResult.OK) return;
      var filePath = openFileDialog.FileName;

      Console.WriteLine($@"文件路径：{filePath}");
      _otherDbPath = filePath;
      _dbPath      = filePath;
      comboBox.Items.Clear();
      从数据库文件获取表名();
    }

    // 切换表
    private void 事件_表组合框_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (sender is not ComboBox cb || cb.SelectedIndex == -1) return;
      _tableName = cb.SelectedValue as string;
      searchTextBox.Text = "";
      if (dbCheckBox.IsChecked != null && (bool)dbCheckBox.IsChecked) 
        _tableName2 = _tableName;
      else
        _tableName1 = _tableName;
      删除表内所有空行并改空权重为零();         // 先删除空行
      获取数据();
      stackPanel_2.IsEnabled    = true;
      fontComboBox.IsEnabled = true; //读取词条后才可以改字体
      stackPanel_3.IsEnabled   = true; //读取词条后才可以翻页和搜索
    }

    // 改 ListView 的字体
    private void 事件_字体组合框_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (fontComboBox.SelectedValue == null) return;

      var fontName = fontComboBox.SelectedItem.ToString();
      var font     = new FontFamily(fontName);
      listView.FontFamily      = font;
      keyTextBox.FontFamily    = font;
      valueTextBox.FontFamily  = font;
      weightTextBox.FontFamily = font;
      fcTextBox.FontFamily     = font;

      将字体名称写到表内(fontName);
    }

    // ComboBox 获取焦点
    private void 组合框_MouseEnter(object sender, MouseEventArgs e)
    {
      var cb = sender as ComboBox;
      cb?.Focus();
    }

    #endregion

    #region 复制_删除_导入_导出表事件
    private void 事件_复制表按钮_Click(object sender, RoutedEventArgs e)
    {
      if(comboBox.SelectedIndex == -1) return;
      var dialog = new ReNameDialogBox("复制表到新表", _tableName);
      dialog.ShowDialog();
      var newTableName = dialog.Name;
      if(_tableName == newTableName) return;
      if (comboBox.Items.Cast<object>().Any(item => item.ToString() == newTableName))
      {
        MessageBox.Show("存在同名称的表，请改名称");
        return;
      }

      复制表(newTableName); // 复制表
      comboBox.Items.Add(newTableName);

      // 更新当前表名
      _tableName = newTableName;
    }

    private void 事件_删除表按钮_Click(object sender, RoutedEventArgs e)
    {
      if(comboBox.SelectedIndex == -1) return;
      if (_onlyReading)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      if (comboBox.Items.Count == 1)
      {
        MessageBox.Show("当前库只有一个表了,你不能删除它。");
        return;
      }
      var result = MessageBox.Show($"要删除 '{_tableName}' 表吗？", "删除表",
                                   MessageBoxButton.OKCancel, MessageBoxImage.Question);

      if (result != MessageBoxResult.OK) return;
      if(!删除表(_tableName)) return; // 删除表
      ListViewData?.Clear();
      comboBox.Items.Clear();
      从数据库文件获取表名();
    }

    private void 事件_导入表按钮_Click(object sender, RoutedEventArgs e)
    {
      // if(comboBox.SelectedIndex == -1) return;
      var dialog = new ReNameDialogBox("导入 CSV 文件到新表", "");
      dialog.ShowDialog();
      var newTableName = dialog.Name;
      if(string.IsNullOrEmpty(newTableName)) return;

      // 使用文件对话框获取要导入的CSV文件路径
      var csvFilePath = GetCsvFilePathToImport();

      // 创建一个新表
      CreateNewTable(_dbPath, newTableName);

      // 导入CSV文件数据到新表
      if (!string.IsNullOrEmpty(csvFilePath))
        ImportCsvToTable(_dbPath, newTableName, csvFilePath);
    }

    private void 事件_导出表按钮_Click(object sender, RoutedEventArgs e)
    {
      if(comboBox.SelectedIndex == -1) return;
      var csvFilePath = GetCsvFilePathToExport();// 弹出文件对话框获取将要导出的 CSV 文件路径

      if (!string.IsNullOrEmpty(csvFilePath))
        ExportTableToCsv(_dbPath, _tableName, csvFilePath);
    }

    #endregion

    #region 翻页控件事件
    // 第一页
    private void 事件_第一页按钮_Click(object sender, RoutedEventArgs e)
    {
      if (_currentPage == 0 || searchTextBox.Text != "") return;
      if (_onlyReading)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      _currentPage = 0;
      pageTextBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 上一页
    private void 事件_上一页按钮_Click(object sender, RoutedEventArgs e)
    {
      if (_currentPage == 0 || searchTextBox.Text != "") return;
      if (_onlyReading)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      _currentPage --;
      pageTextBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 鼠标滚轮翻页
    private void 事件_页码框_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      e.Handled = true;                                         // 阻止滚轮事件继续向上冒泡
      if (searchTextBox.Text != "") return;
      if (_onlyReading)
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
      pageTextBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 下一页
    private void 事件_下一页按钮_Click(object sender, RoutedEventArgs e)
    {
      if (_currentPage == _pageCount - 1 || searchTextBox.Text != "") return;
      if (_onlyReading)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      _currentPage ++;
      pageTextBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 最后页
    private void 事件_最后页按钮_Click(object sender, RoutedEventArgs e)
    {
      if (_currentPage == _pageCount - 1 || searchTextBox.Text != "") return;
      if (_onlyReading)
      {
        MessageBox.Show("当前有数据没有提交。");
        return;
      }

      _currentPage = _pageCount - 1;
      pageTextBox.Text = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    // 刷新
    private void 事件_刷新按钮_Click(object sender, RoutedEventArgs e)
    {
      if (_onlyReading != true) return;
      var result = MessageBox.Show("当前有数据没有提交，确定后将取消未提交的操作！", "刷新",
        MessageBoxButton.OKCancel, MessageBoxImage.Question);

      if (result != MessageBoxResult.OK) return;

      if (searchTextBox.Text != "")
      {
        var dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
        数据填充到列表(dataTable);
      }
      else 搜索();

      要删除项.Clear();
      要添加项.Clear();
      要修改项.Clear();
      修改项值.Clear();
      切换编缉状态(false);
    }

    private void 事件_撤消按钮_Click(object sender, RoutedEventArgs e)
    {
      if (listView.SelectedItems.Count == 0)
      {
        MessageBox.Show("没有选中任何标记项");
        return;
      }
      // 撤消添加项
      var itemsToRemove = listView.SelectedItems.Cast<ListViewDataItem>().Where(item => item.IsAdd).Cast<object>().ToList();
      foreach (ListViewDataItem item in itemsToRemove)
        ListViewData.Remove(item);

      // 撤消删除项
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        if (!item.IsDel) continue;
        要删除项.Remove(item);
        item.IsDel = false;
      }

      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        if (!item.IsMod) continue;
        修改项值.Remove(item);
        foreach (var item2 in 要修改项)
        {
          if (item.RowNumber != item2.RowNumber) continue;
          item.IsMod  = item2.IsMod;
          item.Key    = item2.Key;
          item.Value  = item2.Value;
          item.Weight = item2.Weight;
          item.Fc     = item2.Fc;
          itemsToRemove.Add(item2);
        }
      }
      foreach (ListViewDataItem item in itemsToRemove)
        要修改项.Remove(item);

      if(要删除项.Count ==0 && 要添加项.Count == 0 && 要修改项.Count == 0)
        切换编缉状态(false);
    }


    private void 事件_到首行按钮_Click(object sender, RoutedEventArgs e)
    {
      if (listView.Items.Count - 1 >= 0)
        listView.ScrollIntoView(ListViewData[0]);
    }

    private void 事件_到尾行按钮_Click(object sender, RoutedEventArgs e)
    {
      if (listView.Items.Count - 1 >= 0)
        listView.ScrollIntoView(ListViewData[listView.Items.Count - 1]);
    }

    #endregion

    #region 增删改查事件

    // 标记删除选中项
    private void 事件_标记删除按钮_Click(object sender, RoutedEventArgs e)
    {
      if (listView.SelectedItems.Count == 0) return;
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        if(item.IsMod || item.IsAdd) return;
        item.IsDel = true;
        要删除项.Add(item);
      }
      切换编缉状态(true);
    }

    // 添加或修改
    private void 事件_添加或修改按钮_Click(object sender, RoutedEventArgs e)
    {
      if (listView.Items.Count == 0 || addOrModCheckBox.IsChecked == null) return;

      if ((bool)addOrModCheckBox.IsChecked && _currentPage != _pageCount - 1)
      {
        MessageBox.Show("添加词条请转到最后一页。");
        return;
      }

      if (是否有相同数据(keyTextBox.Text, valueTextBox.Text))
      {
        MessageBox.Show("存在相同编码和词条的数据。");
        return;
      }

      if ((bool)addOrModCheckBox.IsChecked)
        在最后插入数据();
      else 修改选中行数据();

      切换编缉状态(true);
    }

    private void 事件_列表_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      var view = sender as ListView;
      if (view?.SelectedItem is not ListViewDataItem item) return;
      if(item.IsDel || item.IsAdd) return;
      keyTextBox.Text    = item.Key;
      valueTextBox.Text  = item.Value;
      weightTextBox.Text = item.Weight.ToString();
      fcTextBox.Text     = item.Fc;
    }

    // 切换 添加或修改
    private void 事件_切换按钮_Click(object sender, RoutedEventArgs e)
    {
      var cb = (CheckBox)sender;
      switch (cb.Name)
      {
        case "addOrModCheckBox":    // 切换 添加 或 修改
          add_Mod_Button.Content = cb.IsChecked == true ? "添加" : "修改";
          break;
        case "keyOrValueCheckBox":  // 切换搜索项
          searchTextBox.Tag  = cb.IsChecked == true ? "搜索词条" : "搜索编码";
          searchTextBox.Text = "";
          searchTextBox.Focus();
          break;
      }
    }

    // 权重只能输入数字
    private void 事件_权重框_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (!Regex.IsMatch(e.Text, "^[0-9]+$"))
        e.Handled = true;
    }

    // 权重只能输入数字
    private void 事件_权重框_TextChanged(object sender, TextChangedEventArgs e)
    {
      weightTextBox.Text = Regex.Replace(weightTextBox.Text, "[^0-9]", "");
    }

    // 从剪切板接收数字
    private void 事件_权重框_Pasting(object sender, DataObjectPastingEventArgs e)
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

    private void 事件_搜索框_TextChanged(object sender, TextChangedEventArgs e)
    {
      搜索();
    }

    // 提交
    private void 事件_提交按钮_Click(object sender, RoutedEventArgs e)
    {
      var result = MessageBox.Show("要提交所有变更的数据吗？", "提交数据",
                                   MessageBoxButton.OKCancel, MessageBoxImage.Question);

      if (result != MessageBoxResult.OK) return;
      提交数据();

      切换编缉状态(false);
    }

    #endregion

    #endregion 控件事件

    #region 数据库操作

    /// <summary>
    /// 删除表内所有 key 为 null 和 "" 的行
    /// </summary>
    private void 删除表内所有空行并改空权重为零()
    {
      using var connection = new SQLiteConnection($"Data Source={_dbPath}");
      connection.Open();
      try
      {
        // 删除空行
        int deletedRows;
        using (var deleteCommand = new SQLiteCommand($"DELETE FROM '{_tableName}' WHERE key IS NULL OR TRIM(key) = ''", connection))
          deletedRows = deleteCommand.ExecuteNonQuery();

        // 改空权重为零
        int updatedRows;
        using (var updateCommand = new SQLiteCommand($"UPDATE '{_tableName}' SET weight = 0 WHERE weight IS NULL OR TRIM(weight) = ''", connection))
          updatedRows = updateCommand.ExecuteNonQuery();

        if (deletedRows != 0) MessageBox.Show($"已删除 {deletedRows} 行空数据");
        if (updatedRows != 0) MessageBox.Show($"已将 {updatedRows} 行权重为空的数据改权重为 0");
      }
      catch (Exception ex)
      {
        MessageBox.Show($"操作出错: {ex.Message}");
      }
      finally
      {
        connection.Close();
      }
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
    private DataTable 从表内指定行开始获取指定行数的数据(int offset, int limit)
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
          RowNumber =row["RowNumber"] != DBNull.Value ? int.Parse(row["RowNumber"].ToString()) : 0,
          Key    = row["Key"]    != DBNull.Value ? row["Key"].ToString() : string.Empty,
          Value  = row["Value"]  != DBNull.Value ? row["Value"].ToString() : string.Empty,
          Weight = row["Weight"] != DBNull.Value ? int.Parse(row["Weight"].ToString()) : 0,
          Fc     = row["FC"]     != DBNull.Value ? row["FC"].ToString() : string.Empty
        });
      }

      listView.ItemsSource = ListViewData;
    }

    /// <summary>
    /// 从指定表 tableName 内读取 key 列为""时 value 的值
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    private string 从表里获取字体名称(string tableName)
    {
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      var                    query      = $"SELECT value FROM '{tableName}' WHERE key = '字体'";
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
    private void 将字体名称写到表内(string fontName)
    {
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      connection.Open();
      // 检查是否已存在该键，如果存在则更新，否则插入新记录
      var                 checkQuery   = $"SELECT COUNT(*) FROM '{_tableName}' WHERE key = '字体'";
      using SQLiteCommand checkCommand = new(checkQuery, connection);
      var                 count        = (long)checkCommand.ExecuteScalar();

      if (count > 0)
      {
        // 更新已存在的记录
        var                 updateQuery   = $"UPDATE '{_tableName}' SET value = @font WHERE key = '字体'";
        using SQLiteCommand updateCommand = new(updateQuery, connection);
        updateCommand.Parameters.AddWithValue("@font", fontName);
        updateCommand.ExecuteNonQuery();
      }
      else
      {
        // 插入新记录
        var                 insertQuery   = $"INSERT INTO '{_tableName}' (key, value) VALUES ('字体', @font)";
        using SQLiteCommand insertCommand = new(insertQuery, connection);
        insertCommand.Parameters.AddWithValue("@font", fontName);
        insertCommand.ExecuteNonQuery();
      }

      connection.Close();
    }
    
    private void 提交数据()
    {
      try
      {
        using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
        connection.Open();
        // 开始一个事务
        using var transaction = connection.BeginTransaction();

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
        transaction.Commit();  // 提交剩余的数据行
      }
      catch (Exception ex)
      {
        MessageBox.Show($"数据导入失败: {ex.Message}");
      }

      MessageBox.Show("数据提交完成！");
      要删除项.Clear();
      要添加项.Clear();
      要修改项.Clear();
      修改项值.Clear();

      var count = 获取表数据总行数(_tableName);
      _pageCount    = count / _pageLen;
      countTextBox.Text = $"词条数：{count}";
      pageTextBox.Text  = $"{_currentPage}/{_pageCount}";

      var dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
    }

    private void 删除数据行(SQLiteConnection con, string key, string value)
    {
      var command = new SQLiteCommand(con);
      command.CommandText = $"DELETE FROM '{_tableName}' WHERE key = @key AND value = @value";
      command.Parameters.AddWithValue("@key",   key);
      command.Parameters.AddWithValue("@value", value);

      // command.Prepare();
      command.ExecuteNonQuery();
    }

    private void 添加数据行(SQLiteConnection con, string key, string value, int weight, string fc)
    {
      var command = new SQLiteCommand(con);
      command.CommandText     = $"INSERT INTO '{_tableName}'(key, value, weight, fc) VALUES(@key,@value,@weight,@fc)";
      command.Parameters.AddWithValue("@key",    key);
      command.Parameters.AddWithValue("@value",  value);
      command.Parameters.AddWithValue("@weight", weight);
      command.Parameters.AddWithValue("@fc",     fc);

      // command.Prepare();
      command.ExecuteNonQuery();
    }

    private void 修改数据行(SQLiteConnection con, string key, string value, int weight, string fc, string oldKey, string oldValue)
    {
      var command = new SQLiteCommand(con);
      command.CommandText = $"UPDATE '{_tableName}' SET key=@key, value=@value, weight=@weight, fc=@fc WHERE key=@oldKey AND value=@oldValue";
      command.Parameters.AddWithValue("@key",      key);
      command.Parameters.AddWithValue("@value",    value);
      command.Parameters.AddWithValue("@weight",   weight);
      command.Parameters.AddWithValue("@fc",       fc);
      command.Parameters.AddWithValue("@oldKey",   oldKey);
      command.Parameters.AddWithValue("@oldValue", oldValue);

      // command.Prepare();
      command.ExecuteNonQuery();
    }

    private void 获取数据()
    {
      var count = 获取表数据总行数(_tableName);
      _pageLen   = 5000;
      _pageCount = count / _pageLen;
      if (count          % _pageLen > 0) _pageCount++;

      countTextBox.Text = $"词条数：{count}";
      _currentPage  = 0;
      pageTextBox.Text  = $"{_currentPage + 1}/{_pageCount}";
      var dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
      数据填充到列表(dataTable);
      var fontName = 从表里获取字体名称(_tableName);
      if (fontName == null) return;
      for (var i = 0; i < fontComboBox.Items.Count; i++)
        if (fontComboBox.Items[i].ToString() == fontName)
          fontComboBox.SelectedIndex = i;
    }

    private void 搜索()
    {
      DataTable dataTable = new();
      if (searchTextBox.Text == "")
      {
        dataTable = 从表内指定行开始获取指定行数的数据(_currentPage * _pageLen, _pageLen);
        数据填充到列表(dataTable);
        var count = 获取表数据总行数(_tableName);
        countTextBox.Text = $"词条数：{count}";
        pageTextBox.Text  = $"{_currentPage + 1}/{_pageCount}";
        stackPanel_5.IsEnabled = true;
        addOrModCheckBox.IsEnabled = true;

        return;
      }

      var name = keyOrValueCheckBox.IsChecked == true ? "value" : "key";
      var str  = searchTextBox.Text;

      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      var query = $"SELECT ROW_NUMBER() OVER (ORDER BY ROWID) AS RowNumber, * FROM '{_tableName}' WHERE {name} LIKE @str";
      connection.Open();

      using SQLiteCommand command = new(query, connection);
      command.Parameters.AddWithValue("@str", $"{str}%");

      using var reader = command.ExecuteReader();
      dataTable.Load(reader);
      countTextBox.Text = $"词条数：{dataTable.Rows.Count}";
      pageTextBox.Text  = "1/1";
      数据填充到列表(dataTable);
      stackPanel_5.IsEnabled = false;
      addOrModCheckBox.IsChecked = false;
      addOrModCheckBox.IsEnabled = false;
      add_Mod_Button.Content = "修改";
    }

    /// <summary>
    /// 复制表
    /// </summary>
    /// <param name="newTableName">新的表名</param>
    private void 复制表(string newTableName)
    {      
      // if(comboBox.SelectedIndex == -1) return;
      using var connection = new SQLiteConnection($"Data Source={_dbPath}");
      connection.Open();
      using var transaction = connection.BeginTransaction();
      try
      {        
        // 创建新表
        using (var createCommand = new SQLiteCommand($"CREATE TABLE IF NOT EXISTS '{newTableName}' AS SELECT * FROM '{_tableName}' WHERE 1 = 0", connection, transaction))
          createCommand.ExecuteNonQuery();

        // 复制数据到新表
        using (var copyCommand = new SQLiteCommand($"INSERT INTO '{newTableName}' SELECT * FROM '{_tableName}'", connection, transaction))
          copyCommand.ExecuteNonQuery();

        // 删除旧表
        // using (var dropCommand = new SQLiteCommand($"DROP TABLE IF EXISTS '{_tableName}'", connection, transaction)) dropCommand.ExecuteNonQuery();

        // 提交事务
        transaction.Commit();
        MessageBox.Show("复制表成功！");
      }
      catch (Exception ex)
      {
        // 回滚事务并显示错误消息
        transaction.Rollback();
        MessageBox.Show($"操作出错: {ex.Message}");
      }
    }

    /// <summary>
    /// 删除表
    /// </summary>
    /// <param name="tableName">表名</param>
    private bool 删除表(string tableName)
    {
      try
      {
        using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
        connection.Open();
        var       deleteQuery = $"DROP TABLE IF EXISTS '{tableName}'";
        using var command     = new SQLiteCommand(deleteQuery, connection);

        command.ExecuteNonQuery();
        MessageBox.Show("表删除成功");
        return true;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"删除表失败: {ex.Message}");
      }
      return false;
    }

    private bool 是否有相同数据(string key, string value)
    {
      using SQLiteConnection connection = new($"Data Source={_dbPath};Version=3;");
      connection.Open();
      var                 query   = $"SELECT EXISTS(SELECT 1 FROM '{_tableName}' WHERE key = @key AND value = @value)";
      using SQLiteCommand command = new(query, connection);
      command.Parameters.AddWithValue("@key",   key);
      command.Parameters.AddWithValue("@value", value);
      var result = command.ExecuteScalar();
      return Convert.ToInt32(result) == 1;
    }

    /// <summary>
    /// 弹出文件选择框
    /// </summary>
    /// <returns>文件的路径</returns>
    private static string GetCsvFilePathToImport()
    {
      var openFileDialog = new OpenFileDialog()
      {
        Title = @"选择 CSV 文件导入新表",
        Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
        FilterIndex = 1,
        RestoreDirectory = true
      };

      // 显示文件对话框并获取用户选择的文件路径
      return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : null;
    }

    /// <summary>
    /// 创建新的空表
    /// </summary>
    /// <param name="databasePath"></param>
    /// <param name="tableName"></param>
    private static void CreateNewTable(string databasePath, string tableName)
    {
      var connectionString = $"Data Source={databasePath};Version=3;";

      // 使用参数传入的表名创建新表，包含四列（key, value, weight, fc）
      // var createTableQuery = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" (key TEXT, value TEXT, weight INTEGER, fc TEXT)";
      var createTableQuery = $"CREATE TABLE IF NOT EXISTS '{tableName}' (key TEXT, value TEXT, weight INTEGER, fc TEXT)";

      using var connection = new SQLiteConnection(connectionString);
      connection.Open();
      using var command = new SQLiteCommand(createTableQuery, connection);
      command.ExecuteNonQuery();
    }

    /// <summary>
    /// 导入CSV文件数据到新表
    /// </summary>
    /// <param name="databasePath">数据库路径</param>
    /// <param name="tableName">新表名称</param>
    /// <param name="csvFilePath">CSV 文件路径</param>
    private void ImportCsvToTable(string databasePath, string tableName, string csvFilePath)
    {
      try
      {
        var       connectionString = $"Data Source={databasePath};Version=3;";
        using var connection       = new SQLiteConnection(connectionString);
        connection.Open();
        // 开始一个事务
        using var transaction = connection.BeginTransaction();
        using var reader      = new StreamReader(csvFilePath);
        foreach (var line in File.ReadLines(csvFilePath).Skip(1))
        {
          var data = line.Split(',');
          if (data.Length != 4) continue; // Ensure there are exactly four elements

          var key    = data[0].Trim('"');
          var value  = data[1].Trim('"');
          var weight = data[2].Trim('"');
          var fc     = data[3].Trim('"');

          var insertQuery = $"INSERT INTO '{tableName}' (key, value, weight, fc) VALUES (@key, @value, @weight, @fc)";
          using var command = new SQLiteCommand(insertQuery, connection);

          command.Parameters.AddWithValue("@key",    key);
          command.Parameters.AddWithValue("@value",  value);
          command.Parameters.AddWithValue("@weight", weight);
          command.Parameters.AddWithValue("@fc",     fc);

          command.ExecuteNonQuery();
        }
        transaction.Commit();  // 提交剩余的数据行
        MessageBox.Show("数据导入成功");
        comboBox.Items.Add(tableName);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"数据导入失败: {ex.Message}");
      }
    }

    /// <summary>
    /// 获取导出表到 CSV 文件路径
    /// </summary>
    /// <returns></returns>
    private static string GetCsvFilePathToExport()
    {
      var saveFileDialog = new SaveFileDialog
      {
        Title            = @"导出表到 CSV 文件",
        Filter           = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
        FilterIndex      = 1,
        RestoreDirectory = true
      };

      // 显示文件对话框并获取用户选择的文件路径
      return saveFileDialog.ShowDialog() == DialogResult.OK ? saveFileDialog.FileName : null;
    }

    /// <summary>
    /// 导出表到CSV文件
    /// </summary>
    /// <param name="databasePath">数据库文件鼠径</param>
    /// <param name="tableName">要导出的表名称</param>
    /// <param name="csvFilePath">CSV 文件的鼠径</param>
    private static void ExportTableToCsv(string databasePath, string tableName, string csvFilePath)
    {
      var connectionString = $"Data Source={databasePath};Version=3;";

      // 使用SQLite连接器连接到数据库
      using var connection = new SQLiteConnection(connectionString);
      connection.Open();

      // 创建SQL查询命令
      var       query   = $"SELECT * FROM '{tableName}'";
      using var command = new SQLiteCommand(query, connection);
      using var reader  = command.ExecuteReader();
      if (reader.HasRows)
      {
        // 创建一个StreamWriter来写入CSV文件
        using var writer = new StreamWriter(csvFilePath, false, Encoding.UTF8);
        // 写入CSV文件的标题行（列名）
        var headerBuilder = new StringBuilder();
        for (var i = 0; i < reader.FieldCount; i++)
        {
          headerBuilder.Append($"\"{reader.GetName(i)}\"");
          if (i < reader.FieldCount - 1)
            headerBuilder.Append(",");
        }
        writer.WriteLine(headerBuilder.ToString());

        // 逐行写入数据到CSV文件
        while (reader.Read())
        {
          var dataBuilder = new StringBuilder();
          for (var i = 0; i < reader.FieldCount; i++)
          {
            // 处理字段值中的特殊字符（如逗号、引号等）
            var value = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
            value = value.Replace("\"", "\"\"");
            value = $"\"{value}\"";
            dataBuilder.Append(value);
            if (i < reader.FieldCount - 1)
              dataBuilder.Append(",");
          }
          writer.WriteLine(dataBuilder.ToString());
        }
      }
      else
      {
        MessageBox.Show("这是一个空表");
        return;
      }

      MessageBox.Show("导出表成功");
    }

    #endregion

    #region 其它

    /// <summary>
    /// 编缉状态切换
    /// </summary>
    /// <param name="b"></param>
    private void 切换编缉状态(bool b)
    {
      _onlyReading            = b;
      submit_Button.IsEnabled = b;
      stackPanel_1.IsEnabled  = !b;
      stackPanel_4.IsEnabled  = !b;
      stackPanel_5.IsEnabled  = !b;
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

    private void 在最后插入数据()
    {
      if(keyTextBox.Text == "" || valueTextBox.Text == "") return;

      ListViewDataItem lastItem = new();
      if (ListViewData.Count - 1 >= 0)
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
      listView.ScrollIntoView(newItem);
    }

    private void 修改选中行数据()
    {
      foreach (ListViewDataItem item in listView.SelectedItems)
      {
        要修改项.Add(item.Clone());
        item.Key    = keyTextBox.Text;
        item.Value  = valueTextBox.Text;
        item.Weight = weightTextBox.Text == "" ? 0 : int.Parse(weightTextBox.Text);
        item.Fc     = fcTextBox.Text;
        item.IsMod  = true;
        修改项值.Add(item);
      }
    }


    #endregion

    #endregion













  }
}
