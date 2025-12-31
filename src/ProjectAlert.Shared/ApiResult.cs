namespace ProjectAlert.Shared;

/// <summary>
/// API 统一响应结果
/// </summary>
public class ApiResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 响应代码
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 创建成功响应
    /// </summary>
    /// <param name="message">成功消息</param>
    /// <returns>成功的 ApiResult</returns>
    public static ApiResult Ok(string? message = null)
        => new() { Success = true, Code = 0, Message = message ?? "操作成功" };

    /// <summary>
    /// 创建失败响应
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="code">错误代码</param>
    /// <returns>失败的 ApiResult</returns>
    public static ApiResult Fail(string message, int code = -1)
        => new() { Success = false, Code = code, Message = message };

    /// <summary>
    /// 创建带数据的成功响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">响应数据</param>
    /// <param name="message">成功消息</param>
    /// <returns>成功的 ApiResult</returns>
    public static ApiResult<T> Ok<T>(T data, string? message = null)
        => new() { Success = true, Code = 0, Data = data, Message = message ?? "操作成功" };

    /// <summary>
    /// 创建带类型的失败响应
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <param name="code">错误代码</param>
    /// <returns>失败的 ApiResult</returns>
    public static ApiResult<T> Fail<T>(string message, int code = -1)
        => new() { Success = false, Code = code, Message = message };
}

/// <summary>
/// API 统一响应结果（带数据）
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResult<T> : ApiResult
{
    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据项类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 数据项列表
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// 总记录数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
