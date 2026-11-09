using UnityEngine;
using Leap;

public class HandFollowCamera : MonoBehaviour
{
    [Header("Leap")]
    public LeapProvider leapProvider;

    [Header("Camera")]
    public Transform targetCamera;

    [Header("Position tuning")]
    public float positionSmooth = 15f;
    public float offsetScale = 1.0f;

    [Header("Hand selection")]
    public Chirality handType = Chirality.Right;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main.transform;
    }

    void Update()
    {
        if (leapProvider == null) return;

        Frame frame = leapProvider.CurrentFrame;
        if (frame == null) return;

        Hand hand = frame.GetHand(handType);
        if (hand == null) return;

        // =========================
        // 1. Leap 世界坐标
        // =========================
        Vector3 leapPos = hand.PalmPosition;

        // =========================
        // 2. 转换为相机局部空间（关键）
        // =========================
        Vector3 localPos = targetCamera.InverseTransformPoint(leapPos);

        // =========================
        // 3. 平滑（防抖）
        // =========================
        Vector3 targetPos = localPos * offsetScale;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            Time.deltaTime * positionSmooth
        );

        // =========================
        // 4. 可选：让手跟随相机朝向
        // =========================
        transform.localRotation = Quaternion.identity;
    }
}