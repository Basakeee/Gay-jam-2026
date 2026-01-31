using System;
using System.Collections;
using UnityEngine;
[Serializable]
public class MaskData
{
    [Header("Config")]
    public float jumpHeight;
    [Range(0.01f,2f)] public float wallFrictionSlide;
    public float cooldownInterval;
    public float currentCooldown;
}
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskNormal")]
public class MaskNormal : MaskBase
{
    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        Debug.Log("MaskNormal ActiveSkill");
    }
}
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskDash")]
public class MaskDash : MaskBase
{
    [Header("Dash Config")]
    public float dashSpeed;
    public float dashTime;
    public float dashDistanceHardLimit;

    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        Rigidbody2D rb = parent.GetComponent<Rigidbody2D>();
        PlayerController pc = parent.GetComponent<PlayerController>(); 
        int direction = pc.GetFacingRight() ? 1 : -1; 
        
        pc.StartCoroutine(DashRoutine(rb, pc, direction)); // ส่ง pc เข้าไป
    }

    private IEnumerator DashRoutine(Rigidbody2D rb, PlayerController pc, int direction)
    {
        Vector2 startPos = rb.position;
        float timer = 0f;
        float originalGravity = rb.gravityScale;
        
        // 1. เริ่ม Dash: บอก PlayerController ให้เปิดสถานะชนของ
        pc.OnStartDashing(); 
        
        rb.gravityScale = 0;

        // วนลูปการเคลื่อนที่
        while (timer < dashTime)
        {
            // ถ้า Dash ชนของแล้ว (isDashing ถูกปิดจาก PlayerController) ให้หลุดลูปทันที
            if (!pc.IsDashing()) break;

            float distanceTraveled = Vector2.Distance(startPos, rb.position);
            if (distanceTraveled >= dashDistanceHardLimit) break; 

            rb.linearVelocity = new Vector2(dashSpeed * direction, 0);
            timer += Time.deltaTime;
            yield return null; 
        }

        // 2. จบ Dash: บอก PlayerController ให้ปิดสถานะ
        if (pc.IsDashing()) // ถ้ายัง Dash อยู่ (จบเพราะหมดเวลา ไม่ใช่ชนของ)
        {
            rb.linearVelocity = Vector2.zero;
            pc.OnEndDashing();
        }
        
        rb.gravityScale = originalGravity; 
    }
}
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskHook")]
public class MaskHook : MaskBase
{
    [Header("Hook Config")]
    public float hookRange = 10f;
    public float hookSpeed = 15f;

    public override void ActiveSkill(GameObject parent)
    {
        maskData.currentCooldown = maskData.cooldownInterval;
        PlayerController pc = parent.GetComponent<PlayerController>();
        
        // ส่งค่า hookRange ไปด้วย (แม้ pc จะรู้อยู่แล้วจากการเล็ง แต่ส่งไปเพื่อยืนยัน)
        pc.StartHook(hookRange, hookSpeed);
        
        Debug.Log("MaskHook Fired");
    }
}
[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskPlatform")]
public class MaskPlatform : MaskBase
{
    [Header("Platform Config")]
    public GameObject platformPrefab;
    public float timePlatformLast;
    public float checkRadius = 0.5f; // รัศมีเช็คพื้น
    public LayerMask whatIsGround;   // Layer ของพื้น (เพื่อไม่ให้สร้างทับพื้นเดิม)

    public override void ActiveSkill(GameObject parent)
    {
        PlayerController pc = parent.GetComponent<PlayerController>();
        Transform spawnPos = pc.GetPlatformSpawnPos(); // จุดเสก (ควรอยู่ใต้เท้า)

        // 1. เช็คว่าตรงจุดที่จะเสก มีพื้นอยู่แล้วหรือเปล่า?
        if (!Physics2D.OverlapCircle(spawnPos.position, checkRadius, whatIsGround))
        {
            // ถ้าว่างเปล่า ค่อยสร้าง
            maskData.currentCooldown = maskData.cooldownInterval; // เริ่มนับ Cooldown เมื่อสร้างสำเร็จเท่านั้น
            
            GameObject platform = Instantiate(platformPrefab, spawnPos.position, Quaternion.identity);
            Destroy(platform, timePlatformLast);
            Debug.Log("Platform Created!");
        }
        else
        {
            Debug.Log("Cannot create platform here (Ground detected).");
            // ไม่เริ่มนับ Cooldown ผู้เล่นจะได้กดใหม่ได้เลย
        }
    }
}
