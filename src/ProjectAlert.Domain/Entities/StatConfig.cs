using Dapper.Contrib.Extensions;
using ProjectAlert.Domain.Enums;

namespace ProjectAlert.Domain.Entities;

/// <summary>
/// 统计配置
/// </summary>
[Table("stat_configs")]
public class StatConfig : BaseEntity
{
    /// <summary>
    /// 统计名称
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

    /// <summary>
    /// JSON 数据路径
    /// </summary>
    public string? DataPath { get; set; }

    #endregion

    #region 图表配置

    /// <summary>
    /// 图表类型
    /// </summary>
    public ChartType ChartType { get; set; } = ChartType.表格;

    /// <summary>
    /// 图表配置 (JSON)
    /// </summary>
    public string? ChartConfig { get; set; }

    /// <summary>
    /// 刷新间隔（秒）
    /// </summary>
    public int RefreshInterval { get; set; } = 60;

    /// <summary>
    /// 排序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    #endregion

    #region 状态

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    #endregion

    #region 导航属性

    /// <summary>
    /// 关联的数据库连接
    /// </summary>
    [Write(false)]
    public DbConnection? DbConnection { get; set; }

    #endregion
}
