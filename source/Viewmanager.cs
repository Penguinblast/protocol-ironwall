using UnityEngine;

/// <summary>
/// 全局视角状态管理器（单例 + 静态变量）
/// 所有脚本均可通过 ViewStateManager.CurrentView 读取当前视角（0=第一视角，1=第二视角）
/// 当 G 键按下时调用 ViewStateManager.ToggleView() 切换状态，并自动启用/禁用 SkillController
/// </summary>
public class ViewStateManager : MonoBehaviour
{
    private static int _currentView = 0;  // 0 = 第一视角（技能禁用），1 = 第二视角（技能启用）

    /// <summary>
    /// 当前视角：0=第一视角，1=第二视角
    /// </summary>
    public static int CurrentView
    {
        get => _currentView;
        private set
        {
            if (_currentView == value) return;
            _currentView = value;
            OnViewChanged();
        }
    }

    private static ViewStateManager _instance;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 切换视角（由 G 键触发）
    /// </summary>
    public static void ToggleView()
    {
        CurrentView = (CurrentView == 0) ? 1 : 0;
        Debug.Log($"[ViewStateManager] 视角切换 -> {(CurrentView == 0 ? "第一视角（技能禁用）" : "第二视角（技能启用）")}");
    }

    // 当视角改变时，自动启用/禁用 SkillController
    private static void OnViewChanged()
    {
        SkillController sc = FindObjectOfType<SkillController>();
        if (sc != null)
        {
            bool shouldEnable = (CurrentView == 1);
            if (sc.enabled != shouldEnable)
            {
                sc.enabled = shouldEnable;
                Debug.Log($"[ViewStateManager] SkillController 已{(shouldEnable ? "启用" : "禁用")}");
            }
        }
        else
        {
            Debug.LogWarning("[ViewStateManager] 未找到 SkillController 组件，请确保场景中存在 SkillController");
        }
    }
}