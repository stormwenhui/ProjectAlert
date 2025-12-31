using System.Windows;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 数据库连接编辑对话框
/// </summary>
public partial class DbConnectionEditDialog : HandyControl.Controls.Window
{
    private readonly IDbConnectionRepository _repository;
    private readonly DbConnection _connection;
    private readonly bool _isNew;

    /// <summary>
    /// 是否已保存
    /// </summary>
    public bool IsSaved { get; private set; }

    /// <summary>
    /// 连接对象
    /// </summary>
    public DbConnection Connection => _connection;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">仓储</param>
    /// <param name="connection">连接对象（null表示新增）</param>
    public DbConnectionEditDialog(IDbConnectionRepository repository, DbConnection? connection = null)
    {
        InitializeComponent();
        _repository = repository;
        _isNew = connection == null;
        _connection = connection ?? new DbConnection
        {
            Name = "新连接",
            DbType = DbType.MySql,
            ConnectionString = "Server=localhost;Database=mydb;User=root;Password=;",
            Enabled = true
        };

        DataContext = _connection;

        // 设置下拉框选中项
        CmbDbType.SelectedIndex = _connection.DbType == DbType.MySql ? 0 : 1;
    }

    /// <summary>
    /// 测试连接按钮点击事件
    /// </summary>
    private async void BtnTest_Click(object sender, RoutedEventArgs e)
    {
        TxtTestResult.Text = "正在测试连接...";
        TxtTestResult.Foreground = System.Windows.Media.Brushes.Gray;

        try
        {
            // 更新 DbType
            UpdateDbType();

            var success = await _repository.TestConnectionAsync(_connection);
            if (success)
            {
                TxtTestResult.Text = "连接成功！";
                TxtTestResult.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                TxtTestResult.Text = "连接失败";
                TxtTestResult.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        catch (Exception ex)
        {
            TxtTestResult.Text = $"连接失败: {ex.Message}";
            TxtTestResult.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    /// <summary>
    /// 保存按钮点击事件
    /// </summary>
    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // 验证
        if (string.IsNullOrWhiteSpace(_connection.Name))
        {
            HandyControl.Controls.MessageBox.Show("请输入连接名称", "提示");
            return;
        }

        if (string.IsNullOrWhiteSpace(_connection.ConnectionString))
        {
            HandyControl.Controls.MessageBox.Show("请输入连接字符串", "提示");
            return;
        }

        // 更新 DbType
        UpdateDbType();

        try
        {
            if (_isNew)
            {
                await _repository.InsertAsync(_connection);
            }
            else
            {
                await _repository.UpdateAsync(_connection);
            }

            IsSaved = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            HandyControl.Controls.MessageBox.Show($"保存失败: {ex.Message}", "错误");
        }
    }

    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// 更新数据库类型
    /// </summary>
    private void UpdateDbType()
    {
        _connection.DbType = CmbDbType.SelectedIndex == 0 ? DbType.MySql : DbType.SqlServer;
    }
}
