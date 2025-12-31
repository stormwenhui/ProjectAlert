using Dapper;
using Dapper.Contrib.Extensions;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.Repository.Repositories;

/// <summary>
/// 数据库连接仓储实现
/// </summary>
public class DbConnectionRepository : IDbConnectionRepository
{
    private readonly SqliteContext _context;
    private readonly IDbConnectionFactory _factory;

    public DbConnectionRepository(SqliteContext context, IDbConnectionFactory factory)
    {
        _context = context;
        _factory = factory;
    }

    public async Task<DbConnection?> GetByIdAsync(int id)
    {
        using var conn = _context.CreateConnection();
        return await conn.GetAsync<DbConnection>(id);
    }

    public async Task<IEnumerable<DbConnection>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<DbConnection>("SELECT * FROM db_connections ORDER BY id");
    }

    public async Task<IEnumerable<DbConnection>> GetEnabledAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<DbConnection>(
            "SELECT * FROM db_connections WHERE enabled = 1 ORDER BY name");
    }

    public async Task<int> InsertAsync(DbConnection entity)
    {
        entity.CreatedAt = DateTime.Now;
        entity.UpdatedAt = DateTime.Now;

        using var conn = _context.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO db_connections (name, db_type, connection_string, enabled, created_at, updated_at)
            VALUES (@Name, @DbType, @ConnectionString, @Enabled, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
            """, entity);
    }

    public async Task<bool> UpdateAsync(DbConnection entity)
    {
        entity.UpdatedAt = DateTime.Now;

        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            """
            UPDATE db_connections
            SET name = @Name, db_type = @DbType, connection_string = @ConnectionString,
                enabled = @Enabled, updated_at = @UpdatedAt
            WHERE id = @Id
            """, entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _context.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM db_connections WHERE id = @Id", new { Id = id });
        return affected > 0;
    }

    public Task<bool> TestConnectionAsync(DbConnection connection)
    {
        return Task.Run(() =>
        {
            try
            {
                using var conn = _factory.CreateConnection(connection);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }
}
