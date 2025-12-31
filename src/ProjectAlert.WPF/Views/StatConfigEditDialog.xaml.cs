using System.Windows;
using System.Windows.Controls;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 统计配置编辑对话框
/// </summary>
public partial class StatConfigEditDialog : HandyControl.Controls.Window
{
    private readonly IStatConfigRepository _configRepository;
    private readonly IDbConnectionRepository _dbConnectionRepository;
    private readonly StatConfig _config;
    private readonly bool _isNew;
    private List<DbConnection> _dbConnections = new();

    /// <summary>
    /// 是否已保存
    /// </summary>
    public bool IsSaved { get; private set; }

    /// <summary>
    /// 配置对象
    /// </summary>
    public StatConfig Config => _config;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="configRepository">统计配置仓储</param>
    /// <param name="dbConnectionRepository">数据库连接仓储</param>
    /// <param name="config">配置对象（null表示新增）</param>
    public StatConfigEditDialog(
        IStatConfigRepository configRepository,
        IDbConnectionRepository dbConnectionRepository,
        StatConfig? config = null)
    {
        InitializeComponent();
        _configRepository = configRepository;
        _dbConnectionRepository = dbConnectionRepository;
        _isNew = config == null;
        _config = config ?? new StatConfig
        {
            Name = "",
            SourceType = SourceType.Sql,
            ChartType = ChartType.表格,
            RefreshInterval = 60,
            ApiTimeout = 30,
            Enabled = true
        };

        InitializeControls();
        LoadData();
    }

    /// <summary>
    /// 初始化控件
    /// </summary>
    private void InitializeControls()
    {
        // 系统分类
        CmbCategory.ItemsSource = Enum.GetValues<SystemCategory>();

        // 数据源类型
        CmbSourceType.ItemsSource = Enum.GetValues<SourceType>();

        // 图表类型
        CmbChartType.ItemsSource = Enum.GetValues<ChartType>();
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    private async void LoadData()
    {
        // 加载数据库连接
        _dbConnections = (await _dbConnectionRepository.GetEnabledAsync()).ToList();
        CmbDbConnection.ItemsSource = _dbConnections;

        // 绑定数据
        TxtName.Text = _config.Name;
        CmbCategory.SelectedItem = _config.Category;

        CmbSourceType.SelectedItem = _config.SourceType;
        if (_config.DbConnectionId.HasValue)
        {
            CmbDbConnection.SelectedItem = _dbConnections.FirstOrDefault(c => c.Id == _config.DbConnectionId);
        }

        TxtSqlQuery.Text = _config.SqlQuery;

        TxtApiUrl.Text = _config.ApiUrl;
        CmbApiMethod.SelectedIndex = _config.ApiMethod?.ToUpper() == "POST" ? 1 : 0;
        TxtApiHeaders.Text = _config.ApiHeaders;
        TxtApiBody.Text = _config.ApiBody;
        TxtApiTimeout.Text = _config.ApiTimeout.ToString();
        TxtDataPath.Text = _config.DataPath;

        CmbChartType.SelectedItem = _config.ChartType;
        TxtRefreshInterval.Text = _config.RefreshInterval.ToString();
        TxtSortOrder.Text = _config.SortOrder.ToString();

        ChkEnabled.IsChecked = _config.Enabled;

        // 更新UI显示
        UpdateSourceTypeUI();
    }

    /// <summary>
    /// 数据源类型选择变化事件
    /// </summary>
    private void CmbSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSourceTypeUI();
    }

    /// <summary>
    /// 根据数据源类型更新UI
    /// </summary>
    private void UpdateSourceTypeUI()
    {
        if (CmbSourceType.SelectedItem is SourceType sourceType)
        {
            if (sourceType == SourceType.Sql)
            {
                GrpSql.Visibility = Visibility.Visible;
                GrpApi.Visibility = Visibility.Collapsed;
                LblDbConnection.Visibility = Visibility.Visible;
                CmbDbConnection.Visibility = Visibility.Visible;
            }
            else
            {
                GrpSql.Visibility = Visibility.Collapsed;
                GrpApi.Visibility = Visibility.Visible;
                LblDbConnection.Visibility = Visibility.Collapsed;
                CmbDbConnection.Visibility = Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    /// 保存按钮点击事件
    /// </summary>
    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // 验证
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            HandyControl.Controls.MessageBox.Show("请输入名称", "提示");
            return;
        }

        if (CmbCategory.SelectedItem == null)
        {
            HandyControl.Controls.MessageBox.Show("请选择系统分类", "提示");
            return;
        }

        // 收集数据
        _config.Name = TxtName.Text;
        _config.Category = (SystemCategory)CmbCategory.SelectedItem;

        _config.SourceType = (SourceType)(CmbSourceType.SelectedItem ?? SourceType.Sql);
        _config.DbConnectionId = (CmbDbConnection.SelectedItem as DbConnection)?.Id;

        _config.SqlQuery = TxtSqlQuery.Text;

        _config.ApiUrl = TxtApiUrl.Text;
        _config.ApiMethod = CmbApiMethod.SelectedIndex == 1 ? "POST" : "GET";
        _config.ApiHeaders = TxtApiHeaders.Text;
        _config.ApiBody = TxtApiBody.Text;
        _config.ApiTimeout = int.TryParse(TxtApiTimeout.Text, out var timeout) ? timeout : 30;
        _config.DataPath = TxtDataPath.Text;

        _config.ChartType = (ChartType)(CmbChartType.SelectedItem ?? ChartType.表格);
        _config.RefreshInterval = int.TryParse(TxtRefreshInterval.Text, out var interval) ? interval : 60;
        _config.SortOrder = int.TryParse(TxtSortOrder.Text, out var sortOrder) ? sortOrder : 0;

        _config.Enabled = ChkEnabled.IsChecked == true;

        try
        {
            if (_isNew)
            {
                await _configRepository.InsertAsync(_config);
            }
            else
            {
                await _configRepository.UpdateAsync(_config);
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
}
