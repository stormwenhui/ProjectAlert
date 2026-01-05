namespace ProjectAlert.WPF.Services.TaskQueue;

/// <summary>
/// 任务执行器接口
/// </summary>
public interface ITaskExecutor
{
    /// <summary>
    /// 支持的任务类型
    /// </summary>
    TaskType SupportedType { get; }

    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="request">任务请求</param>
    /// <returns>执行结果数据</returns>
    Task<object?> ExecuteAsync(TaskRequest request);
}
