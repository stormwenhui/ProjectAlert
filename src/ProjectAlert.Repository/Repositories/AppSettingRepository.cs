using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.Repository.Repositories;

/// <summary>
/// 应用设置仓储实现
/// </summary>
public class AppSettingRepository : IAppSettingRepository
{
    private readonly SqliteContext _context;

    public AppSettingRepository(SqliteContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppSetting>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<AppSetting>("SELECT key as KeyName, value as Value FROM app_settings");
    }

    public async Task<AppSetting?> GetByKeyAsync(string key)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AppSetting>(
            "SELECT key as KeyName, value as Value FROM app_settings WHERE key = @Key",
            new { Key = key });
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await GetByKeyAsync(key);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, string? description = null)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO app_settings (key, value) VALUES (@Key, @Value)
            ON CONFLICT(key) DO UPDATE SET value = @Value
            """,
            new { Key = key, Value = value });
    }
}
