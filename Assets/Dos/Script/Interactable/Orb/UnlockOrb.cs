using UnityEngine;

public class UnlockOrb : MonoBehaviour, IResettable
{
    [Header("Target")] public UniversalPlatform targetPlatform; // ลาก Platform ที่ต้องการปลดล็อกมาใส่

    [Header("Visuals")] public GameObject pickupEffect; // Effect ตอนเก็บ (ถ้ามี)
    [Header("Sounds")] public AudioClip collectSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collectSound != null) AudioManager.instance.PlayOneShotSFX(collectSound);
            // 1. สั่งปลดล็อก Platform
            if (targetPlatform != null)
            {
                targetPlatform.Unlock();
            }

            // 2. เล่น Effect (ถ้ามี)
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            gameObject.SetActive(false);
        }
    }
    public void ResetState()
    {
        gameObject.SetActive(true); // กลับมาแสดงผลใหม่
    }
}
