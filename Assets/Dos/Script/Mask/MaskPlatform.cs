using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "MaskData", menuName = "ScriptableObjects/MaskPlatform")]
public class MaskPlatform : MaskBase
{
    [Header("Platform Config")]
    public GameObject platformPrefab;
    public float timePlatformLast;
    public float checkRadius = 0.5f; // รัศมีเช็คพื้น
    public LayerMask whatIsGround;   // Layer ของพื้น (เพื่อไม่ให้สร้างทับพื้นเดิม)
    [Header("Particle Config")]
    public ParticleSystem spawnParticles;
    public ParticleSystem destroyParticles;
    [Header("SFX Config")] 
    public AudioClip spawnSound;
    public AudioClip destroySound;

    public override void ActiveSkill(GameObject parent)
    {
        PlayerController pc = parent.GetComponent<PlayerController>();
        Transform spawnPos = pc.GetPlatformSpawnPos(); // จุดเสก (ควรอยู่ใต้เท้า)

        // 1. เช็คว่าตรงจุดที่จะเสก มีพื้นอยู่แล้วหรือเปล่า?
        if (!Physics2D.OverlapCircle(spawnPos.position, checkRadius, whatIsGround))
        {
            // ถ้าว่างเปล่า ค่อยสร้าง
            maskData.currentCooldown = maskData.cooldownInterval; // เริ่มนับ Cooldown เมื่อสร้างสำเร็จเท่านั้น
            
            GameObject platform = Instantiate(platformPrefab, spawnPos.position, platformPrefab.transform.rotation);
            Instantiate(spawnParticles, spawnPos.position, Quaternion.identity);
            AudioManager.instance.PlayOneShotSFX(spawnSound);
            pc.StartCoroutine(SpawnParticle(platform));
            Destroy(platform, timePlatformLast);
            Debug.Log("Platform Created!");
        }
        else
        {
            Debug.Log("Cannot create platform here (Ground detected).");
            // ไม่เริ่มนับ Cooldown ผู้เล่นจะได้กดใหม่ได้เลย
        }
    }

    IEnumerator SpawnParticle(GameObject platform)
    {
        yield return new WaitForSeconds(timePlatformLast - 0.05f);
        Instantiate(destroyParticles, platform.transform.position, Quaternion.identity);
        AudioManager.instance.PlayOneShotSFX(destroySound);
    }
}

