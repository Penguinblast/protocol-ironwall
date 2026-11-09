using Leap;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Runtime.InteropServices;

public class HandGestureTracker : MonoBehaviour
{
    [Header("Leap 数据源")]
    public LeapProvider leapProvider;

    [Header("参考对象")]
    [Tooltip("Hero 对象，用于计算手掌相对坐标")]
    public Transform heroTransform;

    [Header("握拳点击设置")]
    [Range(0, 1)] public float grabThreshold = 0.8f;

    [Header("左手手掌映射设置（相对于 Hero）")]
    [Tooltip("手掌 X 坐标有效范围（米），相对于 Hero")]
    public Vector2 palmXRange = new Vector2(-0.3f, 0.3f);
    [Tooltip("手掌 Y 坐标有效范围（米），相对于 Hero")]
    public Vector2 palmYRange = new Vector2(0.0f, 0.5f);
    [Tooltip("手掌 Z 坐标有效范围（米），相对于 Hero（用于计算到 Hero 的距离）")]
    public Vector2 palmZRange = new Vector2(0.2f, 0.8f);
    [Tooltip("开启后手掌向上移动 → 光标向下移动")]
    public bool invertY = true;
    [Tooltip("是否反转 X 轴")]
    public bool invertX = false;

    [Header("光标边界预留")]
    [Tooltip("屏幕边界预留比例（0~0.2），比如0.05=留5%边界，避免光标触顶/触底")]
    [Range(0f, 0.2f)] public float borderMargin = 0.05f;

    [Header("光标防抖设置")]
    [Tooltip("启用光标移动防抖")]
    public bool enableAntiShake = true;
    [Tooltip("手掌移动速度阈值（单位：米/秒），低于此值不移动光标")]
    [Range(0.001f, 0.05f)] public float moveSpeedThreshold = 0.008f;

    [Header("鼠标控制")]
    public bool enableMouseControl = true;
    public bool debugLogPalmPos = false;

    // ==================== 方案一：动态校准 ====================
    [Header("动态校准设置")]
    [Tooltip("是否在游戏启动时自动校准 Y 轴范围（推荐开启）")]
    public bool calibrateOnStart = true;
    [Tooltip("校准持续时间（秒），期间请将手掌在可活动范围内上下移动")]
    public float calibrationDuration = 2f;
    [Tooltip("是否在校准完成后将原始 palmYRange 替换为校准值")]
    public bool overrideOriginalRange = true;   // 为 true 则直接修改 palmYRange
    private bool isCalibrating = false;

    // Windows API
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
    private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    private const uint MOUSEEVENTF_LEFTUP = 0x04;

    private bool leftIsGrabbing = false;
    private bool previousLeftGrabbing;
    private Mouse _mouse;

    // 防抖专用变量
    private Vector3 _lastPalmPosition;
    private bool _hasLastPosition = false;

    void Start()
    {
        if (leapProvider == null)
            leapProvider = FindObjectOfType<LeapProvider>();
        _mouse = Mouse.current;
        if (_mouse == null)
        {
            Debug.LogWarning("未检测到鼠标设备！");
            enableMouseControl = false;
        }

        if (calibrateOnStart && heroTransform != null && leapProvider != null)
        {
            StartCoroutine(CalibrateYRange());
        }
    }

    void Update()
    {
        Frame frame = leapProvider?.CurrentFrame;
        if (frame == null) return;

        HandleLeftHand(frame);
        HandleRightHand(frame);
    }

    private void HandleRightHand(Frame frame)
    {
        // 右手占位（保留）
    }

    private void HandleLeftHand(Frame frame)
    {
        if (!enableMouseControl || _mouse == null) return;

        Hand leftHand = frame.GetHand(Chirality.Left);
        if (leftHand == null)
        {
            _hasLastPosition = false;
            return;
        }

        // 获取当前手掌坐标（世界坐标）
        Vector3 palmWorldPos = leftHand.PalmPosition;

        // 转换为相对于 Hero 的坐标
        Vector3 palmRelativePos = palmWorldPos;
        if (heroTransform != null)
        {
            palmRelativePos = heroTransform.InverseTransformPoint(palmWorldPos);
        }

        if (debugLogPalmPos)
            Debug.Log($"左掌坐标 [世界]: X={palmWorldPos.x:F3}, Y={palmWorldPos.y:F3}, Z={palmWorldPos.z:F3}" +
                     $" [相对Hero]: X={palmRelativePos.x:F3}, Y={palmRelativePos.y:F3}, Z={palmRelativePos.z:F3}");

        Vector3 palmPos = palmRelativePos;

        // 光标移动（如果正在校准，可选择性禁用移动，这里让校准期间仍然移动但使用旧范围，不影响校准数据采集）
        if (enableAntiShake)
        {
            if (_hasLastPosition)
            {
                float moveSpeed = Vector3.Distance(palmPos, _lastPalmPosition) / Time.deltaTime;
                if (moveSpeed > moveSpeedThreshold)
                {
                    _mouse.WarpCursorPosition(MapPalmToScreen(palmPos));
                }
            }
            else
            {
                _mouse.WarpCursorPosition(MapPalmToScreen(palmPos));
            }

            _lastPalmPosition = palmPos;
            _hasLastPosition = true;
        }
        else
        {
            _mouse.WarpCursorPosition(MapPalmToScreen(palmPos));
        }

        // 左手握拳 → 鼠标左键
        leftIsGrabbing = leftHand.GrabStrength > grabThreshold;
        if (leftIsGrabbing != previousLeftGrabbing)
        {
            SetMouseLeftButtonState(leftIsGrabbing ? 1 : 0);
            previousLeftGrabbing = leftIsGrabbing;
        }
    }

    private Vector2 MapPalmToScreen(Vector3 palmPos)
    {
        float clampedX = Mathf.Clamp(palmPos.x, palmXRange.x, palmXRange.y);
        float clampedY = Mathf.Clamp(palmPos.y, palmYRange.x, palmYRange.y);

        float nx = (clampedX - palmXRange.x) / (palmXRange.y - palmXRange.x);
        float ny = (clampedY - palmYRange.x) / (palmYRange.y - palmYRange.x);

        nx = Mathf.Lerp(borderMargin, 1 - borderMargin, nx);
        ny = Mathf.Lerp(borderMargin, 1 - borderMargin, ny);

        if (invertX) nx = 1f - nx;
        if (invertY) ny = 1f - ny;

        return new Vector2(nx * Screen.width, ny * Screen.height);
    }

    private void SetMouseLeftButtonState(int state)
    {
        if (state == 1)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Debug.Log("[左手] 握拳 -> 鼠标左键：按下");
        }
        else if (state == 0)
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            Debug.Log("[左手] 松开 -> 鼠标左键：释放");
        }
    }

    // ==================== 动态校准协程 ====================
    private IEnumerator CalibrateYRange()
    {
        if (heroTransform == null || leapProvider == null)
        {
            Debug.LogError("校准失败：heroTransform 或 leapProvider 为空！");
            yield break;
        }

        isCalibrating = true;
        Debug.Log("开始校准左手手掌 Y 轴范围，请在 " + calibrationDuration + " 秒内上下移动手掌（覆盖最低点到最高点）...");

        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float elapsed = 0f;

        while (elapsed < calibrationDuration)
        {
            Frame frame = leapProvider.CurrentFrame;
            Hand left = frame?.GetHand(Chirality.Left);
            if (left != null)
            {
                Vector3 relPos = heroTransform.InverseTransformPoint(left.PalmPosition);
                float y = relPos.y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (minY < maxY)
        {
            // 给范围加一点缓冲（10%），避免运动中刚好卡边界
            float margin = (maxY - minY) * 0.1f;
            minY -= margin;
            maxY += margin;

            if (overrideOriginalRange)
            {
                palmYRange = new Vector2(minY, maxY);
                Debug.Log($"校准完成！已更新 palmYRange 为 [{minY:F3}, {maxY:F3}]");
            }
            else
            {
                Debug.Log($"校准完成！建议设置 palmYRange = [{minY:F3}, {maxY:F3}]");
            }
        }
        else
        {
            Debug.LogWarning("校准期间未检测到有效的左手数据，Y 轴范围未更新。");
        }

        isCalibrating = false;
    }

    // 可选：提供一个公共方法，允许运行时手动触发校准（例如按 F2 重新校准）
    public void ManualCalibrate()
    {
        if (!isCalibrating)
            StartCoroutine(CalibrateYRange());
    }
}