using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Services.Executor;

namespace ProjectAlert.WPF.Services.TaskQueue.Executors;

/// <summary>
/// 预警规则检查执行器
/// </summary>
public class AlertCheckExecutor : ITaskExecutor
{
    private readonly IAlertRuleRepository _alertRuleRepository;
    private readonly ICurrentAlertRepository _currentAlertRepository;
    private readonly IIgnoredAlertRepository _ignoredAlertRepository;
    private readonly SqlAlertExecutor _sqlExecutor;
    private readonly ApiAlertExecutor _apiExecutor;
    private readonly LogService _logService;

    public TaskType SupportedType => TaskType.AlertCheck;

    public AlertCheckExecutor(
        IAlertRuleRepository alertRuleRepository,
        ICurrentAlertRepository currentAlertRepository,
        IIgnoredAlertRepository ignoredAlertRepository,
        SqlAlertExecutor sqlExecutor,
        ApiAlertExecutor apiExecutor,
        LogService logService)
    {
        _alertRuleRepository = alertRuleRepository;
        _currentAlertRepository = currentAlertRepository;
        _ignoredAlertRepository = ignoredAlertRepository;
        _sqlExecutor = sqlExecutor;
        _apiExecutor = apiExecutor;
        _logService = logService;
    }

    public async Task<object?> ExecuteAsync(TaskRequest request)
    {
        if (!request.TargetId.HasValue)
            throw new ArgumentException("AlertCheck 任务需要 TargetId（RuleId）");

        var ruleId = request.TargetId.Value;
        _logService.TaskQueueLog("Executor开始", request.TaskKey, $"AlertCheckExecutor.ExecuteAsync 开始, RuleId={ruleId}");

        var rule = await _alertRuleRepository.GetByIdAsync(ruleId);

        if (rule == null || !rule.Enabled)
        {
            _logService.TaskQueueLog("Executor跳过", request.TaskKey, "规则不存在或未启用");
            return null;
        }

        var sourceTag = rule.SourceType == SourceType.Api ? "API" : "SQL";
        _logService.TaskQueueLog("Executor规则", request.TaskKey, $"规则名称={rule.Name}, 类型={sourceTag}");

        try
        {
            // 根据数据源类型执行检查
            IAlertExecutor executor = rule.SourceType switch
            {
                SourceType.Sql => _sqlExecutor,
                SourceType.Api => _apiExecutor,
                _ => throw new NotSupportedException($"不支持的数据源类型: {rule.SourceType}")
            };

            _logService.TaskQueueLog("Executor查询", request.TaskKey, "开始执行预警检查查询");
            var queryStart = DateTime.Now;

            var results = await executor.ExecuteAsync(rule);

            var queryDuration = DateTime.Now - queryStart;
            _logService.TaskQueueLog("Executor查询完成", request.TaskKey,
                $"查询耗时={queryDuration.TotalMilliseconds:F0}ms, 匹配结果数={results.Count}");

            var newAlertCount = 0;

            // 处理执行结果
            foreach (var result in results)
            {
                // 检查是否被忽略
                if (await _ignoredAlertRepository.IsIgnoredAsync(rule.Id, result.AlertKey))
                    continue;

                // 检查是否是新预警
                var existing = await _currentAlertRepository.GetByRuleAndKeyAsync(rule.Id, result.AlertKey);
                if (existing == null)
                {
                    newAlertCount++;
                }

                // 新增或更新预警
                var alert = new CurrentAlert
                {
                    RuleId = rule.Id,
                    AlertKey = result.AlertKey,
                    Message = result.Message,
                    AlertLevel = rule.AlertLevel,
                    Status = AlertStatus.未处理,
                    FirstTime = DateTime.Now,
                    LastTime = DateTime.Now,
                    OccurCount = 1
                };
                await _currentAlertRepository.UpsertAlertAsync(alert);
            }

            // 更新规则状态
            rule.LastRunTime = DateTime.Now;
            rule.LastRunSuccess = true;
            rule.LastRunResult = $"检测到 {results.Count} 条预警";
            rule.CurrentFailCount = 0;
            await _alertRuleRepository.UpdateAsync(rule);

            _logService.TaskQueueLog("Executor完成", request.TaskKey,
                $"预警数={results.Count}, 新预警={newAlertCount}");

            // 记录执行结果日志
            if (results.Count > 0)
            {
                _logService.Warning(sourceTag, $"[{rule.Name}] 检测到 {results.Count} 条预警");
            }
            else
            {
                _logService.Info(sourceTag, $"[{rule.Name}] 执行成功，无预警");
            }

            return new AlertCheckResult
            {
                RuleId = ruleId,
                RuleName = rule.Name,
                AlertCount = results.Count,
                HasNewAlert = newAlertCount > 0,
                UpdateTime = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logService.TaskQueueLog("Executor失败", request.TaskKey, $"错误={ex.Message}");

            // 记录执行失败
            rule.LastRunTime = DateTime.Now;
            rule.LastRunSuccess = false;
            rule.LastRunResult = ex.Message;
            rule.CurrentFailCount++;
            await _alertRuleRepository.UpdateAsync(rule);

            _logService.Error(sourceTag, $"[{rule.Name}] 执行失败: {ex.Message}");

            throw;
        }
    }
}
