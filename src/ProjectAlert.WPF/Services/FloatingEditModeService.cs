namespace ProjectAlert.WPF.Services;

/// <summary>
/// 浮窗编辑模式服务
/// 管理全局编辑模式状态，控制浮窗的交互能力
/// </summary>
public class FloatingEditModeService
{
    private bool _isEditMode;

    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (_isEditMode != value)
            {
                _isEditMode = value;
                EditModeChanged?.Invoke(this, value);
            }
        }
    }

    /// <summary>
    /// 编辑模式变化事件
    /// </summary>
    public event EventHandler<bool>? EditModeChanged;

    /// <summary>
    /// 进入编辑模式
    /// </summary>
    public void EnterEditMode()
    {
        IsEditMode = true;
    }

    /// <summary>
    /// 退出编辑模式
    /// </summary>
    public void ExitEditMode()
    {
        IsEditMode = false;
    }

    /// <summary>
    /// 切换编辑模式
    /// </summary>
    public void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
    }
}
