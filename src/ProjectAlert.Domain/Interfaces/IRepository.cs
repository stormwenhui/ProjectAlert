namespace ProjectAlert.Domain.Interfaces;

/// <summary>
/// 通用仓储接口
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>实体对象，不存在则返回null</returns>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <returns>实体集合</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// 插入新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns>新插入的实体ID</returns>
    Task<int> InsertAsync(T entity);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateAsync(T entity);

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(int id);
}
