using System.ComponentModel;

namespace ProjectAlert.Domain.Enums;

/// <summary>
/// 系统分类
/// </summary>
public enum SystemCategory
{
    /// <summary>
    /// 药入库
    /// </summary>
    [Description("药入库")]
    药入库 = 1,

    /// <summary>
    /// 抽奖
    /// </summary>
    [Description("抽奖")]
    抽奖 = 2,

    /// <summary>
    /// 采销
    /// </summary>
    [Description("采销")]
    采销 = 3,

    /// <summary>
    /// 电销
    /// </summary>
    [Description("电销")]
    电销 = 4,

}
