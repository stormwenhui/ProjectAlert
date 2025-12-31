using ProjectAlert.Domain.Entities;

namespace ProjectAlert.WPF.Services.Executor;

/// <summary>
/// 预警执行器接口
/// </summary>
public interface IAlertExecutor
{
    /// <summary>
    /// 执行预警检查
    /// </summary>
    /// <param name="rule">预警规则</param>
    /// <returns>执行结果列表</returns>
    Task<List<ExecuteResult>> ExecuteAsync(AlertRule rule);
}

/// <summary>
/// 执行结果
/// </summary>
public class ExecuteResult
{
    /// <summary>
    /// 预警唯一标识（用于区分多行数据的预警）
    /// </summary>
    public string? AlertKey { get; set; }

    /// <summary>
    /// 预警消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 原始数据（JSON格式）
    /// </summary>
    public string? RawData { get; set; }
}
