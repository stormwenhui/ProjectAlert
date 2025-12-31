using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.Repository.Repositories;

/// <summary>
/// 当前预警仓储实现
/// </summary>
public class CurrentAlertRepository : ICurrentAlertRepository
{
    private readonly SqliteContext _context;

    public CurrentAlertRepository(SqliteContext context)
    {
        _context = context;
    }

    public async Task<CurrentAlert?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CurrentAlert>(
            "SELECT * FROM current_alerts WHERE id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<CurrentAlert>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<CurrentAlert>("SELECT * FROM current_alerts ORDER BY last_time DESC");
    }

    public async Task<CurrentAlert?> GetByRuleAndKeyAsync(int ruleId, string? alertKey)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CurrentAlert>(
            """
            SELECT * FROM current_alerts
            WHERE rule_id = @RuleId
            AND (alert_key = @AlertKey OR (alert_key IS NULL AND @AlertKey IS NULL))
            """,
            new { RuleId = ruleId, AlertKey = alertKey });
    }

    public async Task<IEnumerable<CurrentAlert>> GetActiveAlertsAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<CurrentAlert>(
            """
            SELECT * FROM current_alerts
            WHERE status IN (0, 1)
            ORDER BY
                CASE alert_level WHEN 2 THEN 0 WHEN 1 THEN 1 ELSE 2 END,
                last_time DESC
            """);
    }

    public async Task<IEnumerable<CurrentAlert>> GetByRuleIdAsync(int ruleId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<CurrentAlert>(
            "SELECT * FROM current_alerts WHERE rule_id = @RuleId",
            new { RuleId = ruleId });
    }

    public async Task<int> InsertAsync(CurrentAlert entity)
    {
        entity.CreatedAt = DateTime.Now;
        entity.UpdatedAt = DateTime.Now;

        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO current_alerts (
                rule_id, alert_key, message, alert_level, status,
                first_time, last_time, occur_count, created_at, updated_at
            ) VALUES (
                @RuleId, @AlertKey, @Message, @AlertLevel, @Status,
                @FirstTime, @LastTime, @OccurCount, @CreatedAt, @UpdatedAt
            );
            SELECT last_insert_rowid();
            """, entity);
    }

    public async Task<bool> UpdateAsync(CurrentAlert entity)
    {
        entity.UpdatedAt = DateTime.Now;

        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE current_alerts SET
                message = @Message, alert_level = @AlertLevel, status = @Status,
                last_time = @LastTime, occur_count = @OccurCount, updated_at = @UpdatedAt
            WHERE id = @Id
            """, entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM current_alerts WHERE id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task UpdateStatusAsync(int id, AlertStatus status, string changedBy)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            """
            UPDATE current_alerts SET
                status = @Status, updated_at = @UpdatedAt
            WHERE id = @Id
            """,
            new { Id = id, Status = (int)status, UpdatedAt = DateTime.Now });
    }

    public async Task UpsertAlertAsync(CurrentAlert alert)
    {
        var existing = await GetByRuleAndKeyAsync(alert.RuleId, alert.AlertKey);

        if (existing != null)
        {
            existing.Message = alert.Message;
            existing.AlertLevel = alert.AlertLevel;
            existing.LastTime = alert.LastTime;
            existing.OccurCount++;
            await UpdateAsync(existing);
        }
        else
        {
            await InsertAsync(alert);
        }
    }

    public async Task RemoveRecoveredAlertsAsync(int ruleId, IEnumerable<string?> activeKeys)
    {
        using var conn = _context.CreateConnection();
        var keyList = activeKeys.ToList();

        if (keyList.Count == 0)
        {
            await conn.ExecuteAsync(
                "DELETE FROM current_alerts WHERE rule_id = @RuleId AND status != 2",
                new { RuleId = ruleId });
        }
        else
        {
            var nonNullKeys = keyList.Where(k => k != null).ToList();
            if (nonNullKeys.Count > 0)
            {
                await conn.ExecuteAsync(
                    """
                    DELETE FROM current_alerts
                    WHERE rule_id = @RuleId AND status != 2
                    AND (alert_key NOT IN @Keys OR alert_key IS NULL)
                    """,
                    new { RuleId = ruleId, Keys = nonNullKeys });
            }
        }
    }

    public async Task<IEnumerable<CurrentAlert>> GetAllWithRuleAsync()
    {
        using var conn = _context.CreateConnection();
        var sql = """
            SELECT ca.*, ar.*
            FROM current_alerts ca
            LEFT JOIN alert_rules ar ON ca.rule_id = ar.id
            ORDER BY
                CASE ca.alert_level WHEN 3 THEN 0 WHEN 2 THEN 1 ELSE 2 END,
                ca.last_time DESC
            """;

        var alertDict = new Dictionary<int, CurrentAlert>();
        await conn.QueryAsync<CurrentAlert, AlertRule?, CurrentAlert>(
            sql,
            (alert, rule) =>
            {
                if (!alertDict.TryGetValue(alert.Id, out var existingAlert))
                {
                    existingAlert = alert;
                    alertDict.Add(alert.Id, existingAlert);
                }
                existingAlert.Rule = rule;
                return existingAlert;
            },
            splitOn: "id");

        return alertDict.Values;
    }
}
