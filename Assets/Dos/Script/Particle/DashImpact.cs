using Unity.Cinemachine;
using UnityEngine;

public class DashImpact : MonoBehaviour
{
    [Header("Config")]
    public float lifeTime = 2f; // เวลาทำลายตัวเอง
    
    [Header("Screen Shake")]
    public CinemachineImpulseSource impulseSource; // ตัวสั่งสั่น
    public float shakeForce = 1f;

    void Start()
    {
        // 1. สั่งสั่นทันทีที่เกิด
        if (impulseSource != null)
        {
            // ยิงแรงสั่นสะเทือนออกไป
            impulseSource.GenerateImpulse(shakeForce);
        }

        // 2. ทำลายตัวเองตามเวลาที่กำหนด
        Destroy(gameObject, lifeTime);
    }
}
