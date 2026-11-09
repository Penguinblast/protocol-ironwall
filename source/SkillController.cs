using UnityEngine;
using Leap;
using System;
using System.Collections;
using System.Runtime.InteropServices;

#region ===================== 输入模拟器 =====================

public static class InputSimulator
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static void PressKey(byte key, MonoBehaviour host, float duration = 0.05f)
    {
        keybd_event(key, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
        host.StartCoroutine(ReleaseAfter(key, duration));
    }

    private static IEnumerator ReleaseAfter(byte key, float delay)
    {
        yield return new WaitForSeconds(delay);
        keybd_event(key, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }

    public static void KeyDown(byte key)
    {
        keybd_event(key, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
    }

    public static void KeyUp(byte key)
    {
        keybd_event(key, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }
}

#endregion

#region ===================== 数据结构 =====================

public struct HandData
{
    public bool leftValid;
    public bool rightValid;

    public Vector3 leftPalm;
    public Vector3 rightPalm;

    public float leftPinch;
    public float rightPinch;

    public Vector3 leftPalmNormal;
    public Vector3 rightPalmNormal;
}

#endregion

#region ===================== 手势：Push (J) 向前推 =====================

public class PushGesture
{
    public float distance = 0.12f;
    public float timeWindow = 0.3f;

    private Vector3 leftStart;
    private Vector3 rightStart;
    private float timer;
    private bool recording;

    public bool Update(HandData data)
    {
        if (!data.leftValid || !data.rightValid)
        {
            recording = false;
            return false;
        }

        if (!recording)
        {
            leftStart = data.leftPalm;
            rightStart = data.rightPalm;
            timer = 0;
            recording = true;
            return false;
        }

        timer += Time.deltaTime;

        float l = data.leftPalm.z - leftStart.z;
        float r = data.rightPalm.z - rightStart.z;

        if (timer <= timeWindow && l > distance && r > distance)
        {
            recording = false;
            return true;
        }

        if (timer > timeWindow)
            recording = false;

        return false;
    }
}

#endregion

#region ===================== 手势：Press (Space) 向下压 =====================

public class PressGesture
{
    public float distance = 0.08f;
    public float timeWindow = 0.25f;

    private Vector3 leftStart;
    private Vector3 rightStart;
    private float timer;
    private bool recording;

    public bool Update(HandData data)
    {
        if (!data.leftValid || !data.rightValid)
        {
            recording = false;
            return false;
        }

        if (!recording)
        {
            leftStart = data.leftPalm;
            rightStart = data.rightPalm;
            timer = 0;
            recording = true;
            return false;
        }

        timer += Time.deltaTime;

        float l = leftStart.y - data.leftPalm.y;
        float r = rightStart.y - data.rightPalm.y;

        if (timer <= timeWindow && l > distance && r > distance)
        {
            recording = false;
            return true;
        }

        if (timer > timeWindow)
            recording = false;

        return false;
    }
}

#endregion

#region ===================== 主控制器 =====================

public class SkillController : MonoBehaviour
{
    public LeapProvider leapProvider;

    [Header("捏合持续按键 (K)")]
    [Range(0, 1)] public float pinchThreshold = 0.7f;

    [Header("手掌向上持续按键 (Q / E)")]
    [Range(0, 1)] public float palmUpThreshold = 0.6f;

    // ========== 角色持续移动 ==========
    [Header("双手同时掌心向上 → 持续向前移动")]
    public Transform characterTransform;      // 需要移动的角色 Transform
    public float forwardSpeed = 5f;           // 移动速度（米/秒）

    private PushGesture push = new PushGesture();
    private PressGesture press = new PressGesture();

    // 持续按键状态
    private bool isKPressed = false;
    private bool isQPressed = false;
    private bool isEPressed = false;

    // 持续移动状态
    private bool isMovingForward = false;

    void Start()
    {
        // 根据全局状态初始化自身启用/禁用（确保与 ViewStateManager 同步）
        bool shouldEnable = (ViewStateManager.CurrentView == 1);
        if (enabled != shouldEnable)
            enabled = shouldEnable;

        if (characterTransform == null)
        {
            Debug.LogWarning("SkillController: characterTransform 未赋值，双手掌心向上将无法移动角色。");
        }
    }

    void OnEnable()
    {
        // 每次启用时重置状态
        isKPressed = false;
        isQPressed = false;
        isEPressed = false;
        isMovingForward = false;
    }

    void OnDisable()
    {
        // 释放所有持续按下的按键
        if (isKPressed) { InputSimulator.KeyUp(0x4B); isKPressed = false; }
        if (isQPressed) { InputSimulator.KeyUp(0x51); isQPressed = false; }
        if (isEPressed) { InputSimulator.KeyUp(0x45); isEPressed = false; }
        
        // 停止移动
        isMovingForward = false;
    }

    void Update()
    {
        Frame frame = leapProvider?.CurrentFrame;
        if (frame == null) return;

        HandData data = GetHandData(frame);

        // 一次性手势：向前推 (J)
        if (push.Update(data))
            InputSimulator.PressKey(0x4A, this);

        // 一次性手势：向下压 (Space)
        if (press.Update(data))
            InputSimulator.PressKey(0x20, this);

        // 持续 K：双手捏合
        bool shouldK = data.leftValid && data.rightValid &&
                       data.leftPinch > pinchThreshold &&
                       data.rightPinch > pinchThreshold;

        if (shouldK && !isKPressed)
        {
            InputSimulator.KeyDown(0x4B);
            isKPressed = true;
        }
        else if (!shouldK && isKPressed)
        {
            InputSimulator.KeyUp(0x4B);
            isKPressed = false;
        }

        // 持续 Q / E：掌心向上
        bool leftUp = data.leftValid && data.leftPalmNormal.y > palmUpThreshold;
        bool rightUp = data.rightValid && data.rightPalmNormal.y > palmUpThreshold;
        bool bothUp = leftUp && rightUp;

        if (bothUp)
        {
            // 双手同时掌心向上 → 不触发 Q/E，并释放已按下的键
            if (isQPressed) { InputSimulator.KeyUp(0x51); isQPressed = false; }
            if (isEPressed) { InputSimulator.KeyUp(0x45); isEPressed = false; }
        }
        else
        {
            // 左手单独向上 → 按住 Q
            if (leftUp && !isQPressed)
            {
                InputSimulator.KeyDown(0x51);
                isQPressed = true;
            }
            else if (!leftUp && isQPressed)
            {
                InputSimulator.KeyUp(0x51);
                isQPressed = false;
            }

            // 右手单独向上 → 按住 E
            if (rightUp && !isEPressed)
            {
                InputSimulator.KeyDown(0x45);
                isEPressed = true;
            }
            else if (!rightUp && isEPressed)
            {
                InputSimulator.KeyUp(0x45);
                isEPressed = false;
            }
        }

        // ========== 持续移动：双手同时掌心向上 ==========
        // 根据当前手势状态更新移动标志
        isMovingForward = bothUp && characterTransform != null;

        // 每帧执行移动
        if (isMovingForward)
        {
            Vector3 displacement = characterTransform.forward * forwardSpeed * Time.deltaTime;
            characterTransform.position += displacement;
        }
    }

    private HandData GetHandData(Frame frame)
    {
        Hand left = frame.GetHand(Chirality.Left);
        Hand right = frame.GetHand(Chirality.Right);

        HandData d = new HandData();

        d.leftValid = left != null;
        d.rightValid = right != null;

        if (left != null)
        {
            d.leftPalm = left.PalmPosition;
            d.leftPinch = left.PinchStrength;
            d.leftPalmNormal = left.PalmNormal;
        }

        if (right != null)
        {
            d.rightPalm = right.PalmPosition;
            d.rightPinch = right.PinchStrength;
            d.rightPalmNormal = right.PalmNormal;
        }

        return d;
    }
}

#endregion