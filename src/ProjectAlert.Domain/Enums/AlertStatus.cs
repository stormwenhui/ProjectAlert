namespace ProjectAlert.Domain.Enums;

/// <summary>
/// 预警处理状态
/// </summary>
public enum AlertStatus
{
    /// <summary>
    /// 未处理
    /// </summary>
    未处理 = 1,

    /// <summary>
    /// 处理中
    /// </summary>
    处理中 = 2,

    /// <summary>
    /// 已忽略
    /// </summary>
    已忽略 = 3,

    /// <summary>
    /// 已恢复
    /// </summary>
    已恢复 = 4
}
