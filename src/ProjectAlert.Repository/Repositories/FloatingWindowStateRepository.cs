using Dapper;
using ProjectAlert.Domain.Entities;
using ProjectAlert.Domain.Interfaces;

namespace ProjectAlert.Repository.Repositories;

/// <summary>
/// 浮窗状态仓储实现
/// </summary>
public class FloatingWindowStateRepository : IFloatingWindowStateRepository
{
    private readonly SqliteContext _context;

    public FloatingWindowStateRepository(SqliteContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FloatingWindowState>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<FloatingWindowState>(
            """
            SELECT
                window_id as WindowId,
                is_visible as IsVisible,
                left as Left,
                top as Top,
                width as Width,
                height as Height,
                opacity as Opacity,
                is_locked as IsLocked,
                is_topmost as IsTopmost,
                updated_at as UpdatedAt
            FROM floating_window_states
            """);
    }

    public async Task<FloatingWindowState?> GetByIdAsync(string windowId)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<FloatingWindowState>(
            """
            SELECT
                window_id as WindowId,
                is_visible as IsVisible,
                left as Left,
                top as Top,
                width as Width,
                height as Height,
                opacity as Opacity,
                is_locked as IsLocked,
                is_topmost as IsTopmost,
                updated_at as UpdatedAt
            FROM floating_window_states
            WHERE window_id = @WindowId
            """,
            new { WindowId = windowId });
    }

    public async Task SaveAsync(FloatingWindowState state)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO floating_window_states
                (window_id, is_visible, left, top, width, height, opacity, is_locked, is_topmost, updated_at)
            VALUES
                (@WindowId, @IsVisible, @Left, @Top, @Width, @Height, @Opacity, @IsLocked, @IsTopmost, @UpdatedAt)
            ON CONFLICT(window_id) DO UPDATE SET
                is_visible = @IsVisible,
                left = @Left,
                top = @Top,
                width = @Width,
                height = @Height,
                opacity = @Opacity,
                is_locked = @IsLocked,
                is_topmost = @IsTopmost,
                updated_at = @UpdatedAt
            """,
            new
            {
                state.WindowId,
                state.IsVisible,
                state.Left,
                state.Top,
                state.Width,
                state.Height,
                state.Opacity,
                state.IsLocked,
                state.IsTopmost,
                UpdatedAt = DateTime.Now
            });
    }

    public async Task DeleteAsync(string windowId)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM floating_window_states WHERE window_id = @WindowId",
            new { WindowId = windowId });
    }
}
