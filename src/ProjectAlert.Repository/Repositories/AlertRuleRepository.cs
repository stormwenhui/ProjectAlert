using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.Repository.Repositories;

/// <summary>
/// 预警规则仓储实现
/// </summary>
public class AlertRuleRepository : IAlertRuleRepository
{
    private readonly SqliteContext _context;

    public AlertRuleRepository(SqliteContext context)
    {
        _context = context;
    }

    public async Task<AlertRule?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AlertRule>(
            "SELECT * FROM alert_rules WHERE id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<AlertRule>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<AlertRule>("SELECT * FROM alert_rules ORDER BY id");
    }

    public async Task<IEnumerable<AlertRule>> GetEnabledAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<AlertRule>(
            "SELECT * FROM alert_rules WHERE enabled = 1 ORDER BY category, name");
    }

    public async Task<IEnumerable<AlertRule>> GetByCategoryAsync(SystemCategory category)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<AlertRule>(
            "SELECT * FROM alert_rules WHERE category = @Category ORDER BY name",
            new { Category = (int)category });
    }

    public async Task<AlertRule?> GetWithDbConnectionAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var sql = """
            SELECT r.*, c.*
            FROM alert_rules r
            LEFT JOIN db_connections c ON r.db_connection_id = c.id
            WHERE r.id = @Id
            """;

        var result = await conn.QueryAsync<AlertRule, DbConnection, AlertRule>(
            sql,
            (rule, dbConn) =>
            {
                rule.DbConnection = dbConn;
                return rule;
            },
            new { Id = id },
            splitOn: "id");

        return result.FirstOrDefault();
    }

    public async Task<int> InsertAsync(AlertRule entity)
    {
        entity.CreatedAt = DateTime.Now;
        entity.UpdatedAt = DateTime.Now;

        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO alert_rules (
                name, category, source_type,
                db_connection_id, sql_query,
                api_url, api_method, api_headers, api_body, api_timeout,
                judge_level, data_path, judge_type, judge_field, judge_operator, judge_value, key_field,
                alert_level, message_template, cron_expression, fail_threshold, current_fail_count,
                enabled, last_run_time, last_run_success, last_run_result,
                created_at, updated_at
            ) VALUES (
                @Name, @Category, @SourceType,
                @DbConnectionId, @SqlQuery,
                @ApiUrl, @ApiMethod, @ApiHeaders, @ApiBody, @ApiTimeout,
                @JudgeLevel, @DataPath, @JudgeType, @JudgeField, @JudgeOperator, @JudgeValue, @KeyField,
                @AlertLevel, @MessageTemplate, @CronExpression, @FailThreshold, @CurrentFailCount,
                @Enabled, @LastRunTime, @LastRunSuccess, @LastRunResult,
                @CreatedAt, @UpdatedAt
            );
            SELECT last_insert_rowid();
            """, entity);
    }

    public async Task<bool> UpdateAsync(AlertRule entity)
    {
        entity.UpdatedAt = DateTime.Now;

        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE alert_rules SET
                name = @Name, category = @Category, source_type = @SourceType,
                db_connection_id = @DbConnectionId, sql_query = @SqlQuery,
                api_url = @ApiUrl, api_method = @ApiMethod, api_headers = @ApiHeaders,
                api_body = @ApiBody, api_timeout = @ApiTimeout,
                judge_level = @JudgeLevel, data_path = @DataPath, judge_type = @JudgeType,
                judge_field = @JudgeField, judge_operator = @JudgeOperator,
                judge_value = @JudgeValue, key_field = @KeyField,
                alert_level = @AlertLevel, message_template = @MessageTemplate,
                cron_expression = @CronExpression, fail_threshold = @FailThreshold,
                enabled = @Enabled, updated_at = @UpdatedAt
            WHERE id = @Id
            """, entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM alert_rules WHERE id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task UpdateRunStatusAsync(int id, bool success, string? result)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            """
            UPDATE alert_rules SET
                last_run_time = @LastRunTime,
                last_run_success = @Success,
                last_run_result = @Result,
                current_fail_count = CASE WHEN @Success = 1 THEN 0 ELSE current_fail_count + 1 END,
                updated_at = @UpdatedAt
            WHERE id = @Id
            """,
            new
            {
                Id = id,
                Success = success ? 1 : 0,
                Result = result,
                LastRunTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }
}
