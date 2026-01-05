using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.WPF.Services.TaskQueue.Executors;

/// <summary>
/// 预警数据刷新执行器
/// </summary>
public class AlertRefreshExecutor : ITaskExecutor
{
    private readonly ICurrentAlertRepository _repository;
    private readonly LogService _logService;

    public TaskType SupportedType => TaskType.AlertRefresh;

    public AlertRefreshExecutor(ICurrentAlertRepository repository, LogService logService)
    {
        _repository = repository;
        _logService = logService;
    }

    public async Task<object?> ExecuteAsync(TaskRequest request)
    {
        _logService.TaskQueueLog("Executor开始", request.TaskKey, "AlertRefreshExecutor.ExecuteAsync 开始");

        var alerts = await _repository.GetAllWithRuleAsync();
        _logService.TaskQueueLog("Executor查询", request.TaskKey, $"查询完成, 原始数据量={alerts.Count()}");

        // 过滤掉已忽略的预警
        var filtered = alerts.Where(a => a.Status != AlertStatus.已忽略).ToList();
        _logService.TaskQueueLog("Executor过滤", request.TaskKey, $"过滤后数据量={filtered.Count}");

        var result = new AlertRefreshResult
        {
            Alerts = filtered,
            CriticalCount = filtered.Count(a => a.AlertLevel == AlertLevel.严重),
            WarningCount = filtered.Count(a => a.AlertLevel == AlertLevel.警告),
            InfoCount = filtered.Count(a => a.AlertLevel == AlertLevel.信息),
            UpdateTime = DateTime.Now
        };

        _logService.TaskQueueLog("Executor完成", request.TaskKey,
            $"严重={result.CriticalCount}, 警告={result.WarningCount}, 信息={result.InfoCount}");

        return result;
    }
}
