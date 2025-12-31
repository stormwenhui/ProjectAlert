using System.Text.Json;
using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;
using ProjectAlert.Repository;

namespace ProjectAlert.WPF.Services.Executor;

/// <summary>
/// SQL 预警执行器
/// </summary>
public class SqlAlertExecutor : IAlertExecutor
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IDbConnectionRepository _dbConnectionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbConnectionFactory">数据库连接工厂</param>
    /// <param name="dbConnectionRepository">数据库连接仓储</param>
    public SqlAlertExecutor(
        IDbConnectionFactory dbConnectionFactory,
        IDbConnectionRepository dbConnectionRepository)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _dbConnectionRepository = dbConnectionRepository;
    }

    /// <summary>
    /// 执行 SQL 预警检查
    /// </summary>
    /// <param name="rule">预警规则</param>
    /// <returns>执行结果列表</returns>
    public async Task<List<ExecuteResult>> ExecuteAsync(AlertRule rule)
    {
        var results = new List<ExecuteResult>();

        // 获取数据库连接配置
        if (!rule.DbConnectionId.HasValue)
            throw new InvalidOperationException("未配置数据库连接");

        var dbConfig = await _dbConnectionRepository.GetByIdAsync(rule.DbConnectionId.Value);
        if (dbConfig == null)
            throw new InvalidOperationException($"数据库连接配置不存在: {rule.DbConnectionId}");

        // 创建数据库连接
        using var connection = _dbConnectionFactory.CreateConnection(dbConfig);
        connection.Open();

        // 执行 SQL 查询
        var queryResults = await connection.QueryAsync<dynamic>(rule.SqlQuery ?? string.Empty);
        var dataList = queryResults.ToList();

        if (dataList.Count == 0)
            return results;

        // 根据判断类型处理结果
        switch (rule.JudgeType)
        {
            case JudgeType.多行预警:
                // 多行预警 - 每行数据都生成一个预警
                foreach (var row in dataList)
                {
                    var dict = (IDictionary<string, object>)row;
                    var alertKey = GetAlertKey(dict, rule.KeyField);
                    var message = FormatMessage(rule.MessageTemplate, dict);

                    results.Add(new ExecuteResult
                    {
                        AlertKey = alertKey,
                        Message = message,
                        RawData = JsonSerializer.Serialize(dict)
                    });
                }
                break;

            case JudgeType.单值预警:
                // 单值预警 - 检查指定字段的值
                foreach (var row in dataList)
                {
                    var dict = (IDictionary<string, object>)row;
                    if (ShouldAlert(dict, rule))
                    {
                        var alertKey = GetAlertKey(dict, rule.KeyField);
                        var message = FormatMessage(rule.MessageTemplate, dict);

                        results.Add(new ExecuteResult
                        {
                            AlertKey = alertKey,
                            Message = message,
                            RawData = JsonSerializer.Serialize(dict)
                        });
                    }
                }
                break;
        }

        return results;
    }

    /// <summary>
    /// 获取预警唯一标识
    /// </summary>
    /// <param name="data">数据行</param>
    /// <param name="keyField">主键字段名</param>
    /// <returns>唯一标识</returns>
    private string? GetAlertKey(IDictionary<string, object> data, string? keyField)
    {
        if (string.IsNullOrEmpty(keyField))
            return null;

        if (data.TryGetValue(keyField, out var value))
            return value?.ToString();

        return null;
    }

    /// <summary>
    /// 格式化预警消息
    /// </summary>
    /// <param name="template">消息模板</param>
    /// <param name="data">数据行</param>
    /// <returns>格式化后的消息</returns>
    private string FormatMessage(string? template, IDictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template))
            return JsonSerializer.Serialize(data);

        var message = template;
        foreach (var kvp in data)
        {
            // 只支持 {table.列名} 格式
            message = message.Replace($"{{table.{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty);
        }
        return message;
    }

    /// <summary>
    /// 判断是否应该触发预警
    /// </summary>
    /// <param name="data">数据行</param>
    /// <param name="rule">预警规则</param>
    /// <returns>是否触发</returns>
    private bool ShouldAlert(IDictionary<string, object> data, AlertRule rule)
    {
        if (string.IsNullOrEmpty(rule.JudgeField) || !rule.JudgeOperator.HasValue)
            return false;

        if (!data.TryGetValue(rule.JudgeField, out var fieldValue))
            return false;

        var actualValue = Convert.ToDouble(fieldValue);
        var expectedValue = Convert.ToDouble(rule.JudgeValue ?? "0");

        return rule.JudgeOperator.Value switch
        {
            JudgeOperator.等于 => actualValue == expectedValue,
            JudgeOperator.不等于 => actualValue != expectedValue,
            JudgeOperator.大于 => actualValue > expectedValue,
            JudgeOperator.大于等于 => actualValue >= expectedValue,
            JudgeOperator.小于 => actualValue < expectedValue,
            JudgeOperator.小于等于 => actualValue <= expectedValue,
            _ => false
        };
    }
}
