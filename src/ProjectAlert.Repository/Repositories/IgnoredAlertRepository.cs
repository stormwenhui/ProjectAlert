using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.Repository.Repositories;

/// <summary>
/// 已忽略预警仓储实现
/// </summary>
public class IgnoredAlertRepository : IIgnoredAlertRepository
{
    private readonly SqliteContext _context;

    public IgnoredAlertRepository(SqliteContext context)
    {
        _context = context;
    }

    public async Task<IgnoredAlert?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<IgnoredAlert>(
            "SELECT * FROM ignored_alerts WHERE id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<IgnoredAlert>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<IgnoredAlert>(
            "SELECT * FROM ignored_alerts ORDER BY ignored_at DESC");
    }

    public async Task<IEnumerable<IgnoredAlert>> GetByRuleIdAsync(int ruleId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<IgnoredAlert>(
            "SELECT * FROM ignored_alerts WHERE rule_id = @RuleId",
            new { RuleId = ruleId });
    }

    public async Task<bool> IsIgnoredAsync(int ruleId, string? alertKey)
    {
        using var conn = _context.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM ignored_alerts
            WHERE rule_id = @RuleId
            AND (alert_key = @AlertKey OR (alert_key IS NULL AND @AlertKey IS NULL))
            """,
            new { RuleId = ruleId, AlertKey = alertKey });
        return count > 0;
    }

    public async Task<int> InsertAsync(IgnoredAlert entity)
    {
        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO ignored_alerts (rule_id, alert_key, ignored_at)
            VALUES (@RuleId, @AlertKey, @IgnoredAt);
            SELECT last_insert_rowid();
            """, entity);
    }

    public async Task<bool> UpdateAsync(IgnoredAlert entity)
    {
        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE ignored_alerts SET
                rule_id = @RuleId, alert_key = @AlertKey, ignored_at = @IgnoredAt
            WHERE id = @Id
            """, entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM ignored_alerts WHERE id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task AddAsync(IgnoredAlert ignoredAlert)
    {
        await InsertAsync(ignoredAlert);
    }

    public async Task RemoveAsync(int id)
    {
        await DeleteAsync(id);
    }

    public async Task RemoveByRuleAndKeyAsync(int ruleId, string? alertKey)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            """
            DELETE FROM ignored_alerts
            WHERE rule_id = @RuleId
            AND (alert_key = @AlertKey OR (alert_key IS NULL AND @AlertKey IS NULL))
            """,
            new { RuleId = ruleId, AlertKey = alertKey });
    }
}
