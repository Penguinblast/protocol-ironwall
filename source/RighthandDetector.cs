using Leap;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class RightHandGKey : MonoBehaviour
{
    [Header("Leap 数据源")]
    public LeapProvider leapProvider;

    [Header("握拳阈值（G键专用）")]
    [Range(0, 1)] public float grabThreshold = 0.8f;

    [Header("捏合阈值（鼠标左键专用）")]
    [Range(0, 1)] public float pinchThreshold = 0.8f;

    [Header("调试日志")]
    public bool debugLog = true;

    // Windows API 键盘模拟
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);
    private const byte VK_G = 0x47;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    // Windows API 鼠标模拟
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
    private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    private const uint MOUSEEVENTF_LEFTUP = 0x04;

    // 状态变量
    private bool leftIsGrabbing = false;
    private bool rightIsGrabbing = false;
    private bool rightIsPinching = false;
    
    private bool wasLeftGrabbing = false;
    private bool wasRightPinching = false;
    private bool wasBothGrabbing = false;

    void Start()
    {
        if (leapProvider == null)
            leapProvider = FindObjectOfType<LeapProvider>();
    }

    void Update()
    {
        Frame frame = leapProvider?.CurrentFrame;
        if (frame == null) return;

        // 获取左右手
        Hand leftHand = frame.GetHand(Chirality.Left);
        Hand rightHand = frame.GetHand(Chirality.Right);

        // 1. 检测手势状态
        leftIsGrabbing = leftHand != null && leftHand.GrabStrength > grabThreshold;
        rightIsGrabbing = rightHand != null && rightHand.GrabStrength > grabThreshold;
        rightIsPinching = rightHand != null && rightHand.PinchStrength > pinchThreshold;

        // 2. 双手握拳 → 按下/松开 G 键，并切换全局视角
        bool isBothGrabbing = leftIsGrabbing && rightIsGrabbing;
        if (isBothGrabbing != wasBothGrabbing)
        {
            PressGKey(isBothGrabbing);
            wasBothGrabbing = isBothGrabbing;
        }

        // 3. 右手捏合 → 鼠标左键
        if (rightIsPinching != wasRightPinching)
        {
            SetMouseLeftButtonState(rightIsPinching);
            wasRightPinching = rightIsPinching;
        }
    }

    // 双手握拳触发G键，并切换全局视角
    private void PressGKey(bool press)
    {
        if (press)
        {
            keybd_event(VK_G, 0, 0, IntPtr.Zero);
            if (debugLog) Debug.Log("[双手握拳] G 键 按下");

            // 切换全局视角状态（自动启用/禁用 SkillController）
            ViewStateManager.ToggleView();
        }
        else
        {
            keybd_event(VK_G, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
            if (debugLog) Debug.Log("[双手松开] G 键 释放");
        }
    }

    // 右手捏合触发鼠标左键
    private void SetMouseLeftButtonState(bool press)
    {
        if (press)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            if (debugLog) Debug.Log("[右手捏合] 鼠标左键：按下");
        }
        else
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            if (debugLog) Debug.Log("[右手松开] 鼠标左键：释放");
        }
    }
}