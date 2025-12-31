using Microsoft.Extensions.DependencyInjection;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.WPF.Services.Executor;
using Quartz;

namespace ProjectAlert.WPF.Services.Scheduler;

/// <summary>
/// 预警检查任务
/// </summary>
public class AlertCheckJob : IJob
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AlertCheckJob(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 执行任务
    /// </summary>
    public async Task Execute(IJobExecutionContext context)
    {
        var ruleId = context.JobDetail.JobDataMap.GetInt("RuleId");

        using var scope = _serviceProvider.CreateScope();
        var alertRuleRepo = scope.ServiceProvider.GetRequiredService<IAlertRuleRepository>();
        var currentAlertRepo = scope.ServiceProvider.GetRequiredService<ICurrentAlertRepository>();
        var ignoredAlertRepo = scope.ServiceProvider.GetRequiredService<IIgnoredAlertRepository>();
        var logService = scope.ServiceProvider.GetRequiredService<LogService>();

        var rule = await alertRuleRepo.GetByIdAsync(ruleId);
        if (rule == null || !rule.Enabled) return;

        var sourceTag = rule.SourceType == SourceType.Api ? "API" : "SQL";
        logService.Debug("定时任务", $"开始执行规则检查: {rule.Name}");

        try
        {
            // 根据数据源类型执行检查
            IAlertExecutor executor = rule.SourceType switch
            {
                SourceType.Sql => scope.ServiceProvider.GetRequiredService<SqlAlertExecutor>(),
                SourceType.Api => scope.ServiceProvider.GetRequiredService<ApiAlertExecutor>(),
                _ => throw new NotSupportedException($"不支持的数据源类型: {rule.SourceType}")
            };

            var results = await executor.ExecuteAsync(rule);

            // 收集当前活跃的预警Key
            var activeKeys = new List<string?>();

            // 处理执行结果
            foreach (var result in results)
            {
                activeKeys.Add(result.AlertKey);

                // 检查是否被忽略
                if (await ignoredAlertRepo.IsIgnoredAsync(rule.Id, result.AlertKey))
                    continue;

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
                await currentAlertRepo.UpsertAlertAsync(alert);
            }

            // 移除已恢复的预警
            await currentAlertRepo.RemoveRecoveredAlertsAsync(rule.Id, activeKeys);

            // 更新规则状态
            rule.LastRunTime = DateTime.Now;
            rule.LastRunSuccess = true;
            rule.LastRunResult = $"检测到 {results.Count} 条预警";
            rule.CurrentFailCount = 0;
            await alertRuleRepo.UpdateAsync(rule);

            // 记录执行结果日志
            if (results.Count > 0)
            {
                logService.Warning(sourceTag, $"[{rule.Name}] 检测到 {results.Count} 条预警");
            }
            else
            {
                logService.Info(sourceTag, $"[{rule.Name}] 执行成功，无预警");
            }
        }
        catch (Exception ex)
        {
            // 记录执行失败
            rule.LastRunTime = DateTime.Now;
            rule.LastRunSuccess = false;
            rule.LastRunResult = ex.Message;
            rule.CurrentFailCount++;
            await alertRuleRepo.UpdateAsync(rule);

            logService.Error(sourceTag, $"[{rule.Name}] 执行失败: {ex.Message}");
        }
    }
}
