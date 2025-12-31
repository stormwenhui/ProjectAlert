using Microsoft.Extensions.DependencyInjection;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace ProjectAlert.WPF.Services.Scheduler;

/// <summary>
/// Quartz 调度服务
/// </summary>
public class SchedulerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LogService _logService;
    private IScheduler? _scheduler;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SchedulerService(IServiceProvider serviceProvider, LogService logService)
    {
        _serviceProvider = serviceProvider;
        _logService = logService;
    }

    /// <summary>
    /// 启动调度器
    /// </summary>
    public async Task StartAsync()
    {
        var factory = new StdSchedulerFactory();
        _scheduler = await factory.GetScheduler();
        _scheduler.JobFactory = new JobFactory(_serviceProvider);
        await _scheduler.Start();

        _logService.Info("系统", "调度服务已启动");

        // 加载所有启用的规则
        await ReloadRulesAsync();
    }

    /// <summary>
    /// 停止调度器
    /// </summary>
    public async Task StopAsync()
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(true);
            _logService.Info("系统", "调度服务已停止");
        }
    }

    /// <summary>
    /// 重新加载所有规则
    /// </summary>
    public async Task ReloadRulesAsync()
    {
        if (_scheduler == null) return;

        // 清除所有现有任务
        await _scheduler.Clear();

        // 加载启用的规则
        using var scope = _serviceProvider.CreateScope();
        var alertRuleRepo = scope.ServiceProvider.GetRequiredService<IAlertRuleRepository>();
        var rules = await alertRuleRepo.GetEnabledAsync();

        foreach (var rule in rules)
        {
            await ScheduleRuleAsync(rule);
        }

        _logService.Info("系统", $"已加载 {rules.Count()} 条预警规则");
    }

    /// <summary>
    /// 调度单个规则
    /// </summary>
    public async Task ScheduleRuleAsync(AlertRule rule)
    {
        if (_scheduler == null || !rule.Enabled) return;

        var jobKey = new JobKey($"AlertRule_{rule.Id}", "AlertRules");
        var triggerKey = new TriggerKey($"AlertRule_{rule.Id}_Trigger", "AlertRules");

        // 如果已存在，先删除
        if (await _scheduler.CheckExists(jobKey))
        {
            await _scheduler.DeleteJob(jobKey);
        }

        var job = JobBuilder.Create<AlertCheckJob>()
            .WithIdentity(jobKey)
            .UsingJobData("RuleId", rule.Id)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .WithCronSchedule(rule.CronExpression)
            .StartNow()
            .Build();

        await _scheduler.ScheduleJob(job, trigger);
    }

    /// <summary>
    /// 移除规则调度
    /// </summary>
    public async Task UnscheduleRuleAsync(int ruleId)
    {
        if (_scheduler == null) return;

        var jobKey = new JobKey($"AlertRule_{ruleId}", "AlertRules");
        if (await _scheduler.CheckExists(jobKey))
        {
            await _scheduler.DeleteJob(jobKey);
        }
    }

    /// <summary>
    /// 立即执行规则
    /// </summary>
    public async Task TriggerRuleAsync(int ruleId)
    {
        if (_scheduler == null) return;

        var jobKey = new JobKey($"AlertRule_{ruleId}", "AlertRules");
        if (await _scheduler.CheckExists(jobKey))
        {
            await _scheduler.TriggerJob(jobKey);
        }
    }
}

/// <summary>
/// Job 工厂
/// </summary>
public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        return _serviceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob
            ?? throw new InvalidOperationException($"Unable to create job of type {bundle.JobDetail.JobType}");
    }

    public void ReturnJob(IJob job)
    {
        (job as IDisposable)?.Dispose();
    }
}
