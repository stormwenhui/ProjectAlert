using Dapper.Contrib.Extensions;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.Domain.Entities;

/// <summary>
/// 预警规则
/// </summary>
[Table("alert_rules")]
public class AlertRule : BaseEntity
{
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 系统分类（必填）
    /// </summary>
    public SystemCategory Category { get; set; }

    /// <summary>
    /// 数据源类型
    /// </summary>
    public SourceType SourceType { get; set; }

    #region SQL 配置

    /// <summary>
    /// 数据库连接ID
    /// </summary>
    public int? DbConnectionId { get; set; }

    /// <summary>
    /// SQL 查询语句
    /// </summary>
    public string? SqlQuery { get; set; }

    #endregion

    #region API 配置（内联）

    /// <summary>
    /// API 请求地址
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// API 请求方法
    /// </summary>
    public string? ApiMethod { get; set; }

    /// <summary>
    /// API 请求头 (JSON)
    /// </summary>
    public string? ApiHeaders { get; set; }

    /// <summary>
    /// API 请求体
    /// </summary>
    public string? ApiBody { get; set; }

    /// <summary>
    /// API 超时时间（秒）
    /// </summary>
    public int ApiTimeout { get; set; } = 30;

    #endregion

    #region 判断配置

    /// <summary>
    /// API 判断层级
    /// </summary>
    public JudgeLevel? JudgeLevel { get; set; }

    /// <summary>
    /// JSON 数据路径
    /// </summary>
    public string? DataPath { get; set; }

    /// <summary>
    /// 判断方式
    /// </summary>
    public JudgeType JudgeType { get; set; }

    /// <summary>
    /// 判断字段名
    /// </summary>
    public string? JudgeField { get; set; }

    /// <summary>
    /// 判断运算符
    /// </summary>
    public JudgeOperator? JudgeOperator { get; set; }

    /// <summary>
    /// 判断值/阈值
    /// </summary>
    public string? JudgeValue { get; set; }

    /// <summary>
    /// 唯一标识字段名（多行预警用）
    /// </summary>
    public string? KeyField { get; set; }

    #endregion

    #region 预警配置

    /// <summary>
    /// 预警级别
    /// </summary>
    public AlertLevel AlertLevel { get; set; } = AlertLevel.警告;

    /// <summary>
    /// 消息模板
    /// </summary>
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Cron 表达式
    /// </summary>
    public string CronExpression { get; set; } = "0 */5 * * * ?";

    /// <summary>
    /// 连续失败阈值
    /// </summary>
    public int FailThreshold { get; set; } = 1;

    /// <summary>
    /// 当前连续失败次数
    /// </summary>
    public int CurrentFailCount { get; set; } = 0;

    #endregion

    #region 状态

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 上次执行时间
    /// </summary>
    public DateTime? LastRunTime { get; set; }

    /// <summary>
    /// 上次执行是否成功
    /// </summary>
    public bool? LastRunSuccess { get; set; }

    /// <summary>
    /// 上次执行结果
    /// </summary>
    public string? LastRunResult { get; set; }

    #endregion

    #region 导航属性

    /// <summary>
    /// 关联的数据库连接
    /// </summary>
    [Write(false)]
    public DbConnection? DbConnection { get; set; }

    #endregion
}
