using System.Windows;
using System.Windows.Controls;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.WPF.Views;

/// <summary>
/// 预警规则编辑对话框
/// </summary>
public partial class AlertRuleEditDialog : HandyControl.Controls.Window
{
    private readonly IAlertRuleRepository _ruleRepository;
    private readonly IDbConnectionRepository _dbConnectionRepository;
    private readonly AlertRule _rule;
    private readonly bool _isNew;
    private List<DbConnection> _dbConnections = new();

    /// <summary>
    /// 是否已保存
    /// </summary>
    public bool IsSaved { get; private set; }

    /// <summary>
    /// 规则对象
    /// </summary>
    public AlertRule Rule => _rule;

    // SQL判断类型选项
    private static readonly JudgeType[] SqlJudgeTypes = { JudgeType.单值预警, JudgeType.多行预警 };

    // API判断类型选项
    private static readonly JudgeType[] ApiJudgeTypes = { JudgeType.请求层判断, JudgeType.响应文本层判断, JudgeType.JSON解析层_单值, JudgeType.JSON解析层_多行 };

    // 数值运算符（用于单值预警）
    private static readonly JudgeOperator[] NumericOperators = { JudgeOperator.大于, JudgeOperator.大于等于, JudgeOperator.小于, JudgeOperator.小于等于, JudgeOperator.等于, JudgeOperator.不等于 };

    // 文本运算符（用于响应文本层判断）
    private static readonly JudgeOperator[] TextOperators = { JudgeOperator.等于, JudgeOperator.不等于, JudgeOperator.包含, JudgeOperator.不包含 };

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ruleRepository">规则仓储</param>
    /// <param name="dbConnectionRepository">数据库连接仓储</param>
    /// <param name="rule">规则对象（null表示新增）</param>
    public AlertRuleEditDialog(
        IAlertRuleRepository ruleRepository,
        IDbConnectionRepository dbConnectionRepository,
        AlertRule? rule = null)
    {
        InitializeComponent();
        _ruleRepository = ruleRepository;
        _dbConnectionRepository = dbConnectionRepository;
        _isNew = rule == null;
        _rule = rule ?? new AlertRule
        {
            Name = "",
            SourceType = SourceType.Sql,
            AlertLevel = AlertLevel.警告,
            CronExpression = "0 */5 * * * ?",
            FailThreshold = 1,
            ApiTimeout = 30,
            JudgeType = JudgeType.单值预警,
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

        // 预警级别
        CmbAlertLevel.ItemsSource = Enum.GetValues<AlertLevel>();

        // 数据源类型
        CmbSourceType.ItemsSource = Enum.GetValues<SourceType>();

        // SQL判断类型
        CmbSqlJudgeType.ItemsSource = SqlJudgeTypes;

        // SQL运算符
        CmbSqlOperator.ItemsSource = NumericOperators;

        // API判断类型
        CmbApiJudgeType.ItemsSource = ApiJudgeTypes;

        // API请求层运算符
        CmbApiRequestOperator.ItemsSource = NumericOperators;

        // API文本层运算符
        CmbApiTextOperator.ItemsSource = TextOperators;

        // API JSON层运算符
        CmbApiJsonOperator.ItemsSource = NumericOperators;
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
        TxtName.Text = _rule.Name;
        CmbCategory.SelectedItem = _rule.Category;
        CmbAlertLevel.SelectedItem = _rule.AlertLevel;
        TxtCron.Text = _rule.CronExpression;

        CmbSourceType.SelectedItem = _rule.SourceType;
        if (_rule.DbConnectionId.HasValue)
        {
            CmbDbConnection.SelectedItem = _dbConnections.FirstOrDefault(c => c.Id == _rule.DbConnectionId);
        }

        // SQL配置
        TxtSqlQuery.Text = _rule.SqlQuery;

        // API配置
        TxtApiUrl.Text = _rule.ApiUrl;
        CmbApiMethod.SelectedIndex = _rule.ApiMethod?.ToUpper() == "POST" ? 1 : 0;
        TxtApiHeaders.Text = _rule.ApiHeaders;
        TxtApiBody.Text = _rule.ApiBody;
        TxtApiTimeout.Text = _rule.ApiTimeout.ToString();

        // 根据数据源类型设置判断类型
        if (_rule.SourceType == SourceType.Sql)
        {
            // SQL判断类型
            if (_rule.JudgeType == JudgeType.单值预警 || _rule.JudgeType == JudgeType.多行预警)
            {
                CmbSqlJudgeType.SelectedItem = _rule.JudgeType;
            }
            else
            {
                CmbSqlJudgeType.SelectedItem = JudgeType.单值预警;
            }

            // SQL单值配置
            TxtSqlJudgeField.Text = _rule.JudgeField;
            CmbSqlOperator.SelectedItem = _rule.JudgeOperator;
            TxtSqlThreshold.Text = _rule.JudgeValue;

            // SQL多行配置
            TxtSqlKeyField.Text = _rule.KeyField;
        }
        else
        {
            // API判断类型
            if (ApiJudgeTypes.Contains(_rule.JudgeType))
            {
                CmbApiJudgeType.SelectedItem = _rule.JudgeType;
            }
            else
            {
                CmbApiJudgeType.SelectedItem = JudgeType.请求层判断;
            }

            // API请求层配置
            CmbApiRequestOperator.SelectedItem = _rule.JudgeOperator;
            TxtApiStatusCode.Text = _rule.JudgeValue;

            // API文本层配置
            CmbApiTextOperator.SelectedItem = _rule.JudgeOperator;
            TxtApiTextMatch.Text = _rule.JudgeValue;

            // API JSON单值配置
            TxtApiJsonPath.Text = _rule.DataPath;
            TxtApiJsonField.Text = _rule.JudgeField;
            CmbApiJsonOperator.SelectedItem = _rule.JudgeOperator;
            TxtApiJsonThreshold.Text = _rule.JudgeValue;

            // API JSON多行配置
            TxtApiJsonMultiPath.Text = _rule.DataPath;
            TxtApiJsonKeyField.Text = _rule.KeyField;
        }

        TxtMessageTemplate.Text = _rule.MessageTemplate;
        TxtFailThreshold.Text = _rule.FailThreshold.ToString();
        ChkEnabled.IsChecked = _rule.Enabled;

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
    /// SQL判断类型选择变化事件
    /// </summary>
    private void CmbSqlJudgeType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSqlJudgeTypeUI();
    }

    /// <summary>
    /// API判断类型选择变化事件
    /// </summary>
    private void CmbApiJudgeType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateApiJudgeTypeUI();
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
                // 显示SQL配置
                PnlSqlConfig.Visibility = Visibility.Visible;
                PnlApiConfig.Visibility = Visibility.Collapsed;
                LblDbConnection.Visibility = Visibility.Visible;
                CmbDbConnection.Visibility = Visibility.Visible;

                // 更新消息模板提示
                TxtMessageTemplateHint.Text = "可用变量: {table.列名} - SQL查询返回的字段值\n示例: 客户 {table.CustomerName} 有异常: {table.LogContent}";

                UpdateSqlJudgeTypeUI();
            }
            else
            {
                // 显示API配置
                PnlSqlConfig.Visibility = Visibility.Collapsed;
                PnlApiConfig.Visibility = Visibility.Visible;
                LblDbConnection.Visibility = Visibility.Collapsed;
                CmbDbConnection.Visibility = Visibility.Collapsed;

                // 更新消息模板提示
                TxtMessageTemplateHint.Text = "可用变量: {api.参数名} - API返回的JSON字段值\n示例: 服务 {api.serviceName} 状态异常: {api.errorMsg}";

                UpdateApiJudgeTypeUI();
            }
        }
    }

    /// <summary>
    /// 根据SQL判断类型更新UI
    /// </summary>
    private void UpdateSqlJudgeTypeUI()
    {
        if (CmbSqlJudgeType.SelectedItem is JudgeType judgeType)
        {
            if (judgeType == JudgeType.单值预警)
            {
                PnlSqlSingleValue.Visibility = Visibility.Visible;
                PnlSqlMultiRow.Visibility = Visibility.Collapsed;
                TxtSqlKeyFieldHint.Visibility = Visibility.Collapsed;
            }
            else
            {
                PnlSqlSingleValue.Visibility = Visibility.Collapsed;
                PnlSqlMultiRow.Visibility = Visibility.Visible;
                TxtSqlKeyFieldHint.Visibility = Visibility.Visible;
            }
        }
    }

    /// <summary>
    /// 根据API判断类型更新UI
    /// </summary>
    private void UpdateApiJudgeTypeUI()
    {
        // 隐藏所有API判断配置
        PnlApiRequestLevel.Visibility = Visibility.Collapsed;
        TxtApiRequestHint.Visibility = Visibility.Collapsed;
        PnlApiTextLevel.Visibility = Visibility.Collapsed;
        TxtApiTextHint.Visibility = Visibility.Collapsed;
        PnlApiJsonSingle.Visibility = Visibility.Collapsed;
        TxtApiJsonSingleHint.Visibility = Visibility.Collapsed;
        PnlApiJsonMulti.Visibility = Visibility.Collapsed;
        TxtApiJsonMultiHint.Visibility = Visibility.Collapsed;

        if (CmbApiJudgeType.SelectedItem is JudgeType judgeType)
        {
            switch (judgeType)
            {
                case JudgeType.请求层判断:
                    PnlApiRequestLevel.Visibility = Visibility.Visible;
                    TxtApiRequestHint.Visibility = Visibility.Visible;
                    break;
                case JudgeType.响应文本层判断:
                    PnlApiTextLevel.Visibility = Visibility.Visible;
                    TxtApiTextHint.Visibility = Visibility.Visible;
                    break;
                case JudgeType.JSON解析层_单值:
                    PnlApiJsonSingle.Visibility = Visibility.Visible;
                    TxtApiJsonSingleHint.Visibility = Visibility.Visible;
                    break;
                case JudgeType.JSON解析层_多行:
                    PnlApiJsonMulti.Visibility = Visibility.Visible;
                    TxtApiJsonMultiHint.Visibility = Visibility.Visible;
                    break;
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
            HandyControl.Controls.MessageBox.Show("请输入规则名称", "提示");
            return;
        }

        if (CmbCategory.SelectedItem == null)
        {
            HandyControl.Controls.MessageBox.Show("请选择系统分类", "提示");
            return;
        }

        // 收集通用数据
        _rule.Name = TxtName.Text;
        _rule.Category = (SystemCategory)CmbCategory.SelectedItem;
        _rule.AlertLevel = (AlertLevel)(CmbAlertLevel.SelectedItem ?? AlertLevel.警告);
        _rule.CronExpression = TxtCron.Text;

        _rule.SourceType = (SourceType)(CmbSourceType.SelectedItem ?? SourceType.Sql);
        _rule.DbConnectionId = (CmbDbConnection.SelectedItem as DbConnection)?.Id;

        _rule.MessageTemplate = TxtMessageTemplate.Text;
        _rule.FailThreshold = int.TryParse(TxtFailThreshold.Text, out var threshold) ? threshold : 1;
        _rule.Enabled = ChkEnabled.IsChecked == true;

        // 根据数据源类型收集数据
        if (_rule.SourceType == SourceType.Sql)
        {
            _rule.SqlQuery = TxtSqlQuery.Text;
            _rule.JudgeType = (JudgeType)(CmbSqlJudgeType.SelectedItem ?? JudgeType.单值预警);

            // 清除API相关字段
            _rule.ApiUrl = null;
            _rule.ApiMethod = null;
            _rule.ApiHeaders = null;
            _rule.ApiBody = null;
            _rule.DataPath = null;

            if (_rule.JudgeType == JudgeType.单值预警)
            {
                _rule.JudgeField = TxtSqlJudgeField.Text;
                _rule.JudgeOperator = CmbSqlOperator.SelectedItem as JudgeOperator?;
                _rule.JudgeValue = TxtSqlThreshold.Text;
                _rule.KeyField = null;
            }
            else
            {
                _rule.KeyField = TxtSqlKeyField.Text;
                _rule.JudgeField = null;
                _rule.JudgeOperator = null;
                _rule.JudgeValue = null;
            }
        }
        else
        {
            _rule.ApiUrl = TxtApiUrl.Text;
            _rule.ApiMethod = CmbApiMethod.SelectedIndex == 1 ? "POST" : "GET";
            _rule.ApiHeaders = TxtApiHeaders.Text;
            _rule.ApiBody = TxtApiBody.Text;
            _rule.ApiTimeout = int.TryParse(TxtApiTimeout.Text, out var timeout) ? timeout : 30;
            _rule.JudgeType = (JudgeType)(CmbApiJudgeType.SelectedItem ?? JudgeType.请求层判断);

            // 清除SQL相关字段
            _rule.SqlQuery = null;

            switch (_rule.JudgeType)
            {
                case JudgeType.请求层判断:
                    _rule.JudgeOperator = CmbApiRequestOperator.SelectedItem as JudgeOperator?;
                    _rule.JudgeValue = TxtApiStatusCode.Text;
                    _rule.DataPath = null;
                    _rule.JudgeField = null;
                    _rule.KeyField = null;
                    break;
                case JudgeType.响应文本层判断:
                    _rule.JudgeOperator = CmbApiTextOperator.SelectedItem as JudgeOperator?;
                    _rule.JudgeValue = TxtApiTextMatch.Text;
                    _rule.DataPath = null;
                    _rule.JudgeField = null;
                    _rule.KeyField = null;
                    break;
                case JudgeType.JSON解析层_单值:
                    _rule.DataPath = TxtApiJsonPath.Text;
                    _rule.JudgeField = TxtApiJsonField.Text;
                    _rule.JudgeOperator = CmbApiJsonOperator.SelectedItem as JudgeOperator?;
                    _rule.JudgeValue = TxtApiJsonThreshold.Text;
                    _rule.KeyField = null;
                    break;
                case JudgeType.JSON解析层_多行:
                    _rule.DataPath = TxtApiJsonMultiPath.Text;
                    _rule.KeyField = TxtApiJsonKeyField.Text;
                    _rule.JudgeField = null;
                    _rule.JudgeOperator = null;
                    _rule.JudgeValue = null;
                    break;
            }
        }

        try
        {
            if (_isNew)
            {
                await _ruleRepository.InsertAsync(_rule);
            }
            else
            {
                await _ruleRepository.UpdateAsync(_rule);
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
