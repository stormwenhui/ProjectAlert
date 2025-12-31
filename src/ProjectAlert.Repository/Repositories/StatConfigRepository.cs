using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Enums;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.Repository.Repositories;

/// <summary>
/// 统计配置仓储实现
/// </summary>
public class StatConfigRepository : IStatConfigRepository
{
    private readonly SqliteContext _context;

    public StatConfigRepository(SqliteContext context)
    {
        _context = context;
    }

    public async Task<StatConfig?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<StatConfig>(
            "SELECT * FROM stat_configs WHERE id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<StatConfig>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<StatConfig>("SELECT * FROM stat_configs ORDER BY sort_order, id");
    }

    public async Task<IEnumerable<StatConfig>> GetEnabledAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<StatConfig>(
            "SELECT * FROM stat_configs WHERE enabled = 1 ORDER BY sort_order, name");
    }

    public async Task<IEnumerable<StatConfig>> GetByCategoryAsync(SystemCategory category)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<StatConfig>(
            "SELECT * FROM stat_configs WHERE category = @Category ORDER BY sort_order, name",
            new { Category = (int)category });
    }

    public async Task<int> InsertAsync(StatConfig entity)
    {
        entity.CreatedAt = DateTime.Now;
        entity.UpdatedAt = DateTime.Now;

        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO stat_configs (
                name, category, source_type,
                db_connection_id, sql_query,
                api_url, api_method, api_headers, api_body, api_timeout, data_path,
                chart_type, chart_config, refresh_interval, sort_order,
                enabled, created_at, updated_at
            ) VALUES (
                @Name, @Category, @SourceType,
                @DbConnectionId, @SqlQuery,
                @ApiUrl, @ApiMethod, @ApiHeaders, @ApiBody, @ApiTimeout, @DataPath,
                @ChartType, @ChartConfig, @RefreshInterval, @SortOrder,
                @Enabled, @CreatedAt, @UpdatedAt
            );
            SELECT last_insert_rowid();
            """, entity);
    }

    public async Task<bool> UpdateAsync(StatConfig entity)
    {
        entity.UpdatedAt = DateTime.Now;

        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE stat_configs SET
                name = @Name, category = @Category, source_type = @SourceType,
                db_connection_id = @DbConnectionId, sql_query = @SqlQuery,
                api_url = @ApiUrl, api_method = @ApiMethod, api_headers = @ApiHeaders,
                api_body = @ApiBody, api_timeout = @ApiTimeout, data_path = @DataPath,
                chart_type = @ChartType, chart_config = @ChartConfig,
                refresh_interval = @RefreshInterval, sort_order = @SortOrder,
                enabled = @Enabled, updated_at = @UpdatedAt
            WHERE id = @Id
            """, entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM stat_configs WHERE id = @Id", new { Id = id });
        return affected > 0;
    }
}
